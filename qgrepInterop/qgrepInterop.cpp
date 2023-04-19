#include "pch.h"

#include <vector>
#include <string>

#include <msclr\marshal.h>
#include <msclr\marshal_cppstd.h>

#include "qgrep.hpp"

#include "qgrepInterop.h"

System::String^ qgrepInterop::QGrepWrapper::CallQGrep(System::Collections::Generic::List<System::String^>^ arguments, System::String^% errors)
{
    std::string unmanagedArguments;
    const char* unmanagedErrors = nullptr;

    for each (System::String^ argument in arguments)
    {
        unmanagedArguments += msclr::interop::marshal_as<std::string>(argument) + "\n";
    }

    const char* unmanagedResults = qgrepWrapper(const_cast<char*>(unmanagedArguments.c_str()), unmanagedArguments.size(), unmanagedErrors);

    errors = gcnew System::String(unmanagedErrors);
    return gcnew System::String(unmanagedResults);
}

void qgrepInterop::QGrepWrapper::CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, Callback^ cb, Callback^ errorsCb)
{
    std::string unmanagedArguments;

    for each (System::String^ argument in arguments)
    {
        unmanagedArguments += msclr::interop::marshal_as<std::string>(argument) + "\n";
    }

    callback = cb;
    errorsCallback = errorsCb;

    qgrepWrapperAsync(const_cast<char*>(unmanagedArguments.c_str()), unmanagedArguments.size(), &NativeQGrepWrapper::nativeCallback, &NativeQGrepWrapper::nativeErrorsCallback);
}

void qgrepInterop::NativeQGrepWrapper::nativeCallback(const char* result, int size)
{
    if (QGrepWrapper::callback != nullptr)
    {
        QGrepWrapper::callback(gcnew System::String(result, 0, size));
    }
}

void qgrepInterop::NativeQGrepWrapper::nativeErrorsCallback(const char* error, int size)
{
    if (QGrepWrapper::errorsCallback != nullptr)
    {
        QGrepWrapper::errorsCallback(gcnew System::String(error, 0, size));
    }
}
