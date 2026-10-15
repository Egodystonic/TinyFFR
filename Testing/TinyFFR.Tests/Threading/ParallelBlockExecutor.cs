// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
unsafe class ParallelBlockExecutorTest {
	[SetUp]
	public void SetUpTest() => ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);

	[TearDown]
	public void TearDownTest() => ThreadSafetyTracker.ClearPrimaryThread();

	sealed class CoverageContext {
		public required int[] VisitCounts { get; init; }
		public ConcurrentDictionary<int, byte> ThreadIds { get; } = new();
		public int BlockCount;
		public int SleepMillisPerBlock { get; init; }
		public int ThrowAtIndex { get; init; } = -1;
	}

	static void RecordCoverage(int start, int count, CoverageContext context) {
		Interlocked.Increment(ref context.BlockCount);
		context.ThreadIds.TryAdd(System.Environment.CurrentManagedThreadId, 0);
		if (context.SleepMillisPerBlock > 0) Thread.Sleep(context.SleepMillisPerBlock);
		for (var i = start; i < start + count; ++i) {
			if (i == context.ThrowAtIndex) throw new InvalidOperationException("Test failure " + i);
			Interlocked.Increment(ref context.VisitCounts[i]);
		}
	}

	static void RecordRowCoverage(XYPair<int> offset, XYPair<int> dimensions, CoverageContext context) {
		Assert.AreEqual(0, offset.X);
		for (var y = offset.Y; y < offset.Y + dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) Interlocked.Increment(ref context.VisitCounts[y * dimensions.X + x]);
		}
	}

	[Test]
	public void ShouldVisitEveryIndexExactlyOnce() {
		foreach (var workerCount in new[] { 0, 1, 3, 8 }) {
			using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = workerCount });
			foreach (var itemCount in new[] { 0, 1, 2, 7, 64, 1000, 4097, 65_536, 1_000_003 }) {
				foreach (var minimumPerBlock in new[] { 1, 3, 4096 }) {
					var context = new CoverageContext { VisitCounts = new int[itemCount] };
					ParallelBlockExecutor.Execute(pool, itemCount, minimumPerBlock, &RecordCoverage, context);
					Assert.IsTrue(context.VisitCounts.All(c => c == 1), $"Workers {workerCount}, count {itemCount}, min {minimumPerBlock}");
					if (itemCount > 0 && itemCount < minimumPerBlock * 2) Assert.AreEqual(1, context.BlockCount);
				}
			}
		}
	}

	[Test]
	public void ShouldVisitEveryTexelExactlyOnceInRowBands() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 5 });
		foreach (var dimensions in new XYPair<int>[] { (0, 10), (10, 0), (1, 1), (1, 999), (999, 1), (1920, 1080), (17, 513) }) {
			foreach (var minimumRows in new[] { 1, 16 }) {
				var context = new CoverageContext { VisitCounts = new int[dimensions.Area] };
				ParallelBlockExecutor.ExecuteRows(pool, dimensions, minimumRows, &RecordRowCoverage, context);
				Assert.IsTrue(context.VisitCounts.All(c => c == 1), $"{dimensions}, min rows {minimumRows}");
			}
		}
	}

	[Test]
	public void ShouldRunInlineWithSynchronousFacade() {
		var context = new CoverageContext { VisitCounts = new int[100_000] };
		ParallelBlockExecutor.Execute(IJobExecutionFacade.Synchronous, context.VisitCounts.Length, 1, &RecordCoverage, context);
		Assert.AreEqual(1, context.BlockCount);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, context.ThreadIds.Keys.Single());
		Assert.IsTrue(context.VisitCounts.All(c => c == 1));
	}

	[Test]
	public void ShouldUseWorkerThreads() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 });
		var context = new CoverageContext { VisitCounts = new int[20], SleepMillisPerBlock = 20 };
		ParallelBlockExecutor.Execute(pool, context.VisitCounts.Length, 1, &RecordCoverage, context);
		Assert.Greater(context.ThreadIds.Count, 1);
		Assert.IsTrue(context.VisitCounts.All(c => c == 1));
	}

	[Test]
	public void ShouldPropagateExceptionsToTheCallerAndReleaseSlots() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 });
		for (var i = 0; i < ParallelBlockExecutor.SlotCount * 2; ++i) {
			var context = new CoverageContext { VisitCounts = new int[10_000], ThrowAtIndex = (i * 37) % 10_000 };
			var thrown = Assert.Throws<InvalidOperationException>(() => ParallelBlockExecutor.Execute(pool, context.VisitCounts.Length, 1, &RecordCoverage, context));
			StringAssert.StartsWith("Test failure", thrown!.Message);
		}

		var finalContext = new CoverageContext { VisitCounts = new int[20], SleepMillisPerBlock = 20 };
		ParallelBlockExecutor.Execute(pool, finalContext.VisitCounts.Length, 1, &RecordCoverage, finalContext);
		Assert.Greater(finalContext.ThreadIds.Count, 1, "Slots appear to have leaked: the executor fell back to serial execution.");
	}

	sealed class NestedJob {
		public required CooperativeThreadPool Pool { get; init; }
		public required CountdownEvent Countdown { get; init; }
		public int[] VisitCounts { get; } = new int[50_000];
		public Exception? Error { get; set; }
	}

	static void IncrementRange(int start, int count, NestedJob job) {
		for (var i = start; i < start + count; ++i) Interlocked.Increment(ref job.VisitCounts[i]);
	}

	static byte RunNested(NestedJob? job) {
		ParallelBlockExecutor.Execute(job!.Pool, job.VisitCounts.Length, 16, &IncrementRange, job);
		return 0;
	}

	static void CompleteNested(NestedJob? job, Exception? error, byte _) {
		job!.Error = error;
		job.Countdown.Signal();
	}

	[Test, Timeout(60_000)]
	public void ShouldNotDeadlockWhenExecutingFromTheWorkerThreadsItDispatchesTo() {
		const int WorkerCount = 4;
		const int JobCount = WorkerCount * 8;
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = WorkerCount });
		using var countdown = new CountdownEvent(JobCount);
		var jobs = new NestedJob[JobCount];
		for (var i = 0; i < JobCount; ++i) {
			jobs[i] = new NestedJob { Pool = pool, Countdown = countdown };
			((IJobExecutionFacade) pool).AddWorkerThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult<NestedJob, byte>(jobs[i], &RunNested, &CompleteNested));
		}

		Assert.IsTrue(countdown.Wait(TimeSpan.FromSeconds(30)), "Nested executions did not complete (possible deadlock).");
		foreach (var job in jobs) {
			Assert.IsNull(job.Error, job.Error?.ToString());
			Assert.IsTrue(job.VisitCounts.All(c => c == 1));
		}
	}

	sealed class ReferenceHolder {
		public object? Payload;
	}

	static void NoOp(int start, int count, ReferenceHolder context) { }

	[Test]
	public void ShouldNotRetainContextsAfterCompletion() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 });
		var weakRef = ExecuteWithTrackedPayload(pool);
		var spinWait = new SpinWait();
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
		while (DateTime.UtcNow < deadline) {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (!weakRef.IsAlive) break;
			spinWait.SpinOnce();
		}
		Assert.IsFalse(weakRef.IsAlive);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference ExecuteWithTrackedPayload(CooperativeThreadPool pool) {
		var holder = new ReferenceHolder { Payload = new object() };
		ParallelBlockExecutor.Execute(pool, 100_000, 1, &NoOp, holder);
		return new WeakReference(holder);
	}

	struct UnmanagedSumContext {
		public long* Destination;
	}

	static void SumRange(int start, int count, UnmanagedSumContext context) {
		long sum = 0;
		for (var i = start; i < start + count; ++i) sum += i;
		Interlocked.Add(ref *context.Destination, sum);
	}

	[Test]
	public void ShouldNotAllocateOnTheCallingThreadWhenWarm() {
		using var pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 4 });
		long sum = 0;
		var context = new UnmanagedSumContext { Destination = &sum };
		const int ItemCount = 1_000_000;
		for (var i = 0; i < 5; ++i) ParallelBlockExecutor.Execute(pool, ItemCount, 1024, &SumRange, context);

		sum = 0;
		var before = GC.GetAllocatedBytesForCurrentThread();
		ParallelBlockExecutor.Execute(pool, ItemCount, 1024, &SumRange, context);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual((long) ItemCount * (ItemCount - 1) / 2, sum);
		Assert.AreEqual(0L, allocated);
	}
}
