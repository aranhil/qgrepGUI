#pragma once

#ifdef QGREP_EXPORT_DLL
#define QGREP_DLL __declspec(dllexport)
#else
#define QGREP_DLL __declspec(dllimport)
#endif

QGREP_DLL const char* qgrepWrapper(char* arguments, int size, const char* errors);
QGREP_DLL void qgrepWrapperAsync(char* arguments, int size, void (*cb)(const char*, int), void (*errorsCb)(const char*, int));
