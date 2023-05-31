#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace qgrepInterop {

    class NativeQGrepWrapper
    {
    public:
        static bool nativeStringCallback(const char* result);
        static void nativeProgressCallback(double percentage);
        static void nativeErrorsCallback(const char* result);
        static void nativeLocalizedStringCallback(const char** result, int size);
    };

	public ref class QGrepWrapper
	{
	public:
        delegate bool StringCallback(String^ result);
        delegate void ErrorCallback(String^ result);
        delegate void ProgressCalback(double percentage);
        delegate void LocalizedStringCallback(List<String^>^ result);

        static void CallQGrepAsync(List<String^>^ arguments, StringCallback^ stringCb, ErrorCallback^ errorsCb, ProgressCalback^ progressCb, LocalizedStringCallback^ localizedStringCb);

		static StringCallback^ stringCallback = nullptr;
		static ErrorCallback^ errorCallback = nullptr;
		static ProgressCalback^ progressCalback = nullptr;
		static LocalizedStringCallback^ localizedStringCalback = nullptr;
	};
}
