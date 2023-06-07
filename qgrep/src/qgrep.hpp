#pragma once

#ifdef QGREP_EXPORT_DLL
#define QGREP_DLL __declspec(dllexport)
#else
#define QGREP_DLL __declspec(dllimport)
#endif

QGREP_DLL void qgrepWrapperAsync(char* arguments, int size, void (*stringCallback)(const char*), bool (*checkStoppedCallback)(), void (*progressCallback)(double), void (*localizedStringCallback)(const char**, int size));
