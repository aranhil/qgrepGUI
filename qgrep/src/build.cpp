// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#include "common.hpp"
#include "build.hpp"

#include "output.hpp"
#include "format.hpp"
#include "fileutil.hpp"
#include "filestream.hpp"
#include "constants.hpp"
#include "project.hpp"
#include "encoding.hpp"
#include "files.hpp"
#include "bloom.hpp"
#include "casefold.hpp"
#include "compression.hpp"
#include "workqueue.hpp"
#include "blockingqueue.hpp"

#include <algorithm>
#include <vector>
#include <list>
#include <numeric>
#include <string>
#include <memory>
#include <map>

#include <string.h>

struct BuildStatistics
{
	size_t chunkCount;
	size_t fileCount;
	uint64_t fileSize;
	uint64_t resultSize;
};

struct Blob
{
	size_t offset;
	size_t count;
	std::shared_ptr<std::vector<char>> storage;

	Blob(): offset(0), count(0)
	{
	}

	Blob(std::vector<char> storage): offset(0), count(storage.size()), storage(new std::vector<char>(std::move(storage)))
	{
	}

	const char* data() const
	{
		assert(offset + count <= storage->size());
		return storage->empty() ? nullptr : &(*storage)[0] + offset;
	}

	size_t size() const
	{
		return count;
	}
};

struct File
{
	std::string name;
	Blob contents;

	uint32_t startLine;
	uint64_t fileSize;
	uint64_t timeStamp;
};

struct Chunk
{
	std::vector<File> files;
	size_t totalSize;

	Chunk(): totalSize(0)
	{
	}
};

struct ChunkData
{
	std::unique_ptr<char[]> data;
	size_t size;

	size_t dataOffset;
	size_t dataSize;
};

struct ChunkIndex
{
	std::unique_ptr<char[]> data;
	size_t size;
	unsigned int iterations;

	ChunkIndex(): size(0), iterations(0)
	{
	}
};

struct ChunkFileData
{
	unsigned int order;

	DataChunkHeader header;
	std::unique_ptr<char[]> compressedData;
	std::unique_ptr<char[]> index;
	std::unique_ptr<char[]> extra;
	bool firstFileIsSuffix;
};

struct BuildContext
{
	Output* output;
	size_t fileCount;

	std::list<File> pendingFiles;
	size_t pendingSize;

	FileStream outData;

	unsigned int chunkOrder;
	WorkQueue prepareChunkQueue;
	BlockingQueue<ChunkFileData> writeChunkQueue;
	std::thread writeChunkThread;

	BuildContext(Output* output, size_t fileCount)
		: output(output), fileCount(fileCount), pendingSize(0), chunkOrder(0)
		, prepareChunkQueue(std::max(WorkQueue::getIdealWorkerCount(), 2u) - 1, kMaxQueuedChunkData)
	{
	}
};

static void printStatistics(Output* output, const BuildStatistics& stats, unsigned int totalFileCount)
{
	output->printLocalized("FilesInAndOutMessage", {
		std::to_string(stats.fileCount),
		std::to_string((int)(stats.fileSize / 1024 / 1024)),
		std::to_string((int)(stats.resultSize / 1024 / 1024))
	});

	output->progress(totalFileCount == 0 ? 100.0f : stats.fileCount * 100.0f / totalFileCount);
}

static size_t normalizeEOL(char* data, size_t size)
{
	// fast path: no \r in the file
	if (memchr(data, '\r', size) == nullptr)
		return size;

	// replace \r\n with \n, replace stray \r with \n
	size_t result = 0;

	for (size_t i = 0; i < size; ++i)
	{
		if (data[i] == '\r')
		{
			data[result++] = '\n';
			if (i + 1 < size && data[i + 1] == '\n') i++;
		}
		else
			data[result++] = data[i];
	}

	return result;
}

static std::vector<char> readFile(FileStream& in)
{
	std::vector<char> result;

	// read file as is
	char buffer[65536];
	size_t readsize;

	while ((readsize = in.read(buffer, sizeof(buffer))) > 0)
	{
		result.insert(result.end(), buffer, buffer + readsize);
	}

	// normalize new lines in a cross-platform way (don't rely on text-mode file I/O)
	if (!result.empty())
	{
		size_t size = normalizeEOL(&result[0], result.size());
		assert(size <= result.size());
		result.resize(size);
	}

	return result;
}

static std::pair<size_t, unsigned int> skipByLines(const char* data, size_t dataSize)
{
	auto result = std::make_pair(0, 0);

	for (size_t i = 0; i < dataSize; ++i)
		if (data[i] == '\n')
		{
			result.first = i + 1;
			result.second++;
		}

	return result;
}

static size_t skipOneLine(const char* data, size_t dataSize)
{
	for (size_t i = 0; i < dataSize; ++i)
		if (data[i] == '\n')
			return i + 1;

	return dataSize;
}

static File splitPrefix(File& file, size_t size)
{
	File result = file;

	assert(size <= file.contents.size());
	result.contents.count = size;
	file.contents.offset += size;
	file.contents.count -= size;

	return result;
}

static void appendChunkFile(Chunk& chunk, File&& file)
{
	chunk.totalSize += file.contents.size();
	chunk.files.emplace_back(std::move(file));
}

static void appendChunkFilePrefix(Chunk& chunk, File& file, size_t remainingSize)
{
	const char* data = file.contents.data();
	size_t dataSize = file.contents.size();

	assert(remainingSize < dataSize);
	std::pair<size_t, unsigned int> skip = skipByLines(data, remainingSize);

	// add file even if we could not split the (very large) line if it'll be the only file in chunk
	if (skip.first > 0 || chunk.files.empty())
	{
		size_t skipSize = (skip.first > 0) ? skip.first : skipOneLine(data, dataSize);
		unsigned int skipLines = (skip.first > 0) ? skip.second : 1;

		chunk.totalSize += skipSize;
		chunk.files.push_back(splitPrefix(file, skipSize));

		file.startLine += skipLines;
	}
}

static void writeChunk(BuildContext* context, unsigned int order, const DataChunkHeader& header, std::unique_ptr<char[]> compressedData, std::unique_ptr<char[]> index, std::unique_ptr<char[]> extra, bool firstFileIsSuffix)
{
	assert(compressedData);
	ChunkFileData chunk = { order, header, std::move(compressedData), std::move(index), std::move(extra), firstFileIsSuffix };

	context->writeChunkQueue.push(std::move(chunk));
}

static size_t getChunkNameTotalSize(const Chunk& chunk)
{
	size_t result = 0;

	for (size_t i = 0; i < chunk.files.size(); ++i)
		result += chunk.files[i].name.size();

	return result;
}

static size_t getChunkDataTotalSize(const Chunk& chunk)
{
	size_t result = 0;

	for (size_t i = 0; i < chunk.files.size(); ++i)
		result += chunk.files[i].contents.size();

	return result;
}

static ChunkData prepareChunkData(const Chunk& chunk)
{
	size_t headerSize = sizeof(DataChunkFileHeader) * chunk.files.size();
	size_t nameSize = getChunkNameTotalSize(chunk);
	size_t dataSize = getChunkDataTotalSize(chunk);
	size_t totalSize = headerSize + nameSize + dataSize;

	ChunkData result;
	result.data.reset(new char[totalSize]);
	result.size = totalSize;
	result.dataOffset = headerSize + nameSize;
	result.dataSize = dataSize;

	size_t nameOffset = headerSize;
	size_t dataOffset = headerSize + nameSize;

	for (size_t i = 0; i < chunk.files.size(); ++i)
	{
		const File& f = chunk.files[i];

		memcpy(result.data.get() + nameOffset, f.name.c_str(), f.name.length());
		memcpy(result.data.get() + dataOffset, f.contents.data(), f.contents.size());

		DataChunkFileHeader& h = reinterpret_cast<DataChunkFileHeader*>(result.data.get())[i];

		h.nameOffset = nameOffset;
		h.nameLength = f.name.size();
		h.dataOffset = dataOffset;
		h.dataSize = f.contents.size();

		h.startLine = f.startLine;
		h.reserved = 0;

		h.fileSize = f.fileSize;
		h.timeStamp = f.timeStamp;

		nameOffset += f.name.size();
		dataOffset += f.contents.size();
	}

	assert(nameOffset == headerSize + nameSize && dataOffset == totalSize);

	return result;
}

static size_t getChunkIndexSize(size_t dataSize)
{
	// data compression ratio is ~5x
	// we want the index to be ~10% of the compressed data
	// so index is ~50x smaller than the original data
	size_t indexSize = dataSize / 50;

	// don't bother storing tiny indices
	return indexSize < 1024 ? 0 : indexSize;
}

// http://pages.cs.wisc.edu/~cao/papers/summary-cache/node8.html 
static unsigned int getIndexHashIterations(unsigned int indexSize, unsigned int itemCount)
{
	unsigned int m = indexSize * 8;
	unsigned int n = itemCount;
	double k = n == 0 ? 1.0 : 0.693147181 * static_cast<double>(m) / static_cast<double>(n);

	return (k < 1) ? 1 : (k > 16) ? 16 : static_cast<unsigned int>(k);
}

struct IntSet
{
	unsigned int* data;
	size_t capacity;
	size_t size;

	IntSet(size_t capacity = 0): data(new unsigned int[capacity]), capacity(capacity), size(0)
	{
		assert((capacity & (capacity - 1)) == 0);

		memset(data, 0, capacity * sizeof(unsigned int));
	}

	~IntSet()
	{
		delete[] data;
	}

	IntSet(const IntSet&) = delete;
	IntSet(IntSet&&) = delete;
	IntSet& operator=(const IntSet&) = delete;
	IntSet& operator=(IntSet&&) = delete;

	void grow()
	{
		IntSet res(std::max(capacity * 2, size_t(16)));

		for (size_t i = 0; i < capacity; ++i)
			if (data[i])
				res.insert(data[i]);

		std::swap(data, res.data);
		std::swap(capacity, res.capacity);
		assert(size == res.size);
	}

	void insert(unsigned int key)
	{
		assert(key != 0);

		if (size >= capacity / 2)
			grow();

		unsigned int m = capacity - 1;
		unsigned int h = bloomHash2(key) & m;
		unsigned int i = 0;

		while (data[h] != key)
		{
			if (data[h] == 0)
			{
				data[h] = key;
				size++;
				break;
			}

			i = i + 1;
			h = (h + i) & m;
		}
	}

	static size_t optimalCapacity(size_t count)
	{
		size_t capacity = 1;
		while (count >= capacity / 2)
			capacity *= 2;
		return capacity;
	}
};

static ChunkIndex prepareChunkIndex(const char* data, size_t size)
{
	// estimate index size
	size_t indexSize = getChunkIndexSize(size);

	if (indexSize == 0) return ChunkIndex();

	// collect ngram data; assume ~10% ngrams are unique
	IntSet ngrams(IntSet::optimalCapacity(size / 10));

	for (size_t i = 3; i < size; ++i)
	{
		char a = data[i - 3], b = data[i - 2], c = data[i - 1], d = data[i];

		// don't waste bits on ngrams that cross lines
		if (a != '\n' && b != '\n' && c != '\n' && d != '\n')
		{
			unsigned int n = ngram(casefold(a), casefold(b), casefold(c), casefold(d));
			if (n != 0)
				ngrams.insert(n);
		}
	}

	// estimate iteration count
	unsigned int iterations = getIndexHashIterations(indexSize, ngrams.size);

	// fill bloom filter
	ChunkIndex result;
	result.data.reset(new char[indexSize]);
	result.size = indexSize;
	result.iterations = iterations;

	unsigned char* index = reinterpret_cast<unsigned char*>(result.data.get());

	memset(index, 0, indexSize);

	for (size_t i = 0; i < ngrams.capacity; ++i)
		if (unsigned int n = ngrams.data[i])
			bloomFilterUpdate(index, indexSize, n, iterations);

	return result;
}

static void storeChunk(BuildContext* context, const Chunk& chunk)
{
	if (chunk.files.empty()) return;

	ChunkData data = prepareChunkData(chunk);
	unsigned int order = context->chunkOrder++;

	size_t fileCount = chunk.files.size();
	bool firstFileIsSuffix = !chunk.files.empty() && chunk.files[0].startLine != 0;
	std::string lastFile = chunk.files.empty() ? "" : chunk.files.back().name;

	// workaround for lack of generalized capture
	std::shared_ptr<ChunkData> sdata(new ChunkData(std::move(data)));

	context->prepareChunkQueue.push([=] {
		ChunkIndex index = prepareChunkIndex(sdata->data.get() + sdata->dataOffset, sdata->dataSize);

		std::pair<std::unique_ptr<char[]>, size_t> cdata = compress(sdata->data.get(), sdata->size, kFileDataCompressionLevel);

		std::unique_ptr<char[]> extra(new char[lastFile.size()]);
		memcpy(extra.get(), lastFile.data(), lastFile.size());

		DataChunkHeader header = {};
		header.fileCount = fileCount;
		header.fileTableSize = sdata->dataOffset;
		header.compressedSize = cdata.second;
		header.uncompressedSize = sdata->size;
		header.indexSize = index.size;
		header.indexHashIterations = index.iterations;
		header.extraSize = lastFile.size();

		writeChunk(context, order, header, std::move(cdata.first), std::move(index.data), std::move(extra), firstFileIsSuffix);
	}, sdata->size);
}

static void flushChunk(BuildContext* context, size_t size)
{
	Chunk chunk;

	// grab pending files one by one and add it to current chunk
	while (chunk.totalSize < size && !context->pendingFiles.empty())
	{
		File file = std::move(context->pendingFiles.front());
		context->pendingFiles.pop_front();

		size_t remainingSize = size - chunk.totalSize;

		if (file.contents.size() <= remainingSize)
		{
			// no need to split the file, just add it
			appendChunkFile(chunk, std::move(file));
		}
		else
		{
			// last file may not fit completely, store some part of it and put the remaining lines back into pending list
			appendChunkFilePrefix(chunk, file, remainingSize);

			// we might have fully appended the file if it was one huge line, but usually there's a remainder to be processed later
			if (file.contents.size())
				context->pendingFiles.emplace_front(file);

			// it's impossible to add any more files to this chunk without making it larger than requested
			break;
		}
	}

	// update pending size
	assert(chunk.totalSize <= context->pendingSize);
	context->pendingSize -= chunk.totalSize;

	// store resulting chunk
	storeChunk(context, chunk);
}

static void writeChunkThreadFun(BuildContext* context)
{
	unsigned int order = 0;
	std::map<unsigned int, ChunkFileData> chunks;

	BuildStatistics stats = {};

	printStatistics(context->output, stats, context->fileCount);

	while (true)
	{
		ChunkFileData chunk = context->writeChunkQueue.pop();

		assert(chunks.count(chunk.order) == 0);
		chunks[chunk.order] = std::move(chunk);

		while (!chunks.empty() && chunks.begin()->first == order)
		{
			const ChunkFileData& chunk = chunks.begin()->second;
			const DataChunkHeader& header = chunk.header;

			// empty compressed data acts as a terminator flag
			if (!chunk.compressedData)
				return;

			context->outData.write(&header, sizeof(header));
			context->outData.write(chunk.extra.get(), header.extraSize);
			context->outData.write(chunk.index.get(), header.indexSize);
			context->outData.write(chunk.compressedData.get(), header.compressedSize);

			stats.chunkCount++;
			stats.fileCount += header.fileCount - chunk.firstFileIsSuffix;
			stats.fileSize += header.uncompressedSize;
			stats.resultSize += header.compressedSize;

			chunks.erase(chunks.begin());
			order++;

			printStatistics(context->output, stats, context->fileCount);
		}
	}
}

BuildContext* buildStart(Output* output, const char* path, unsigned int fileCount)
{
	std::unique_ptr<BuildContext> context(new BuildContext(output, fileCount));

	createPathForFile(path);

	context->outData.open(path, "wb");
	if (!context->outData)
	{
		output->printLocalized("ErrorOpeningDataFileForWriting", { path });
		return 0;
	}

	DataFileHeader header = {};
	memcpy(header.magic, kDataFileHeaderMagic, sizeof(header.magic));

	context->outData.write(&header, sizeof(header));

	std::thread(std::bind(writeChunkThreadFun, context.get())).swap(context->writeChunkThread);

	return context.release();
}

void buildAppendFilePart(BuildContext* context, const char* path, unsigned int startLine, const char* data, size_t dataSize, uint64_t timeStamp, uint64_t fileSize)
{
	if (!context->pendingFiles.empty() && context->pendingFiles.back().name == path)
	{
		File& file = context->pendingFiles.back();

		assert(file.startLine < startLine);
		assert(file.timeStamp == timeStamp && file.fileSize == fileSize);
		assert(file.contents.offset + file.contents.count == file.contents.storage->size());

		file.contents.storage->insert(file.contents.storage->end(), data, data + dataSize);
		file.contents.count += dataSize;

		context->pendingSize += dataSize;
	}
	else
	{
		File file;

		file.name = path;
		file.startLine = startLine;
		file.timeStamp = timeStamp;
		file.fileSize = fileSize;
		file.contents = std::vector<char>(data, data + dataSize);

		context->pendingFiles.emplace_back(file);
		context->pendingSize += dataSize;
	}

	// We try to maintain a small pending set; this makes sure we have at most 2 chunk sizes worth of data
	// It's possible in theory to flush chunks earlier (when we reach kChunkSize), but this means that if we
	// see an already compressed chunk (buildAppendChunk), we may not be able to rebalance chunk sizes and will
	// be forced to recompress.
	while (context->pendingSize >= kChunkSize * 2)
	{
		flushChunk(context, kChunkSize);
	}
}

bool buildAppendFile(BuildContext* context, const char* path, uint64_t timeStamp, uint64_t fileSize)
{
	FileStream in(path, "rb");
	if (!in)
	{
		context->output->printLocalized("ErrorReadingFile", { path });
		return false;
	}

	try
	{
		std::vector<char> contents = convertToUTF8(readFile(in));

		buildAppendFilePart(context, path, 0, contents.empty() ? 0 : &contents[0], contents.size(), timeStamp, fileSize);

		return true;
	}
	catch (const std::bad_alloc&)
	{
		context->output->printLocalized("OutOfMemoryErrorReadingFile", { path });
		return false;
	}
}

static size_t getOptimalChunkSize(size_t pendingSize)
{
	// This function returns a size in [0.75x .. 1.5x] range (or 0)
	const size_t kChunkMaxSize = kChunkSize * 3 / 2;
	const size_t kChunkMinSize = kChunkMaxSize / 2;

	// Never store chunks smaller than 0.75x
	if (pendingSize < kChunkMinSize)
		return 0;

	// If we have at least 2 chunks worth of data, it's safe to store a full chunk
	if (pendingSize >= kChunkSize * 2)
		return kChunkSize;

	// If we have less than 1.5x chunks, we need to store the entire set in one go
	if (pendingSize < kChunkMaxSize)
		return pendingSize;

	// Otherwise we should split the chunk in half; the reason why it's important to not just
	// return kChunkSize here is that if we have, say, 1.6x chunks to store, splitting into 1x and 0.6x
	// leaves us with a chunk that's smaller than 0.75x
	// Splitting in half makes sure that both halves are at least kChunkMinSize
	return pendingSize / 2;
}

bool buildAppendChunk(BuildContext* context, const DataChunkHeader& header, std::unique_ptr<char[]>& compressedData, std::unique_ptr<char[]>& index, std::unique_ptr<char[]>& extra, bool firstFileIsSuffix)
{
	// In order to maintain file order, we need to flush pending files before writing the chunk.
	// To balance the cost of chunk recompression with chunk sizes, we flush all files but instead of
	// using a fixed chunk size, we use a balanced chunk size computed in getOptimalChunkSize
	while (!context->pendingFiles.empty())
	{
		size_t chunkSize = getOptimalChunkSize(context->pendingSize);

		if (chunkSize == 0)
			return false;

		flushChunk(context, chunkSize);
	}

	// We should be good to go now
	assert(context->pendingSize == 0 && context->pendingFiles.empty());

	unsigned int order = context->chunkOrder++;
	writeChunk(context, order, header, std::move(compressedData), std::move(index), std::move(extra), firstFileIsSuffix);

	return true;
}

unsigned int buildFinish(BuildContext* context)
{
	if (context->writeChunkThread.joinable())
	{
		// Write all remaining files (usually just flushes a single chunk)
		while (!context->pendingFiles.empty())
		{
			flushChunk(context, kChunkSize);
		}

		ChunkFileData chunkDummy = { context->chunkOrder };
		context->writeChunkQueue.push(std::move(chunkDummy));
		context->writeChunkThread.join();
	}

	unsigned int result = context->chunkOrder;

	delete context;

	return result;
}

void buildProject(Output* output, const char* path)
{
	output->printLocalized("BuildingMessage", { path });

	std::unique_ptr<ProjectGroup> group = parseProject(output, path);
	if (!group)
		return;

	removeFile(replaceExtension(path, ".qgc").c_str());

	output->printLocalized("ScanningProjectMessage", { });

	std::vector<FileInfo> files = getProjectGroupFiles(output, group.get());

	output->printLocalized("BuildingFileTableMessage", { });

	if (!buildFiles(output, path, files))
		return;
	
	std::string targetPath = replaceExtension(path, ".qgd");
	std::string tempPath = targetPath + "_";

	{
		BuildContext* builder = buildStart(output, tempPath.c_str(), files.size());
		if (!builder) return;

		for (auto& f: files)
		{
			buildAppendFile(builder, f.path.c_str(), f.timeStamp, f.fileSize);
		}

		buildFinish(builder);
	}
	
	if (!renameFile(tempPath.c_str(), targetPath.c_str()))
	{
		output->printLocalized("ErrorSavingDataFile", { targetPath });
		return;
	}
}
