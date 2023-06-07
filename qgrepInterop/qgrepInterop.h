#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace qgrepInterop {

    class NativeQGrepWrapper
    {
    public:
        static void nativeStringCallback(const char* result);
        static void nativeProgressCallback(double percentage);
        static void nativeLocalizedStringCallback(const char** result, int size);
        static bool nativeCheckForceStopped();
    };

	public ref class QGrepWrapper
	{
	public:
        delegate void StringCallback(String^ result);
        delegate void ProgressCalback(double percentage);
        delegate void LocalizedStringCallback(List<String^>^ result);
        delegate bool CheckForceStoppedCallback();

        static void CallQGrepAsync(List<String^>^ arguments, StringCallback^ stringCb, CheckForceStoppedCallback^ checkStoppedCb, ProgressCalback^ progressCb, LocalizedStringCallback^ localizedStringCb);

		static StringCallback^ stringCallback = nullptr;
		static ProgressCalback^ progressCalback = nullptr;
		static LocalizedStringCallback^ localizedStringCalback = nullptr;
		static CheckForceStoppedCallback^ checkStoppedCalback = nullptr;
	};
}
