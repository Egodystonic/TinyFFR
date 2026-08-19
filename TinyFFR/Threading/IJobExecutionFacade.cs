// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

interface IPrimaryThreadDispatcher {
	void ExecutePendingCooperativeJobs(TimeSpan? targetExecutionTimeCap);
	bool BlockPrimaryThreadUntilConditionSatisfied(ManualResetEventSlim mre, TimeSpan timeout, CancellationToken cancellationToken);
	void NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
	void SchedulePrimaryThreadContinuation(Action continuation);
}

interface IJobExecutionFacade {
	bool HasWorkerThreads { get; }
	public void AddWorkerThreadJob(ThreadJob job);
	public void AddPrimaryThreadJob(ThreadJob job);
	public void AddPrimaryThreadJobAndWait(ThreadJob job);
}

sealed class SynchronousWorkExecutionFacade : IJobExecutionFacade {
	public bool HasWorkerThreads { get; } = false;
	public void AddWorkerThreadJob(ThreadJob job) => job.Execute(null);
	public void AddPrimaryThreadJob(ThreadJob job) => job.Execute(null);
	public void AddPrimaryThreadJobAndWait(ThreadJob job) => job.Execute(null);
}
