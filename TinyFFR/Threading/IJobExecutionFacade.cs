// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

interface IPrimaryThreadDispatcher {
	void ExecutePendingCooperativeJobs(TimeSpan? targetExecutionTimeCap);
	bool BlockPrimaryThreadUntilConditionSatisfied(ManualResetEventSlim mre, IAsyncOperationTrackingData? versionOwner, ulong expectedVersion, TimeSpan timeout, CancellationToken cancellationToken);
	void NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
	bool SchedulePrimaryThreadContinuation(Action continuation);
}

/// <summary>
/// Represents a synchronous or parallelizable/asynchronous job dispatcher; depending on the specific implementation.
/// </summary>
public interface IJobExecutionFacade {
	/// <summary>
	/// Represents a synchronous, single-threaded, non parallelizing job execution facade.
	/// </summary>
	public static IJobExecutionFacade Synchronous { get; } = new SynchronousWorkExecutionFacade(); 
	internal bool HasWorkerThreads { get; }
	internal int WorkerThreadCount { get; }
	internal void AddWorkerThreadJob(ThreadJob job);
	internal void AddPrimaryThreadJob(ThreadJob job);
	internal void AddPrimaryThreadJobAndWait(ThreadJob job);
}

sealed class SynchronousWorkExecutionFacade : IJobExecutionFacade {
	public bool HasWorkerThreads { get; } = false;
	public int WorkerThreadCount { get; } = 0;
	public void AddWorkerThreadJob(ThreadJob job) => job.Execute(null);
	public void AddPrimaryThreadJob(ThreadJob job) => job.Execute(null);
	public void AddPrimaryThreadJobAndWait(ThreadJob job) => job.Execute(null);
}
