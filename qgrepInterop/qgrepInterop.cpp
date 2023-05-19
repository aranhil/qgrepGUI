#include <vector>
#include <string>

#include <msclr\marshal.h>
#include <msclr\marshal_cppstd.h>

#include <qgrep.hpp>

#include "qgrepInterop.h"

void qgrepInterop::QGrepWrapper::CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, StringCallback^ stringCb, ErrorCallback^ errorsCb, ProgressCalback^ progressCb)
{
    std::string unmanagedArguments;

    for each (System::String^ argument in arguments)
    {
        unmanagedArguments += msclr::interop::marshal_as<std::string>(argument) + "\n";
    }

    stringCallback = stringCb;
    errorCallback = errorsCb;
    progressCalback = progressCb;

    qgrepWrapperAsync(const_cast<char*>(unmanagedArguments.c_str()), (int)unmanagedArguments.size(), &NativeQGrepWrapper::nativeStringCallback, &NativeQGrepWrapper::nativeErrorsCallback, &NativeQGrepWrapper::nativeProgressCallback);
}

bool qgrepInterop::NativeQGrepWrapper::nativeStringCallback(const char* result, int size)
{
    if (QGrepWrapper::stringCallback != nullptr)
    {
        return QGrepWrapper::stringCallback(gcnew System::String(result, 0, size));
    }

    return false;
}

void qgrepInterop::NativeQGrepWrapper::nativeProgressCallback(int percentage)
{
    if (QGrepWrapper::progressCalback != nullptr)
    {
        return QGrepWrapper::progressCalback(percentage);
    }
}

void qgrepInterop::NativeQGrepWrapper::nativeErrorsCallback(const char* error, int size)
{
    if (QGrepWrapper::errorCallback != nullptr)
    {
        QGrepWrapper::errorCallback(gcnew System::String(error, 0, size));
    }
}