#pragma once

#ifdef QGREP_EXPORT_DLL
#define QGREP_DLL __declspec(dllexport)
#else
#define QGREP_DLL __declspec(dllimport)
#endif

QGREP_DLL void qgrepWrapperAsync(char* arguments, int size, bool (*stringCallback)(const char*), void (*errorsCallback)(const char*), void (*progressCallback)(double), void (*localizedStringCallback)(const char**, int size));
