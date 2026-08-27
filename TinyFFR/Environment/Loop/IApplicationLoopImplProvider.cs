// Created on 2024-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopImplProvider : IDisposableResourceImplProvider<ApplicationLoop> {
	ILatestInputRetriever GetInputStateProvider(ResourceHandle<ApplicationLoop> handle);
	bool GetEnableInputTextTranscription(ResourceHandle<ApplicationLoop> handle);
	void SetEnableInputTextTranscription(ResourceHandle<ApplicationLoop> handle, bool enable);
	TimeSpan GetTargetIterationInterval(ResourceHandle<ApplicationLoop> handle);
	void SetTargetIterationInterval(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue);
	float? GetTargetPerFrameAsyncCooperativeTaskTimeFraction(ResourceHandle<ApplicationLoop> handle);
	void SetTargetPerFrameAsyncCooperativeTaskTimeFraction(ResourceHandle<ApplicationLoop> handle, float? newValue);
	float GetFramesPerSecondRecentAverage(ResourceHandle<ApplicationLoop> handle);
	float GetFramesPerSecondLatest(ResourceHandle<ApplicationLoop> handle);
	float GetFramesPerSecondRecentMin(ResourceHandle<ApplicationLoop> handle);
	float GetFramesPerSecondRecentMax(ResourceHandle<ApplicationLoop> handle);
	TimeSpan IterateOnce(ResourceHandle<ApplicationLoop> handle, bool executePendingPrimaryThreadCooperativeTasks);
	bool TryIterateOnce(ResourceHandle<ApplicationLoop> handle, out TimeSpan outDeltaTime, bool executePendingPrimaryThreadCooperativeTasks);
	TimeSpan GetTimeUntilNextIteration(ResourceHandle<ApplicationLoop> handle);
	TimeSpan GetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle);
	void SetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue);
}