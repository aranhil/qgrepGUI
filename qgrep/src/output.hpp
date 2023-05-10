#pragma once

#define PRINT_ERROR(output, message, ...) output->error("%s:%d: " message, __FILE__, __LINE__, __VA_ARGS__);

class Output
{
public:
	virtual ~Output() {}

	virtual void rawprint(const char* data, size_t size) = 0;

	virtual void print(const char* message, ...) = 0;
	virtual void error(const char* message, ...) = 0;

	virtual void progress(int percentage) {}
	virtual bool isStopped() { return false; }

	virtual bool isTTY() { return false; }
};
