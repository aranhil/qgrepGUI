#pragma once

using namespace System;

namespace qgrepInterop {

    class NativeQGrepWrapper
    {
    public:
        static bool nativeStringCallback(const char* result, int size);
        static void nativeProgressCallback(int percentage);
        static void nativeErrorsCallback(const char* result, int size);
    };

	public ref class QGrepWrapper
	{
	public:
        delegate bool StringCallback(String^ result);
        delegate void ErrorCallback(String^ result);
        delegate void ProgressCalback(int percentage);

        static void CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, StringCallback^ stringCb, ErrorCallback^ errorsCb, ProgressCalback^ progressCb);

		static StringCallback^ stringCallback = nullptr;
		static ErrorCallback^ errorCallback = nullptr;
		static ProgressCalback^ progressCalback = nullptr;
	};
}
