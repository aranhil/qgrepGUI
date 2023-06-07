#include <vector>
#include <string>

#include <msclr\marshal.h>
#include <msclr\marshal_cppstd.h>

#include <qgrep.hpp>

#include "qgrepInterop.h"

void qgrepInterop::QGrepWrapper::CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, StringCallback^ stringCb, CheckForceStoppedCallback^ checkStoppedCb, ProgressCalback^ progressCb, LocalizedStringCallback^ localizedStringCb)
{
    std::string unmanagedArguments;

    for each (System::String^ argument in arguments)
    {
        unmanagedArguments += msclr::interop::marshal_as<std::string>(argument) + "\n";
    }

    stringCallback = stringCb;
    checkStoppedCalback = checkStoppedCb;
    progressCalback = progressCb;
    localizedStringCalback = localizedStringCb;

    qgrepWrapperAsync(const_cast<char*>(unmanagedArguments.c_str()), (int)unmanagedArguments.size(), &NativeQGrepWrapper::nativeStringCallback, &NativeQGrepWrapper::nativeCheckForceStopped, &NativeQGrepWrapper::nativeProgressCallback, &NativeQGrepWrapper::nativeLocalizedStringCallback);
}

void qgrepInterop::NativeQGrepWrapper::nativeStringCallback(const char* result)
{
    if (QGrepWrapper::stringCallback != nullptr)
    {
        QGrepWrapper::stringCallback(gcnew System::String(result));
    }
}

void qgrepInterop::NativeQGrepWrapper::nativeProgressCallback(double percentage)
{
    if (QGrepWrapper::progressCalback != nullptr)
    {
        return QGrepWrapper::progressCalback(percentage);
    }
}

bool qgrepInterop::NativeQGrepWrapper::nativeCheckForceStopped()
{
    if (QGrepWrapper::checkStoppedCalback != nullptr)
    {
        return QGrepWrapper::checkStoppedCalback();
    }

    return false;
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