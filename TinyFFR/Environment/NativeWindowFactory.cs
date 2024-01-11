// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;

namespace Egodystonic.TinyFFR.Environment;

[SuppressUnmanagedCodeSecurity]
static class NativeWindowFactory {
	[DllImport("TinyFFR.Native", EntryPoint = "WindowFactoryCreateWindow")]
	public static extern byte CreateWindow(
			IntPtr failReason
			);
}