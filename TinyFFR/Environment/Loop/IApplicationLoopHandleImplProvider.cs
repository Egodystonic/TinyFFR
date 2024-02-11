// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment;

interface IApplicationLoopImplProvider {
	DeltaTime IterateOnce(ApplicationLoopHandle handle);
	bool TryIterateOnce(ApplicationLoopHandle handle, out DeltaTime outDeltaTime);
	TimeSpan GetTimeUntilNextIteration(ApplicationLoopHandle handle);
	TimeSpan GetTotalIteratedTime(ApplicationLoopHandle handle);
	void Dispose(ApplicationLoopHandle handle);
	bool IsDisposed(ApplicationLoopHandle handle);
	IInputTracker GetInputTracker(ApplicationLoopHandle handle);
}