// Created on 2024-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="ApplicationLoop"/> resources.
/// </summary>
public interface IApplicationLoopImplProvider : IDisposableResourceImplProvider<ApplicationLoop> {
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.Input"/>.
	/// </summary>
	ILatestInputRetriever GetInputStateProvider(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.EnableInputTextTranscription"/>.
	/// </summary>
	bool GetEnableInputTextTranscription(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.EnableInputTextTranscription"/>.
	/// </summary>
	void SetEnableInputTextTranscription(ResourceHandle<ApplicationLoop> handle, bool enable);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TargetIterationInterval"/> (and, indirectly, <see cref="ApplicationLoop.TargetFrameRate"/>).
	/// </summary>
	TimeSpan GetTargetIterationInterval(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TargetIterationInterval"/> (and, indirectly, <see cref="ApplicationLoop.TargetFrameRate"/>).
	/// </summary>
	void SetTargetIterationInterval(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TargetPerFrameAsyncCooperativeTaskTimeFraction"/>.
	/// </summary>
	float? GetTargetPerFrameAsyncCooperativeTaskTimeFraction(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TargetPerFrameAsyncCooperativeTaskTimeFraction"/>.
	/// </summary>
	void SetTargetPerFrameAsyncCooperativeTaskTimeFraction(ResourceHandle<ApplicationLoop> handle, float? newValue);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.FramesPerSecondRecentAverage"/>.
	/// </summary>
	float GetFramesPerSecondRecentAverage(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.FramesPerSecondLatest"/>.
	/// </summary>
	float GetFramesPerSecondLatest(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.FramesPerSecondRecentMin"/>.
	/// </summary>
	float GetFramesPerSecondRecentMin(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.FramesPerSecondRecentMax"/>.
	/// </summary>
	float GetFramesPerSecondRecentMax(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.IterateOnce"/>.
	/// </summary>
	TimeSpan IterateOnce(ResourceHandle<ApplicationLoop> handle, bool executePendingPrimaryThreadCooperativeTasks);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TryIterateOnce"/>.
	/// </summary>
	bool TryIterateOnce(ResourceHandle<ApplicationLoop> handle, out TimeSpan outDeltaTime, bool executePendingPrimaryThreadCooperativeTasks);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TimeUntilNextIteration"/>.
	/// </summary>
	TimeSpan GetTimeUntilNextIteration(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TotalIteratedTime"/>.
	/// </summary>
	TimeSpan GetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle);
	/// <summary>
	/// Invoked via <see cref="ApplicationLoop.TotalIteratedTime"/> (and, indirectly, <see cref="ApplicationLoop.ResetTotalIteratedTime"/>).
	/// </summary>
	void SetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue);
}