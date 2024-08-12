// Created on 2024-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopImplProvider {
	IInputTracker GetInputTracker(ApplicationLoopHandle handle);
	TimeSpan IterateOnce(ApplicationLoopHandle handle);
	bool TryIterateOnce(ApplicationLoopHandle handle, out TimeSpan outDeltaTime);
	TimeSpan GetTimeUntilNextIteration(ApplicationLoopHandle handle);
	TimeSpan GetTotalIteratedTime(ApplicationLoopHandle handle);
	void Dispose(ApplicationLoopHandle handle);
	bool IsDisposed(ApplicationLoopHandle handle);
}