// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
class TinyFfrSynchronizationContextTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	CooperativeThreadPool _pool = null!;
	TinyFfrSynchronizationContext _context = null!;

	[SetUp]
	public void SetUpTest() {
		ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);
		_pool = new CooperativeThreadPool(new ThreadingConfig { WorkerThreadCount = 0 }, Thread.CurrentThread);
		_context = new TinyFfrSynchronizationContext(_pool);
	}

	[TearDown]
	public void TearDownTest() {
		_pool.Dispose();
		ThreadSafetyTracker.ClearPrimaryThread();
	}

	void PumpPrimaryThreadJobs() => ((IPrimaryThreadDispatcher) _pool).ExecutePendingCooperativeJobs(null);

	[Test, Timeout(30_000)]
	public void ShouldPostContinuationsToPrimaryThread() {
		var callbackRan = false;
		var callbackThreadId = 0;

		var poster = new Thread(() => _context.Post(_ => {
			callbackThreadId = System.Environment.CurrentManagedThreadId;
			callbackRan = true;
		}, null)) { IsBackground = true };
		poster.Start();
		Assert.IsTrue(poster.Join(WaitTimeout));

		Assert.IsFalse(callbackRan);

		PumpPrimaryThreadJobs();

		Assert.IsTrue(callbackRan);
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, callbackThreadId);
	}

	[Test, Timeout(30_000)]
	public void ShouldRunSendInlineWhenAlreadyOnPrimaryThread() {
		var callbackThreadId = 0;

		_context.Send(_ => callbackThreadId = System.Environment.CurrentManagedThreadId, null);

		Assert.AreEqual(System.Environment.CurrentManagedThreadId, callbackThreadId);
	}

	[Test, Timeout(30_000)]
	public void ShouldRunSendFromNonPrimaryThreadOnPrimaryThread() {
		var callbackThreadId = 0;

		var sender = new Thread(() => _context.Send(_ => callbackThreadId = System.Environment.CurrentManagedThreadId, null)) { IsBackground = true };
		sender.Start();

		var startTimestamp = Stopwatch.GetTimestamp();
		while (!sender.Join(TimeSpan.Zero) && Stopwatch.GetElapsedTime(startTimestamp) < WaitTimeout) {
			PumpPrimaryThreadJobs();
			Thread.Sleep(1);
		}

		Assert.IsTrue(sender.Join(WaitTimeout));
		Assert.AreEqual(System.Environment.CurrentManagedThreadId, callbackThreadId);
	}

	[Test, Timeout(30_000)]
	public void ShouldPropagateSendExceptionsToTheCallingThread() {
		Exception? capturedException = null;

		var sender = new Thread(() => {
			try {
				_context.Send(_ => throw new InvalidOperationException("Deliberate test failure."), null);
			}
#pragma warning disable CA1031
			catch (Exception e) {
#pragma warning restore CA1031
				capturedException = e;
			}
		}) { IsBackground = true };
		sender.Start();

		var startTimestamp = Stopwatch.GetTimestamp();
		while (!sender.Join(TimeSpan.Zero) && Stopwatch.GetElapsedTime(startTimestamp) < WaitTimeout) {
			PumpPrimaryThreadJobs();
			Thread.Sleep(1);
		}

		Assert.IsTrue(sender.Join(WaitTimeout));
		Assert.IsInstanceOf<InvalidOperationException>(capturedException);
	}

	[Test, Timeout(30_000)]
	public void ShouldResumeSubsequentAwaitsOnPrimaryThread() {
		var primaryThreadId = System.Environment.CurrentManagedThreadId;
		var previousContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(_context);

		try {
			var operation = new TinyFfrAsyncOperation<int>(_pool);
			var observedThreadIds = new List<int>();
			var completed = false;

			async Task RunAsync() {
				_ = await operation;
				observedThreadIds.Add(System.Environment.CurrentManagedThreadId);
				await Task.Yield();
				observedThreadIds.Add(System.Environment.CurrentManagedThreadId);
				await Task.Delay(TimeSpan.FromMilliseconds(20d));
				observedThreadIds.Add(System.Environment.CurrentManagedThreadId);
				completed = true;
			}

			var runTask = RunAsync();
			operation.SetResult(1);

			var startTimestamp = Stopwatch.GetTimestamp();
			while (!completed && Stopwatch.GetElapsedTime(startTimestamp) < WaitTimeout) {
				PumpPrimaryThreadJobs();
				Thread.Sleep(1);
			}

			Assert.IsTrue(completed);
			Assert.IsTrue(runTask.IsCompletedSuccessfully);
			Assert.AreEqual(3, observedThreadIds.Count);
			foreach (var observedThreadId in observedThreadIds) Assert.AreEqual(primaryThreadId, observedThreadId);
		}
		finally {
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}
	}
}
