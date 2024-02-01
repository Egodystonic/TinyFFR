// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Loop;

interface IApplicationLoopImplProvider {
	DeltaTime IterateOnce(ApplicationLoopHandle handle);
	DeltaTime? TryIterateOnce(ApplicationLoopHandle handle);
	void Dispose(ApplicationLoopHandle handle);
	bool IsDisposed(ApplicationLoopHandle handle);
	IInputTracker GetInputTracker(ApplicationLoopHandle handle);
}