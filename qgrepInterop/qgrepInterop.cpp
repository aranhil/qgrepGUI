#include <vector>
#include <string>

#include <msclr\marshal.h>
#include <msclr\marshal_cppstd.h>

#include <qgrep.hpp>

#include "qgrepInterop.h"

void qgrepInterop::QGrepWrapper::CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, StringCallback^ stringCb, ErrorCallback^ errorsCb, ProgressCalback^ progressCb, LocalizedStringCallback^ localizedStringCb)
{
    std::string unmanagedArguments;

    for each (System::String^ argument in arguments)
    {
        unmanagedArguments += msclr::interop::marshal_as<std::string>(argument) + "\n";
    }

    stringCallback = stringCb;
    errorCallback = errorsCb;
    progressCalback = progressCb;
    localizedStringCalback = localizedStringCb;

    qgrepWrapperAsync(const_cast<char*>(unmanagedArguments.c_str()), (int)unmanagedArguments.size(), &NativeQGrepWrapper::nativeStringCallback, &NativeQGrepWrapper::nativeErrorsCallback, &NativeQGrepWrapper::nativeProgressCallback, &NativeQGrepWrapper::nativeLocalizedStringCallback);
}

bool qgrepInterop::NativeQGrepWrapper::nativeStringCallback(const char* result)
{
    if (QGrepWrapper::stringCallback != nullptr)
    {
        return QGrepWrapper::stringCallback(gcnew System::String(result));
    }

    return false;
}

void qgrepInterop::NativeQGrepWrapper::nativeProgressCallback(double percentage)
{
    if (QGrepWrapper::progressCalback != nullptr)
    {
        return QGrepWrapper::progressCalback(percentage);
    }
}

void qgrepInterop::NativeQGrepWrapper::nativeErrorsCallback(const char* error)
{
    if (QGrepWrapper::errorCallback != nullptr)
    {
        QGrepWrapper::errorCallback(gcnew System::String(error));
    }
}

void qgrepInterop::NativeQGrepWrapper::nativeLocalizedStringCallback(const char** result, int size)
{
    if (QGrepWrapper::localizedStringCalback != nullptr)
    {
        List<String^>^ stringsList = gcnew List<String^>();
        for (int i = 0; i < size; i++)
        {
            stringsList->Add(gcnew String(result[i]));
        }

        QGrepWrapper::localizedStringCalback(stringsList);
    }
}