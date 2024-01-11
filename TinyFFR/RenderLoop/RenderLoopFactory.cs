// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Threading;

namespace Egodystonic.TinyFFR.RenderLoop;

public static class RenderLoopFactory {
	public static IDisposable StartRenderLoop(IRenderLoopPlugin plugin) {
		while (true) {
			Thread.Sleep(16);
			plugin.Tick(TimeSpan.FromMilliseconds(16));
		}
	}
}