// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#pragma once

#include <string>

class Output
{
public:
	virtual ~Output() {}

	virtual void rawprint(const char* data, size_t size) = 0;

	virtual void print(const char* message, ...) = 0;
	virtual void error(const char* message, ...) = 0;
	virtual void printLocalized(const std::string& initialString, std::initializer_list<std::string> args) = 0;

	virtual void progress(double percentage) {}
	virtual bool isStopped() { return false; }

	virtual bool isTTY() { return false; }
};
