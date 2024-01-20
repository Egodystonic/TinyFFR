// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Windowing;
using System.Text;

namespace Egodystonic.TinyFFR.Interop;

static unsafe class NativeUtils {
	public const string NativeLibName = "TinyFFR.Native";
	const int NativeErrorBufferLength = 1001;

	[DllImport(NativeLibName, EntryPoint = "get_err_buffer")]
	static extern byte* GetErrorBuffer();

	public static string GetLastError() => Encoding.UTF8.GetString(GetErrorBuffer(), NativeErrorBufferLength);
}