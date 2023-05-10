// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#pragma once

class Output;

unsigned int filterBuffer(Output* output, const char* string, unsigned int options, unsigned int limit, const char* buffer, size_t bufferSize);
unsigned int filterStdin(Output* output, const char* string, unsigned int options, unsigned int limit);
