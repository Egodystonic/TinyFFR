// Created on 2024-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopImplProvider : IDisposableResourceImplProvider<ApplicationLoop> {
	ILatestInputRetriever GetInputStateProvider(ResourceHandle<ApplicationLoop> handle);
	TimeSpan IterateOnce(ResourceHandle<ApplicationLoop> handle);
	bool TryIterateOnce(ResourceHandle<ApplicationLoop> handle, out TimeSpan outDeltaTime);
	TimeSpan GetTimeUntilNextIteration(ResourceHandle<ApplicationLoop> handle);
	TimeSpan GetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle);
	void SetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue);
}