// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
unsafe class CooperativeThreadPoolTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	sealed class PrimaryThreadHarness : IDisposable {
		readonly Thread _thread;
		readonly ManualResetEventSlim _ready = new(false);
		readonly ManualResetEventSlim _actionAssigned = new(false);
		readonly ManualResetEventSlim _actionComplete = new(false);
		Action? _action;
		Exception? _error;

		public PrimaryThreadHarness() {
			_thread = new Thread(ThreadEntry) { IsBackground = true, Name = "Test Primary Thread" };
			_thread.Start();
			if (!_ready.Wait(WaitTimeout)) throw new TimeoutException("Primary thread harness did not start.");
		}

		void ThreadEntry() {
			ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);
			_ready.Set();
			_actionAssigned.Wait();
			if (_action == null) return;
			try {
				_action();
			}
#pragma warning disable CA1031
			catch (Exception e) {
#pragma warning restore CA1031
				_error = e;
			}
			finally {
				_actionComplete.Set();
			}
		}

		public TimeSpan Invoke(Action action) {
			_action = action;
			var startTimestamp = Stopwatch.GetTimestamp();
			_actionAssigned.Set();
			if (!_actionComplete.Wait(WaitTimeout)) throw new TimeoutException("Primary thread harness action did not complete.");
			var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
			if (_error != null) throw _error;
			return elapsed;
		}

		public void Dispose() {
			_actionAssigned.Set();
			_thread.Join(WaitTimeout);
			_ready.Dispose();
			_actionAssigned.Dispose();
			_actionComplete.Dispose();
		}
	}

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

	static void ThrowingCompletion(JobContext context, Exception? error, int result) {
		context.Error = error;
		context.IntResult = result;
		context.Done.Set();
		throw new InvalidOperationException("Deliberate test failure.");
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

	sealed class NestedWaitContext {
		public TinyFfrAsyncOperation<int>[] Operations = Array.Empty<TinyFfrAsyncOperation<int>>();
		public int NextOperationIndex;
		public int CurrentNesting;
		public int MaxObservedNesting;
		public Exception? Error;
	}

	static int WaitOnNextOperation(NestedWaitContext context) {
		var index = context.NextOperationIndex++;
		++context.CurrentNesting;
		context.MaxObservedNesting = Int32.Max(context.MaxObservedNesting, context.CurrentNesting);
		try {
			context.Operations[index].WaitForCompletion();
		}
		finally {
			--context.CurrentNesting;
		}
		return index;
	}

	static void RecordNestedResult(NestedWaitContext context, Exception? error, int result) => context.Error = error;

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

	[Test, Timeout(30_000)]
	public void ShouldSurfaceJobExceptionForManagedResultInsteadOfCrashing() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 2 });
		var context = new JobContext();

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextManagedResult(context, &ThrowingManagedWork, &RecordStringResult));

		Assert.IsTrue(context.Done.Wait(WaitTimeout));
		Assert.IsInstanceOf<InvalidOperationException>(context.Error);
		Assert.IsNull(context.StringResult);
	}

	[Test, Timeout(30_000)]
	public void ShouldExecuteWorkerJobsAndReportResults() {
		const int JobCount = 50;

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 });
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

	[Test, Timeout(30_000)]
	public void ShouldExecuteInlineWhenConfiguredWithNoWorkerThreads() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		var context = new JobContext { Input = 5 };

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));

		Assert.IsTrue(context.Done.IsSet);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, context.ExecutingThreadId);
		Assert.AreEqual(10, context.IntResult);
	}

	[Test, Timeout(60_000)]
	public void ShouldDisposePromptlyAndJoinWorkerThreads() {
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4, MaxShutdownWaitTime = TimeSpan.FromSeconds(10d) });

		var startTimestamp = Stopwatch.GetTimestamp();
		pool.Dispose();
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
	}

	[Test, Timeout(30_000)]
	public void ShouldTolerateRepeatedDisposal() {
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 2 });

		pool.Dispose();
		Assert.DoesNotThrow(() => pool.Dispose());
		Assert.Throws<ObjectDisposedException>(() => pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(new JobContext(), &DoubleInput, &RecordIntResult)));
	}

	[Test, Timeout(30_000)]
	public void ShouldRejectDisposalFromNonPrimaryThread() {
		using var primaryThread = new PrimaryThreadHarness();
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });

		Assert.Throws<InvalidOperationException>(() => pool.Dispose());

		primaryThread.Invoke(pool.Dispose);
	}

	[Test, Timeout(30_000)]
	public void ShouldCompletePendingPrimaryThreadJobsAtDisposal() {
		using var primaryThread = new PrimaryThreadHarness();
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		var context = new JobContext { Input = 3 };

		pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));
		Assert.IsFalse(context.Done.IsSet);

		primaryThread.Invoke(pool.Dispose);

		Assert.IsTrue(context.Done.Wait(WaitTimeout));
		Assert.IsNull(context.Error);
		Assert.AreEqual(6, context.IntResult);
	}

	[Test, Timeout(30_000)]
	public void ShouldReleaseWorkerJobsAbandonedAtDisposal() {
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 1, MaxShutdownWaitTime = TimeSpan.Zero });
		var occupyingContext = new JobContext { WorkDuration = TimeSpan.FromSeconds(3d) };
		var abandonedContext = new JobContext { Input = 7 };

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(occupyingContext, &SleepThenDoubleInput, &RecordIntResult));
		Thread.Sleep(200);
		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(abandonedContext, &DoubleInput, &RecordIntResult));

		pool.Dispose();

		Assert.IsTrue(abandonedContext.Done.Wait(WaitTimeout));
		Assert.IsInstanceOf<OperationCanceledException>(abandonedContext.Error);
	}

	[Test, Timeout(30_000)]
	public void ShouldWakePrimaryThreadWithoutWaitingForAPollInterval() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		var context = new JobContext { Input = 21 };

		var submitter = new Thread(() => {
			Thread.Sleep(20);
			pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));
		}) { IsBackground = true };
		submitter.Start();

		var startTimestamp = Stopwatch.GetTimestamp();
		var wasSatisfied = ((IPrimaryThreadDispatcher) pool).BlockPrimaryThreadUntilConditionSatisfied(context.Done, null, 0UL, TimeSpan.FromSeconds(5d), default);
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.IsTrue(submitter.Join(WaitTimeout));
		Assert.IsTrue(wasSatisfied);
		Assert.AreEqual(42, context.IntResult);
		Assert.Less(elapsed, TimeSpan.FromMilliseconds(80d));
	}

	[Test, Timeout(30_000)]
	public void ShouldReportTimeoutWhenConditionIsNeverSatisfied() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		using var neverSetIndicator = new ManualResetEventSlim(false);

		var startTimestamp = Stopwatch.GetTimestamp();
		var wasSatisfied = ((IPrimaryThreadDispatcher) pool).BlockPrimaryThreadUntilConditionSatisfied(neverSetIndicator, null, 0UL, TimeSpan.FromMilliseconds(100d), default);
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.IsFalse(wasSatisfied);
		Assert.GreaterOrEqual(elapsed, TimeSpan.FromMilliseconds(90d));
		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
	}

	[Test, Timeout(30_000)]
	public void ShouldHonourWaitTimeoutWhilePumpingQueuedJobs() {
		const int JobCount = 8;

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		using var neverSetIndicator = new ManualResetEventSlim(false);
		var contexts = new JobContext[JobCount];
		for (var i = 0; i < JobCount; ++i) contexts[i] = new JobContext { Input = i, WorkDuration = TimeSpan.FromMilliseconds(100d) };

		var submitter = new Thread(() => {
			foreach (var context in contexts) {
				pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &SleepThenDoubleInput, &RecordIntResult));
			}
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		var startTimestamp = Stopwatch.GetTimestamp();
		var wasSatisfied = ((IPrimaryThreadDispatcher) pool).BlockPrimaryThreadUntilConditionSatisfied(neverSetIndicator, null, 0UL, TimeSpan.FromMilliseconds(100d), default);
		var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

		Assert.IsFalse(wasSatisfied);
		Assert.Less(elapsed, TimeSpan.FromMilliseconds(500d));
	}

	[Test, Timeout(30_000)]
	public void ShouldNotLetAThrowingCompletionEscapeOrStrandThePump() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		var throwingContext = new JobContext { Input = 1 };
		var subsequentContext = new JobContext { Input = 2 };

		var submitter = new Thread(() => {
			pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(throwingContext, &DoubleInput, &ThrowingCompletion));
			pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(subsequentContext, &DoubleInput, &RecordIntResult));
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		Assert.DoesNotThrow(() => ((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(null));

		Assert.IsTrue(throwingContext.Done.IsSet);
		Assert.IsTrue(subsequentContext.Done.IsSet);
		Assert.AreEqual(4, subsequentContext.IntResult);
	}

	[Test, Timeout(60_000)]
	public void ShouldNotDropJobsWhenSubmissionRacesDisposal() {
		const int AttemptCount = 20;
		const int JobsPerAttempt = 20;

		for (var attempt = 0; attempt < AttemptCount; ++attempt) {
			var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 2 });
			var contexts = new JobContext[JobsPerAttempt];
			for (var i = 0; i < JobsPerAttempt; ++i) contexts[i] = new JobContext { Input = i };

			var unexpectedException = (Exception?) null;
			var submitter = new Thread(() => {
				foreach (var context in contexts) {
					try {
						pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &DoubleInput, &RecordIntResult));
					}
					catch (ObjectDisposedException) { }
#pragma warning disable CA1031
					catch (Exception e) {
#pragma warning restore CA1031
						unexpectedException = e;
						return;
					}
				}
			}) { IsBackground = true };

			submitter.Start();
			pool.Dispose();
			Assert.IsTrue(submitter.Join(WaitTimeout));

			Assert.IsNull(unexpectedException);
			foreach (var context in contexts) {
				Assert.IsTrue(context.Done.Wait(WaitTimeout), "A job was neither executed nor cancelled.");
			}
		}
	}

	[Test, Timeout(60_000)]
	public void ShouldDisposePromptlyWhileWorkerIsBlockedOnPrimaryThreadHandoff() {
		using var primaryThread = new PrimaryThreadHarness();
		var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 1, MaxShutdownWaitTime = TimeSpan.FromSeconds(10d) });
		var context = new JobContext { Pool = pool };

		pool.AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &HandOffToPrimaryThread, &RecordIntResult));
		Assert.IsTrue(context.HandoffStarted.Wait(WaitTimeout));
		Thread.Sleep(200);

		var elapsed = primaryThread.Invoke(pool.Dispose);

		Assert.Less(elapsed, TimeSpan.FromSeconds(2d));
		Assert.IsTrue(context.Done.Wait(WaitTimeout));
		Assert.IsNull(context.Error);
	}

	[Test, Timeout(60_000)]
	public void ShouldRespectPrimaryThreadPumpTimeBudget() {
		const int JobCount = 10;

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
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
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
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

	[Test, Timeout(60_000)]
	public void ShouldNotBusyWaitWhenCooperativePumpingHitsMaxNestingDepth() {
		const int ExpectedHydrationDepthCap = 8;
		const int QueuedJobCount = ExpectedHydrationDepthCap + 4;
		var unblockDelay = TimeSpan.FromSeconds(1d);

		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
		var context = new NestedWaitContext { Operations = new TinyFfrAsyncOperation<int>[QueuedJobCount] };
		for (var i = 0; i < QueuedJobCount; ++i) context.Operations[i] = new TinyFfrAsyncOperation<int>(pool);

		var submitter = new Thread(() => {
			for (var i = 0; i < QueuedJobCount; ++i) {
				pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &WaitOnNextOperation, &RecordNestedResult));
			}
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		var unblocker = new Thread(() => {
			Thread.Sleep(unblockDelay);
			for (var i = QueuedJobCount - 1; i >= 0; --i) context.Operations[i].SetResult(i);
		}) { IsBackground = true };
		unblocker.Start();

		var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
		var wallStart = Stopwatch.GetTimestamp();
		((IPrimaryThreadDispatcher) pool).ExecutePendingCooperativeJobs(null);
		var wallElapsed = Stopwatch.GetElapsedTime(wallStart);
		var cpuElapsed = Process.GetCurrentProcess().TotalProcessorTime - cpuBefore;

		Assert.IsTrue(unblocker.Join(WaitTimeout));
		Assert.IsNull(context.Error);
		Assert.AreEqual(ExpectedHydrationDepthCap, context.MaxObservedNesting);
		Assert.AreEqual(QueuedJobCount, context.NextOperationIndex);
		Assert.GreaterOrEqual(wallElapsed, unblockDelay);
		Assert.Less(cpuElapsed, wallElapsed * 0.4d);

		for (var i = 0; i < QueuedJobCount; ++i) _ = context.Operations[i].GetResultAndDisposeOperation();
	}
}
