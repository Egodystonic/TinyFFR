// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Loop;

interface IApplicationLoopImplProvider {
	TimeSpan IterateOnce(ApplicationLoopHandle handle);
	TimeSpan? TryIterateOnce(ApplicationLoopHandle handle);
	void Dispose(ApplicationLoopHandle handle);
	bool IsDisposed(ApplicationLoopHandle handle);
}