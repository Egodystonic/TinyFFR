// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
unsafe class CooperativeThreadPoolTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	sealed class JobContext {
		public int Input;
		public int IntResult;
		public string? StringResult;
		public Exception? Error;
		public int ExecutingThreadId;
		public CooperativeThreadPool? Pool;
		public TimeSpan WorkDuration;
		public readonly ManualResetEventSlim Done = new(false);
		public readonly ManualResetEventSlim HandoffStarted = new(false);
	}

	static int DoubleInput(JobContext context) {
		context.ExecutingThreadId = System.Environment.CurrentManagedThreadId;
		return context.Input * 2;
	}

	static void RecordIntResult(JobContext context, Exception? error, int result) {
		context.Error = error;
		context.IntResult = result;
		context.Done.Set();
	}

	static int SleepThenDoubleInput(JobContext context) {
		context.ExecutingThreadId = System.Environment.CurrentManagedThreadId;
		if (context.WorkDuration > TimeSpan.Zero) Thread.Sleep(context.WorkDuration);
		return context.Input * 2;
	}

	static int NoOpWork(JobContext context) => 0;

	static void NoOpCompletion(JobContext context, Exception? error, int result) { }

	static int HandOffToPrimaryThread(JobContext context) {
		context.HandoffStarted.Set();
		context.Pool!.AddPrimaryThreadJobAndWait(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &NoOpWork, &NoOpCompletion));
		return 0;
	}

	static string ThrowingManagedWork(JobContext context) => throw new InvalidOperationException("Deliberate test failure.");

	static void RecordStringResult(JobContext context, Exception? error, string result) {
		context.Error = error;
		context.StringResult = result;
		context.Done.Set();
	}

	[SetUp]
	public void SetUpTest() => ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);

	[TearDown]
	public void TearDownTest() => ThreadSafetyTracker.ClearPrimaryThread();

	// (Plan step 7, covers finding 1) A job that throws used to reach GCHandle.FromIntPtr(0) inside its completion,
	// which threw from within Execute's catch block and killed the process via the worker loop's rethrow.
	[Test, Timeout(30_000)]
	public void ShouldSurfaceJobExceptionForManagedResultInsteadOfCrashing() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 2 }, Thread.CurrentThread);
		var context = new JobContext();

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextManagedResult(context, &ThrowingManagedWork, &RecordStringResult));

		Assert.IsTrue(context.Done.Wait(WaitTimeout));
		Assert.IsInstanceOf<InvalidOperationException>(context.Error);
		Assert.IsNull(context.StringResult);
	}

	[Test, Timeout(30_000)]
	public void ShouldExecuteWorkerJobsAndReportResults() {
		const int JobCount = 50;

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 }, Thread.CurrentThread);
		var contexts = new JobContext[JobCount];

		for (var i = 0; i < JobCount; ++i) {
			contexts[i] = new JobContext { Input = i };
			pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(contexts[i], &DoubleInput, &RecordIntResult));
		}

		for (var i = 0; i < JobCount; ++i) {
			Assert.IsTrue(contexts[i].Done.Wait(WaitTimeout));
			Assert.IsNull(contexts[i].Error);
			Assert.AreEqual(i * 2, contexts[i].IntResult);
		}
	}

	// (Plan step 7, covers finding 4) HasWorkerThreads was computed but never acted on, so a zero-thread pool enqueued
	// worker jobs to a queue with no consumers at all and they simply never ran.
	[Test, Timeout(30_000)]
	public void ShouldExecuteInlineWhenConfiguredWithNoWorkerThreads() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		var context = new JobContext { Input = 5 };

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));

		Assert.IsTrue(context.Done.IsSet);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, context.ExecutingThreadId);
		Assert.AreEqual(10, context.IntResult);
	}

	// (Plan step 7, covers finding 2) Dispose never cancelled anything and joined before disposing the queues that
	// would have woken the workers, so it always burned the entire MaxShutdownWaitTime and then skipped its own cleanup.
	[Test, Timeout(60_000)]
	public void ShouldDisposePromptlyAndJoinWorkerThreads() {
		var pool = new CooperativeThreadPool(
			new ThreadingConfig { WorkerThreadCount = 4, MaxShutdownWaitTime = TimeSpan.FromSeconds(10d) },
			Thread.CurrentThread
		);

		var startTimestamp = Stopwatch.GetTimestamp();
		pool.Dispose();
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
	}

	[Test, Timeout(30_000)]
	public void ShouldTolerateRepeatedDisposal() {
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 2 }, Thread.CurrentThread);

		pool.Dispose();
		Assert.DoesNotThrow(() => pool.Dispose());
		Assert.Throws<ObjectDisposedException>(() => pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(new JobContext(), &DoubleInput, &RecordIntResult)));
	}

	// (Plan step 7, covers findings 6 and 17) Jobs still queued at disposal used to be dropped silently: their waiters
	// were never released and the context GCHandle allocated at creation time leaked permanently.
	[Test, Timeout(30_000)]
	public void ShouldReleasePrimaryThreadJobsAbandonedAtDisposal() {
		// A primary thread that never pumps, so the job is guaranteed to still be queued when we dispose.
		var neverPumpingPrimaryThread = new Thread(static () => { }) { IsBackground = true };
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, neverPumpingPrimaryThread);
		var context = new JobContext { Input = 3 };

		pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));
		Assert.IsFalse(context.Done.IsSet);

		pool.Dispose();

		Assert.IsTrue(context.Done.Wait(WaitTimeout));
		Assert.IsInstanceOf<OperationCanceledException>(context.Error);
	}

	// (Plan step 7, covers plan step 6) With the old 100ms poll the primary thread sat inside mre.Wait and could not
	// notice a newly-enqueued primary job until that wait expired, so this took ~100ms rather than ~20ms.
	[Test, Timeout(30_000)]
	public void ShouldWakePrimaryThreadWithoutWaitingForAPollInterval() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		var context = new JobContext { Input = 21 };

		var submitter = new Thread(() => {
			Thread.Sleep(20);
			pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));
		}) { IsBackground = true };
		submitter.Start();

		var startTimestamp = Stopwatch.GetTimestamp();
		var wasSatisfied = ((IPrimaryThreadDispatcher) pool).BlockPrimaryThreadUntilConditionSatisfied(context.Done, TimeSpan.FromSeconds(5d), default);
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.IsTrue(submitter.Join(WaitTimeout));
		Assert.IsTrue(wasSatisfied);
		Assert.AreEqual(42, context.IntResult);
		Assert.Less(elapsed, TimeSpan.FromMilliseconds(80d));
	}

	[Test, Timeout(30_000)]
	public void ShouldReportTimeoutWhenConditionIsNeverSatisfied() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		using var neverSetIndicator = new ManualResetEventSlim(false);

		var startTimestamp = Stopwatch.GetTimestamp();
		var wasSatisfied = ((IPrimaryThreadDispatcher) pool).BlockPrimaryThreadUntilConditionSatisfied(neverSetIndicator, TimeSpan.FromMilliseconds(100d), default);
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.IsFalse(wasSatisfied);
		Assert.GreaterOrEqual(elapsed, TimeSpan.FromMilliseconds(90d));
		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
	}

	[Test, Timeout(60_000)]
	public void ShouldDisposePromptlyWhileWorkerIsBlockedOnPrimaryThreadHandoff() {
		var neverPumpingPrimaryThread = new Thread(static () => { }) { IsBackground = true };
		var pool = new CooperativeThreadPool(
			new ThreadingConfig { WorkerThreadCount = 1, MaxShutdownWaitTime = TimeSpan.FromSeconds(10d) },
			neverPumpingPrimaryThread
		);
		var context = new JobContext { Pool = pool };

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &HandOffToPrimaryThread, &RecordIntResult));
		Assert.IsTrue(context.HandoffStarted.Wait(WaitTimeout));
		Thread.Sleep(200);

		var startTimestamp = Stopwatch.GetTimestamp();
		pool.Dispose();
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
	}

	[Test, Timeout(60_000)]
	public void ShouldRespectPrimaryThreadPumpTimeBudget() {
		const int JobCount = 10;

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		var contexts = new JobContext[JobCount];
		for (var i = 0; i < JobCount; ++i) contexts[i] = new JobContext { Input = i, WorkDuration = TimeSpan.FromMilliseconds(30d) };

		var submitter = new Thread(() => {
			foreach (var context in contexts) {
				pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &SleepThenDoubleInput, &RecordIntResult));
			}
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(TimeSpan.FromMilliseconds(90d));

		var budgetedCompletionCount = contexts.Count(context => context.Done.IsSet);
		Assert.Greater(budgetedCompletionCount, 0);
		Assert.Less(budgetedCompletionCount, JobCount);

		((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(null);
		Assert.AreEqual(JobCount, contexts.Count(context => context.Done.IsSet));
		for (var i = 0; i < JobCount; ++i) Assert.AreEqual(i * 2, contexts[i].IntResult);
	}

	[Test, Timeout(60_000)]
	public void ShouldExecuteAtLeastOneJobPerPumpRegardlessOfBudget() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		var contexts = new[] {
			new JobContext { WorkDuration = TimeSpan.FromMilliseconds(50d) },
			new JobContext { WorkDuration = TimeSpan.FromMilliseconds(50d) }
		};

		var submitter = new Thread(() => {
			foreach (var context in contexts) {
				pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &SleepThenDoubleInput, &RecordIntResult));
			}
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(TimeSpan.FromTicks(1L));
		Assert.AreEqual(1, contexts.Count(context => context.Done.IsSet));

		((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(TimeSpan.FromTicks(1L));
		Assert.AreEqual(2, contexts.Count(context => context.Done.IsSet));
	}
}

[TestFixture]
unsafe class TinyFfrAsyncOperationTest {
	CooperativeThreadPool _pool = null!;
	IPrimaryThreadDispatcher Dispatcher => _pool;

	[SetUp]
	public void SetUpTest() {
		ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);
		_pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
	}

	[TearDown]
	public void TearDownTest() {
		_pool.Dispose();
		ThreadSafetyTracker.ClearPrimaryThread();
	}

	// (Plan step 7, covers finding 11) Every one of these calls goes through ThrowIfIncorrectCycleCount while holding
	// the lock in write mode, which tripped the old Debug.Assert(_rwls.IsReadLockHeld) in every Debug build.
	[Test, Timeout(30_000)]
	public void ShouldCompleteOperationWithResult() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);

		Assert.IsFalse(operation.IsCompleted);
		operation.SetResult(7);
		Assert.IsTrue(operation.IsCompleted);
		Assert.AreEqual(7, operation.GetResultAndDisposeOperation());
	}

	[Test, Timeout(30_000)]
	public void ShouldPropagateOperationException() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);

		operation.SetException(new InvalidOperationException("Deliberate test failure."));

		Assert.IsTrue(operation.IsCompleted);
		Assert.Throws<AggregateException>(() => operation.GetResultAndDisposeOperation());
	}

	// (Plan step 7, covers finding 7) A timed-out operation used to be handed back to the tracker pool from a finally
	// block even though Clear() had never run, so its cycle count was unchanged: the still-running job's SetResult then
	// passed the cycle check and wrote its result on to whichever operation had since re-rented that tracker. The pool
	// is LIFO, so under the old behaviour the very next rental below would be the timed-out operation's own tracker.
	[Test, Timeout(30_000)]
	public void ShouldNotRecycleTrackingDataForTimedOutOperation() {
		const int SubsequentOperationCount = 32;

		var timedOutOperation = new TinyFfrAsyncOperation<int>(Dispatcher);
		Assert.IsFalse(timedOutOperation.GetResultAndDisposeOperation(TimeSpan.FromMilliseconds(50d), default, out _));

		var subsequentOperations = new TinyFfrAsyncOperation<int>[SubsequentOperationCount];
		for (var i = 0; i < SubsequentOperationCount; ++i) subsequentOperations[i] = new TinyFfrAsyncOperation<int>(Dispatcher);

		timedOutOperation.SetResult(11);

		foreach (var subsequentOperation in subsequentOperations) {
			Assert.IsFalse(subsequentOperation.IsCompleted);
		}

		Assert.IsTrue(timedOutOperation.IsCompleted);
		Assert.AreEqual(11, timedOutOperation.GetResultAndDisposeOperation());
	}

	[Test, Timeout(30_000)]
	public void ShouldWaitForAllOperationsToComplete() {
		var first = new TinyFfrAsyncOperation<int>(Dispatcher);
		var second = new TinyFfrAsyncOperation<int>(Dispatcher);

		Assert.IsFalse(TinyFfrAsyncOperation.WaitForAllToComplete(TimeSpan.FromMilliseconds(50d), first, second));

		first.SetResult(1);
		second.SetResult(2);

		Assert.IsTrue(TinyFfrAsyncOperation.WaitForAllToComplete(TimeSpan.FromSeconds(5d), first, second));
		Assert.AreEqual(1, first.GetResultAndDisposeOperation());
		Assert.AreEqual(2, second.GetResultAndDisposeOperation());
	}

	// (Plan step 7, covers finding 19) The old implementation used 'operation != operations[^1]' as a position test,
	// which is structural equality on a record struct: repeating one operation stopped the timeout being decremented.
	[Test, Timeout(30_000)]
	public void ShouldRespectTimeoutWhenTheSameOperationIsRepeated() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		TinyFfrAsyncOperation asNonGeneric = operation;

		var startTimestamp = Stopwatch.GetTimestamp();
		Assert.IsFalse(TinyFfrAsyncOperation.WaitForAllToComplete(TimeSpan.FromMilliseconds(100d), asNonGeneric, asNonGeneric, asNonGeneric));
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));

		operation.SetResult(0);
		_ = operation.GetResultAndDisposeOperation();
	}

	// (Plan step 7, covers finding 20) IsCompleted was the one member that dereferenced its tracking data unguarded.
	[Test, Timeout(30_000)]
	public void ShouldRejectDefaultOperations() {
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation<int>).IsCompleted);
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation).IsCompleted);
		Assert.Throws<InvalidObjectException>(() => default(TinyFfrAsyncOperation<int>).WaitForCompletion());
		Assert.Throws<InvalidObjectException>(() => default(TinyFfrAsyncOperation).WaitForCompletion());
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation<int>).GetResultAndDisposeOperation());
	}
}
