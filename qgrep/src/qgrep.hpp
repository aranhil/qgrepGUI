#pragma once

#ifdef QGREP_EXPORT_DLL
#define QGREP_DLL __declspec(dllexport)
#else
#define QGREP_DLL __declspec(dllimport)
#endif

QGREP_DLL void qgrepWrapperAsync(char* arguments, int size, bool (*stringCallback)(const char*, int), void (*errorsCallback)(const char*, int), void (*progressCallback)(int));
