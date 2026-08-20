using System;
using System.Diagnostics;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
unsafe class TinyFfrAsyncOperationTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	sealed class OperationCompletingContext {
		public TinyFfrAsyncOperation<int> Operation;
	}

	static Unused CompleteOperation(OperationCompletingContext? context) {
		context!.Operation.SetResult(5);
		return default;
	}

	static void NoOpOperationCompletion(OperationCompletingContext? context, Exception? error, Unused result) { }

	CooperativeThreadPool _pool = null!;
	IPrimaryThreadDispatcher Dispatcher => _pool;

	[SetUp]
	public void SetUpTest() {
		ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);
		_pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 });
	}

	[TearDown]
	public void TearDownTest() {
		_pool.Dispose();
		OutstandingAsyncOperationRegistry.InvokeDeferredContinuations();
		ThreadSafetyTracker.ClearPrimaryThread();
	}

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

	[Test, Timeout(30_000)]
	public void ShouldRejectDefaultOperations() {
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation<int>).IsCompleted);
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation).IsCompleted);
		Assert.Throws<InvalidObjectException>(() => default(TinyFfrAsyncOperation<int>).WaitForCompletion());
		Assert.Throws<InvalidObjectException>(() => default(TinyFfrAsyncOperation).WaitForCompletion());
		Assert.Throws<InvalidObjectException>(() => _ = default(TinyFfrAsyncOperation<int>).GetResultAndDisposeOperation());
	}

	void PumpPrimaryThreadJobs() => ((IPrimaryThreadDispatcher) _pool).ExecutePendingCooperativeJobs(null);

	[Test, Timeout(30_000)]
	public void ShouldNotBlockForeverWhenAContinuationConsumesTheOperationDuringTheWait() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		var awaiter = operation.GetAwaiter();
		awaiter.OnCompleted(() => _ = awaiter.GetResult());

		var context = new OperationCompletingContext { Operation = operation };
		var submitter = new Thread(() => {
			_pool.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(context, &CompleteOperation, &NoOpOperationCompletion));
		}) { IsBackground = true };
		submitter.Start();
		Assert.IsTrue(submitter.Join(WaitTimeout));

		Assert.IsTrue(operation.WaitForCompletion(TimeSpan.FromSeconds(5d)));
	}

	[Test, Timeout(30_000)]
	public void ShouldIsolateContinuationsFromOneAnother() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		var secondRan = false;
		var thirdRan = false;

		operation.ScheduleContinuation(() => throw new InvalidOperationException("Deliberate test failure."));
		operation.ScheduleContinuation(() => secondRan = true);
		operation.ScheduleContinuation(() => thirdRan = true);

		operation.SetResult(1);
		PumpPrimaryThreadJobs();

		Assert.IsTrue(secondRan);
		Assert.IsTrue(thirdRan);
		Assert.AreEqual(1, operation.GetResultAndDisposeOperation());
	}

	[Test, Timeout(30_000)]
	public void ShouldResumeAwaitContinuationOnPrimaryThread() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		var continuationThreadId = 0;
		var continuationRan = false;

		var awaiter = operation.GetAwaiter();
		awaiter.OnCompleted(() => {
			continuationThreadId = System.Environment.CurrentManagedThreadId;
			continuationRan = true;
		});

		var completer = new Thread(() => operation.SetResult(9)) { IsBackground = true };
		completer.Start();
		Assert.IsTrue(completer.Join(WaitTimeout));

		Assert.IsFalse(continuationRan);

		PumpPrimaryThreadJobs();

		Assert.IsTrue(continuationRan);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, continuationThreadId);
		Assert.AreEqual(9, awaiter.GetResult());
	}

	[Test, Timeout(30_000)]
	public void ShouldResumeOnPrimaryThreadEvenWhenAwaitedFromWorkerThread() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		operation.SetResult(11);

		var awaiterReportedCompleted = true;
		var continuationThreadId = 0;
		var continuationRan = false;

		var awaitingThread = new Thread(() => {
			var awaiter = operation.GetAwaiter();
			awaiterReportedCompleted = awaiter.IsCompleted;
			awaiter.OnCompleted(() => {
				continuationThreadId = System.Environment.CurrentManagedThreadId;
				continuationRan = true;
			});
		}) { IsBackground = true };
		awaitingThread.Start();
		Assert.IsTrue(awaitingThread.Join(WaitTimeout));

		Assert.IsFalse(awaiterReportedCompleted);
		Assert.IsFalse(continuationRan);

		PumpPrimaryThreadJobs();

		Assert.IsTrue(continuationRan);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, continuationThreadId);
	}

	[Test, Timeout(30_000)]
	public void ShouldRecycleTrackingDataWhenAwaited() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		operation.SetResult(5);

		Assert.AreEqual(1, OutstandingAsyncOperationRegistry.OutstandingCount);

		var awaiter = operation.GetAwaiter();
		Assert.IsTrue(awaiter.IsCompleted);
		Assert.AreEqual(5, awaiter.GetResult());

		Assert.AreEqual(0, OutstandingAsyncOperationRegistry.OutstandingCount);
		Assert.Throws<InvalidOperationException>(() => _ = operation.IsCompleted);
	}

	[Test, Timeout(30_000)]
	public void ShouldRunContinuationScheduledAfterCompletion() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		operation.SetResult(13);

		var continuationRan = false;
		var awaiter = operation.GetAwaiter();
		awaiter.OnCompleted(() => continuationRan = true);

		Assert.IsFalse(continuationRan);
		PumpPrimaryThreadJobs();

		Assert.IsTrue(continuationRan);
		Assert.AreEqual(13, awaiter.GetResult());
	}

	[Test, Timeout(30_000)]
	public void ShouldThrowFromAwaiterGetResultWhenOperationFaults() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		operation.SetException(new InvalidOperationException("Deliberate test failure."));

		var awaiter = operation.GetAwaiter();
		Assert.IsTrue(awaiter.IsCompleted);

		var thrown = Assert.Throws<AggregateException>(() => _ = awaiter.GetResult());
		Assert.IsInstanceOf<InvalidOperationException>(thrown!.GetBaseException());
		Assert.AreEqual(0, OutstandingAsyncOperationRegistry.OutstandingCount);
	}

	[Test, Timeout(30_000)]
	public void ShouldFaultOutstandingOperationsWhenPoolDisposed() {
		var operation = new TinyFfrAsyncOperation<int>(Dispatcher);
		var continuationRan = false;
		var awaiter = operation.GetAwaiter();
		awaiter.OnCompleted(() => continuationRan = true);

		_pool.Dispose();

		Assert.IsFalse(continuationRan);
		OutstandingAsyncOperationRegistry.InvokeDeferredContinuations();

		Assert.IsTrue(continuationRan);
		var thrown = Assert.Throws<AggregateException>(() => _ = awaiter.GetResult());
		Assert.IsInstanceOf<ObjectDisposedException>(thrown!.GetBaseException());
	}
}