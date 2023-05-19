// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#pragma once

class Output;

enum SearchOptions
{
	SO_IGNORECASE = 1 << 0,
	SO_LITERAL = 1 << 1,
	SO_BRUTEFORCE = 1 << 2,

	SO_FILE_NAMEREGEX = 1 << 3,
	SO_FILE_PATHREGEX = 1 << 4,
	SO_FILE_VISUALASSIST = 1 << 5,
	SO_FILE_FUZZY = 1 << 6,
	SO_FILE_CUSTOM = 1 << 7,

	SO_VISUALSTUDIO = 1 << 8,
	SO_COLUMNNUMBER = 1 << 9,
	SO_COLUMNNUMBEREND = 1 << 10,

	SO_HIGHLIGHT = 1 << 11,
	SO_HIGHLIGHT_MATCHES = 1 << 12,

	SO_SUMMARY = 1 << 13
};

unsigned int getRegexOptions(unsigned int options);

unsigned int searchProject(Output* output, const char* file, const char* string, unsigned int options, unsigned int limit, const char* include, const char* exclude);
