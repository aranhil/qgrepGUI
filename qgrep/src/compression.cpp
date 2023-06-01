// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#include "common.hpp"
#include "compression.hpp"

#include "lz4.h"
#include "lz4hc.h"

std::pair<std::unique_ptr<char[]>, size_t> compress(const void* data, size_t dataSize, int level)
{
	if (dataSize == 0) return std::make_pair(std::unique_ptr<char[]>(), 0);

	int csizeBound = LZ4_compressBound(dataSize);

	std::unique_ptr<char[]> cdata(new char[csizeBound]);
	
	int csize = (level == 0)
		? LZ4_compress_default(static_cast<const char*>(data), cdata.get(), dataSize, csizeBound)
		: LZ4_compress_HC(static_cast<const char*>(data), cdata.get(), dataSize, csizeBound, level);
	if(!(csize >= 0 && csize <= csizeBound)) throw std::exception("");

	return std::make_pair(std::move(cdata), csize);
}

void decompress(void* dest, size_t destSize, const void* source, size_t sourceSize)
{
	if (sourceSize == 0 && destSize == 0) return;

	int result = LZ4_decompress_safe(static_cast<const char*>(source), static_cast<char*>(dest), sourceSize, destSize);
	if(!(result >= 0)) throw std::exception("");
	if(!(static_cast<size_t>(result) == destSize)) throw std::exception("");
}

void decompressPartial(void* dest, size_t destSize, const void* source, size_t sourceSize, size_t targetSize)
{
	if(!(targetSize <= destSize)) throw std::exception("");
	if (sourceSize == 0 && destSize == 0) return;

	int result = LZ4_decompress_safe_partial(static_cast<const char*>(source), static_cast<char*>(dest), sourceSize, targetSize, destSize);
	if(!(result >= 0)) throw std::exception("");
	if(!(static_cast<size_t>(result) >= targetSize)) throw std::exception("");
	if(!(static_cast<size_t>(result) <= destSize)) throw std::exception("");
}
