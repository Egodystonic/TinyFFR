// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
class HostMessageLoopPumpTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	sealed class RecordingSynchronizationContext : SynchronizationContext {
		readonly List<(SendOrPostCallback Callback, object? State)> _pending = new();

		public int PostCount { get; private set; }
		public bool DispatchOnForeignThread { get; init; }
		public bool ThrowOnPost { get; set; }

		public override void Post(SendOrPostCallback d, object? state) {
			lock (_pending) {
				++PostCount;
				if (ThrowOnPost) throw new InvalidOperationException("Deliberate test failure.");
				_pending.Add((d, state));
			}
		}

		public override void Send(SendOrPostCallback d, object? state) => d(state);

		public int Drain() {
			(SendOrPostCallback Callback, object? State)[] snapshot;
			lock (_pending) {
				snapshot = _pending.ToArray();
				_pending.Clear();
			}

			if (DispatchOnForeignThread) {
				var thread = new Thread(() => {
					foreach (var entry in snapshot) entry.Callback(entry.State);
				}) { IsBackground = true };
				thread.Start();
				Assert.IsTrue(thread.Join(WaitTimeout));
			}
			else {
				foreach (var entry in snapshot) entry.Callback(entry.State);
			}

			return snapshot.Length;
		}
	}

	SynchronizationContext? _previousContext;
	CooperativeThreadPool? _pool;

	[SetUp]
	public void SetUpTest() {
		_previousContext = SynchronizationContext.Current;
		ThreadSafetyTracker.SetPrimaryThread(Thread.CurrentThread);
	}

	[TearDown]
	public void TearDownTest() {
		_pool?.Dispose();
		_pool = null;
		OutstandingAsyncOperationRegistry.InvokeDeferredContinuations();
		ThreadSafetyTracker.ClearPrimaryThread();
		SynchronizationContext.SetSynchronizationContext(_previousContext);
	}

	CooperativeThreadPool CreatePoolWithHostContext(SynchronizationContext? context, bool wakeHostMessageLoop = true) {
		SynchronizationContext.SetSynchronizationContext(context);
		_pool = new CooperativeThreadPool(new ThreadingConfig {
			WorkerThreadCount = 0,
			WakeHostMessageLoopForPrimaryThreadWork = wakeHostMessageLoop
		});
		return _pool;
	}

	[Test, Timeout(30_000)]
	public void ShouldRunContinuationWithoutAnyApplicationLoopIterationWhenHostContextIsPresent() {
		var context = new RecordingSynchronizationContext();
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var continuationRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => continuationRan = true);

		Assert.IsFalse(continuationRan, "Continuation ran before anything pumped it.");
		Assert.AreEqual(1, context.PostCount, "Scheduling primary thread work did not post a pump to the host context.");

		Assert.AreEqual(1, context.Drain());

		Assert.IsTrue(continuationRan, "Draining only the host synchronization context did not run the continuation.");
	}

	[Test, Timeout(30_000)]
	public void ShouldRunWorkScheduledFromAnotherThreadViaHostContext() {
		var context = new RecordingSynchronizationContext();
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var continuationRan = false;

		var scheduler = new Thread(() => dispatcher.SchedulePrimaryThreadContinuation(() => continuationRan = true)) { IsBackground = true };
		scheduler.Start();
		Assert.IsTrue(scheduler.Join(WaitTimeout));

		Assert.IsFalse(continuationRan);
		Assert.AreEqual(1, context.PostCount);

		context.Drain();

		Assert.IsTrue(continuationRan);
	}

	[Test, Timeout(30_000)]
	public void ShouldCoalesceMultiplePumpRequestsInToOnePost() {
		const int ContinuationCount = 8;
		var context = new RecordingSynchronizationContext();
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var runCount = 0;

		for (var i = 0; i < ContinuationCount; ++i) dispatcher.SchedulePrimaryThreadContinuation(() => ++runCount);

		Assert.AreEqual(1, context.PostCount, "Each scheduled continuation posted its own pump instead of coalescing.");

		context.Drain();

		Assert.AreEqual(ContinuationCount, runCount, "The single coalesced pump did not drain every pending continuation.");
	}

	[Test, Timeout(30_000)]
	public void ShouldPostAgainForWorkScheduledAfterAPump() {
		var context = new RecordingSynchronizationContext();
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var secondRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => { });
		context.Drain();
		Assert.AreEqual(1, context.PostCount);

		dispatcher.SchedulePrimaryThreadContinuation(() => secondRan = true);

		Assert.AreEqual(2, context.PostCount, "Work scheduled after a pump did not request a new one.");
		context.Drain();
		Assert.IsTrue(secondRan);
	}

	[Test, Timeout(30_000)]
	public void ShouldNotPumpHostMessageLoopWhenDisabledByConfig() {
		var context = new RecordingSynchronizationContext();
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context, wakeHostMessageLoop: false);
		var continuationRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => continuationRan = true);

		Assert.AreEqual(0, context.PostCount, "Host pumping was disabled but a pump was still posted.");
		Assert.AreEqual(0, context.Drain());
		Assert.IsFalse(continuationRan);

		dispatcher.ExecutePendingCooperativeJobs(null);

		Assert.IsTrue(continuationRan, "The continuation should still run via an explicit pump when host pumping is disabled.");
	}

	[Test, Timeout(30_000)]
	public void ShouldNotPumpWhenNoHostContextExists() {
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(null);
		var continuationRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => continuationRan = true);

		Assert.IsFalse(continuationRan);

		dispatcher.ExecutePendingCooperativeJobs(null);

		Assert.IsTrue(continuationRan);
	}

	[Test, Timeout(30_000)]
	public void ShouldNotExecuteQueuedWorkWhenPumpDoesNotLandOnPrimaryThread() {
		var context = new RecordingSynchronizationContext { DispatchOnForeignThread = true };
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var firstRan = false;
		var secondRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => firstRan = true);
		Assert.AreEqual(1, context.PostCount);

		context.Drain();

		Assert.IsFalse(firstRan, "A pump that did not land on the primary thread executed queued work anyway.");

		dispatcher.SchedulePrimaryThreadContinuation(() => secondRan = true);

		Assert.AreEqual(2, context.PostCount, "A pump that missed the primary thread should decline to execute work, not stop the pool posting altogether.");

		dispatcher.ExecutePendingCooperativeJobs(null);

		Assert.IsTrue(firstRan);
		Assert.IsTrue(secondRan);
	}

	[Test, Timeout(30_000)]
	public void ShouldKeepPumpingAfterAHostContextPostThrows() {
		var context = new RecordingSynchronizationContext { ThrowOnPost = true };
		var dispatcher = (IPrimaryThreadDispatcher) CreatePoolWithHostContext(context);
		var firstRan = false;
		var secondRan = false;

		dispatcher.SchedulePrimaryThreadContinuation(() => firstRan = true);

		Assert.AreEqual(1, context.PostCount, "The pool did not attempt to post to the host context.");
		Assert.IsFalse(firstRan);

		context.ThrowOnPost = false;
		dispatcher.SchedulePrimaryThreadContinuation(() => secondRan = true);

		Assert.AreEqual(2, context.PostCount, "A throwing post left the pump request outstanding, so no further pump was ever requested.");

		context.Drain();

		Assert.IsTrue(firstRan);
		Assert.IsTrue(secondRan);
	}
}
