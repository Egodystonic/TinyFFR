// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;

namespace Egodystonic.TinyFFR.Interop;

static unsafe class NativeUtils {
	public const string NativeLibName = "TinyFFR.Native";
	const int NativeErrorBufferLength = 1001;
	static bool _nativeLibInitialized = false;

	[DllImport(NativeLibName, EntryPoint = "get_err_buffer")]
	static extern byte* GetErrorBuffer();

	public static string GetLastError() {
		var asSpan = new ReadOnlySpan<byte>(GetErrorBuffer(), NativeErrorBufferLength);
		var firstZero = asSpan.IndexOf((byte) 0);
		return Encoding.UTF8.GetString(asSpan[..(firstZero >= 0 ? firstZero : NativeErrorBufferLength)]);
	}

	[DllImport(NativeLibName, EntryPoint = "initialize_all")]
	static extern InteropResult InitializeAll();

	public static void InitializeNativeLibIfNecessary() {
		if (_nativeLibInitialized) return;
		InitializeAll().ThrowIfFailure();
		_nativeLibInitialized = true;
	}
}