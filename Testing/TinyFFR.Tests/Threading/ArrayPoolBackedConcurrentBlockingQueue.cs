// Created on 2026-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Threading;

[TestFixture]
class ArrayPoolBackedConcurrentBlockingQueueTest {
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10d);

	ArrayPoolBackedConcurrentBlockingQueue<int> _queue = null!;

	[SetUp]
	public void SetUpTest() {
		_queue = new();
	}

	[TearDown]
	public void TearDownTest() {
		_queue.Dispose();
	}

	int DequeueOrFail() {
		Assert.IsTrue(_queue.TryDequeueBlocking(out var result));
		return result;
	}

	static Thread StartBackgroundThread(Action threadBody) {
		var result = new Thread(() => {
			try {
				threadBody();
			}
			catch (ObjectDisposedException) { }
		}) { IsBackground = true };
		result.Start();
		return result;
	}

	[Test]
	public void ShouldEnqueueAndDequeueInFifoOrder() {
		_queue.Enqueue(10);
		_queue.Enqueue(20);
		_queue.Enqueue(30);

		Assert.AreEqual(10, DequeueOrFail());
		Assert.AreEqual(20, DequeueOrFail());
		Assert.AreEqual(30, DequeueOrFail());
	}

	[Test]
	public void ShouldGrowBeyondInitialCapacity() {
		const int ItemCount = 100;

		for (var i = 0; i < ItemCount; ++i) _queue.Enqueue(i);
		for (var i = 0; i < ItemCount; ++i) Assert.AreEqual(i, DequeueOrFail());
	}

	[Test]
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	[TestCase(7)]
	public void ShouldCorrectlyGrowWhenWrapped(int rotationCount) {
		const int ItemCount = 50;

		for (var i = 0; i < rotationCount; ++i) {
			_queue.Enqueue(-1);
			Assert.AreEqual(-1, DequeueOrFail());
		}

		for (var i = 0; i < ItemCount; ++i) _queue.Enqueue(i);
		for (var i = 0; i < ItemCount; ++i) Assert.AreEqual(i, DequeueOrFail());
	}

	[Test]
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	[TestCase(7)]
	public void ShouldPreserveBacklogAcrossGrowth(int rotationCount) {
		const int RoundCount = 40;
		const int EnqueuesPerRound = 3;

		for (var i = 0; i < rotationCount; ++i) {
			_queue.Enqueue(-1);
			Assert.AreEqual(-1, DequeueOrFail());
		}

		var expectation = new Queue<int>();
		var nextItem = 0;

		for (var round = 0; round < RoundCount; ++round) {
			for (var i = 0; i < EnqueuesPerRound; ++i) {
				_queue.Enqueue(nextItem);
				expectation.Enqueue(nextItem);
				++nextItem;
			}
			Assert.AreEqual(expectation.Dequeue(), DequeueOrFail());
		}

		while (expectation.Count > 0) Assert.AreEqual(expectation.Dequeue(), DequeueOrFail());
	}

	[Test]
	public void ShouldReuseSlotsWithoutGrowing() {
		const int CycleCount = 1000;

		for (var i = 0; i < CycleCount; ++i) {
			_queue.Enqueue(i);
			Assert.AreEqual(i, DequeueOrFail());
		}
	}

	[Test]
	public void ShouldMatchReferenceQueueUnderRandomOperations() {
		const int OperationCount = 10_000;

		var rng = new Random(12345);
		var expectation = new Queue<int>();
		var nextItem = 0;

		for (var i = 0; i < OperationCount; ++i) {
			if (expectation.Count == 0 || rng.Next(0, 2) == 0) {
				_queue.Enqueue(nextItem);
				expectation.Enqueue(nextItem);
				++nextItem;
			}
			else {
				Assert.AreEqual(expectation.Dequeue(), DequeueOrFail());
			}
		}

		while (expectation.Count > 0) Assert.AreEqual(expectation.Dequeue(), DequeueOrFail());
	}

	[Test]
	public void ShouldThrowOnEnqueueAfterDisposal() {
		_queue.Dispose();

		Assert.Throws<ObjectDisposedException>(() => _queue.Enqueue(1));
	}

	[Test]
	public void ShouldStopDequeueingAfterDisposal() {
		_queue.Enqueue(1);
		_queue.Enqueue(2);
		_queue.Dispose();

		Assert.IsFalse(_queue.TryDequeueBlocking(out _));
	}

	[Test]
	public void ShouldTolerateRepeatedDisposal() {
		_queue.Enqueue(1);
		_queue.Dispose();

		Assert.DoesNotThrow(() => _queue.Dispose());
		Assert.DoesNotThrow(() => _queue.Dispose());
	}

	[Test, Timeout(30_000)]
	public void ShouldUnblockWaitingDequeuersOnDisposal() {
		const int ThreadCount = 4;

		using var startSignal = new CountdownEvent(ThreadCount);
		var unblockedCount = 0;
		var unexpectedExceptionCount = 0;
		var threads = new Thread[ThreadCount];

		for (var i = 0; i < ThreadCount; ++i) {
			threads[i] = new Thread(() => {
				try {
					startSignal.Signal();
					if (!_queue.TryDequeueBlocking(out _)) Interlocked.Increment(ref unblockedCount);
				}
#pragma warning disable CA1031
				catch (Exception) {
#pragma warning restore CA1031
					Interlocked.Increment(ref unexpectedExceptionCount);
				}
			}) { IsBackground = true };
			threads[i].Start();
		}

		Assert.IsTrue(startSignal.Wait(WaitTimeout));
		Thread.Sleep(100);
		_queue.Dispose();

		foreach (var thread in threads) Assert.IsTrue(thread.Join(WaitTimeout));
		Assert.AreEqual(ThreadCount, unblockedCount);
		Assert.AreEqual(0, unexpectedExceptionCount);
	}

	[Test, Timeout(30_000)]
	public void ShouldBlockUntilItemEnqueued() {
		using var resultSignal = new ManualResetEventSlim(false);
		var result = 0;

		var consumer = StartBackgroundThread(() => {
			if (_queue.TryDequeueBlocking(out var dequeuedValue)) result = dequeuedValue;
			resultSignal.Set();
		});

		Assert.IsFalse(resultSignal.Wait(TimeSpan.FromMilliseconds(250d)));

		_queue.Enqueue(1234);

		Assert.IsTrue(resultSignal.Wait(WaitTimeout));
		Assert.IsTrue(consumer.Join(WaitTimeout));
		Assert.AreEqual(1234, result);
	}

	[Test, Timeout(30_000)]
	public void ShouldNotLoseOrDuplicateItemsUnderConcurrentAccess() {
		const int ProducerCount = 4;
		const int ConsumerCount = 3;
		const int ItemsPerProducer = 5000;
		const int TotalItemCount = ProducerCount * ItemsPerProducer;

		var claimCounter = 0;
		var consumedPerThread = new List<int>[ConsumerCount];
		var consumers = new Thread[ConsumerCount];

		for (var c = 0; c < ConsumerCount; ++c) {
			var consumedItems = new List<int>(TotalItemCount / ConsumerCount);
			consumedPerThread[c] = consumedItems;
			consumers[c] = StartBackgroundThread(() => {
				while (Interlocked.Increment(ref claimCounter) <= TotalItemCount) {
					if (_queue.TryDequeueBlocking(out var dequeuedValue)) consumedItems.Add(dequeuedValue);
				}
			});
		}

		var producers = new Thread[ProducerCount];
		for (var p = 0; p < ProducerCount; ++p) {
			var producerIndex = p;
			producers[p] = StartBackgroundThread(() => {
				for (var i = 0; i < ItemsPerProducer; ++i) _queue.Enqueue(producerIndex * ItemsPerProducer + i);
			});
		}

		foreach (var producer in producers) Assert.IsTrue(producer.Join(WaitTimeout));
		foreach (var consumer in consumers) Assert.IsTrue(consumer.Join(WaitTimeout));

		var allConsumedItems = new List<int>(TotalItemCount);
		foreach (var consumedItems in consumedPerThread) allConsumedItems.AddRange(consumedItems);
		allConsumedItems.Sort();

		Assert.AreEqual(TotalItemCount, allConsumedItems.Count);
		for (var i = 0; i < TotalItemCount; ++i) Assert.AreEqual(i, allConsumedItems[i]);
	}

	[Test, Timeout(30_000)]
	public void ShouldPreservePerProducerFifoOrder() {
		const int ProducerCount = 4;
		const int ItemsPerProducer = 5000;
		const int TotalItemCount = ProducerCount * ItemsPerProducer;

		var consumedItems = new List<int>(TotalItemCount);
		var consumer = StartBackgroundThread(() => {
			for (var i = 0; i < TotalItemCount; ++i) {
				if (_queue.TryDequeueBlocking(out var dequeuedValue)) consumedItems.Add(dequeuedValue);
			}
		});

		var producers = new Thread[ProducerCount];
		for (var p = 0; p < ProducerCount; ++p) {
			var producerIndex = p;
			producers[p] = StartBackgroundThread(() => {
				for (var i = 0; i < ItemsPerProducer; ++i) _queue.Enqueue(producerIndex * ItemsPerProducer + i);
			});
		}

		foreach (var producer in producers) Assert.IsTrue(producer.Join(WaitTimeout));
		Assert.IsTrue(consumer.Join(WaitTimeout));

		Assert.AreEqual(TotalItemCount, consumedItems.Count);

		var lastSeenSequenceIndices = new int[ProducerCount];
		Array.Fill(lastSeenSequenceIndices, -1);

		foreach (var item in consumedItems) {
			var producerIndex = item / ItemsPerProducer;
			var sequenceIndex = item % ItemsPerProducer;
			Assert.IsTrue(sequenceIndex > lastSeenSequenceIndices[producerIndex]);
			lastSeenSequenceIndices[producerIndex] = sequenceIndex;
		}

		for (var p = 0; p < ProducerCount; ++p) Assert.AreEqual(ItemsPerProducer - 1, lastSeenSequenceIndices[p]);
	}

	[Test, Timeout(30_000)]
	public void ShouldDrainRemainingItemsAfterSeal() {
		_queue.Enqueue(1);
		_queue.Enqueue(2);
		_queue.Seal();

		Assert.IsTrue(_queue.TryDequeueBlocking(out var first));
		Assert.AreEqual(1, first);
		Assert.IsTrue(_queue.TryDequeueBlocking(out var second));
		Assert.AreEqual(2, second);
		Assert.IsFalse(_queue.TryDequeueBlocking(out _));
	}

	[Test, Timeout(30_000)]
	public void ShouldRejectEnqueueAfterSeal() {
		_queue.Seal();

		Assert.Throws<ObjectDisposedException>(() => _queue.Enqueue(1));
		Assert.DoesNotThrow(() => _queue.Seal());
	}

	[Test, Timeout(30_000)]
	public void ShouldUnblockWaitingDequeuersOnSeal() {
		const int ThreadCount = 4;

		using var startSignal = new CountdownEvent(ThreadCount);
		var unblockedCount = 0;
		var threads = new Thread[ThreadCount];

		for (var i = 0; i < ThreadCount; ++i) {
			threads[i] = new Thread(() => {
				startSignal.Signal();
				if (!_queue.TryDequeueBlocking(out _)) Interlocked.Increment(ref unblockedCount);
			}) { IsBackground = true };
			threads[i].Start();
		}

		Assert.IsTrue(startSignal.Wait(WaitTimeout));
		Thread.Sleep(100);
		_queue.Seal();

		foreach (var thread in threads) Assert.IsTrue(thread.Join(WaitTimeout));
		Assert.AreEqual(ThreadCount, unblockedCount);
	}

	[Test, Timeout(30_000)]
	public void ShouldUseTheFullCapacityOfTheRentedBuffer() {
		for (var i = 0; i < 16; ++i) _queue.Enqueue(i);
		for (var i = 0; i < 16; ++i) Assert.AreEqual(i, DequeueOrFail());
	}
}
