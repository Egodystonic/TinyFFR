// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Threading;

interface IWorkSchedulerFacade {
	public void AddJob<TContext, TResult>(WorkerThreadJob<TContext, TResult> job) where TContext : unmanaged;
}

sealed class SynchronousWorkScheduler : IWorkSchedulerFacade {
	public unsafe void AddJob<TContext, TResult>(WorkerThreadJob<TContext, TResult> job) where TContext : unmanaged {
		if (job.WorkerThreadWork == null) throw new ArgumentException("Given job had null worker thread work pointer.", nameof(job));
		InvalidObjectException.ThrowIfDefault(job.SharedOperation);

		try {
			job.SharedOperation.SetResult(job.WorkerThreadWork(job.Context));
		}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're passing it on to the continuation
		catch (Exception e) {
#pragma warning restore CA1031
			job.SharedOperation.SetException(e);
		}
	}
}