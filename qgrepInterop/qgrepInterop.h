#pragma once

using namespace System;

namespace qgrepInterop {

    class NativeQGrepWrapper
    {
    public:
        static void nativeCallback(const char* result, int size);
        static void nativeErrorsCallback(const char* result, int size);
    };

	public ref class QGrepWrapper
	{
	public:
		static System::String^ CallQGrep(System::Collections::Generic::List<System::String^>^ arguments, System::String^% errors);

        delegate void Callback(String^ result);
        static void CallQGrepAsync(System::Collections::Generic::List<System::String^>^ arguments, Callback^ cb, Callback^ errorsCb);

		static Callback^ callback = nullptr;
		static Callback^ errorsCallback = nullptr;
	};
}
