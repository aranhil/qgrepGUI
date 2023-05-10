// This file is part of qgrep and is distributed under the MIT license, see LICENSE.md
#include "common.hpp"
#include "stringutil.hpp"

void strprintf(std::string& result, const char* format, va_list args)
{
	// copy arglist before use so that we can use it again below
	va_list temp;
	va_copy(temp, args);
	int count = vsnprintf(0, 0, format, temp);
	va_end(temp);

	assert(count >= 0);

	if (count > 0)
	{
		size_t offset = result.size();
		result.resize(offset + count + 1);

		vsnprintf(&result[offset], count + 1, format, args);

		assert(result[offset + count] == 0);
		result.resize(offset + count);
	}
}
