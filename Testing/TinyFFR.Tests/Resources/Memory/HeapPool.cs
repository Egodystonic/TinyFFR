using System.Reflection;

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class HeapPoolTest {
	HeapPool _pool = null!;

	[SetUp]
	public void SetUpTest() {
		_pool = new HeapPool();
	}

	[TearDown]
	public void TearDownTest() {
		_pool?.Dispose();
	}

	[Test]
	public void ShouldBorrowRequestedSize() {
		var sizes = new[] { 0, 1, 7, 16, 100, 1024, 4096 };
		foreach (var size in sizes) {
			Assert.AreEqual(size, _pool.Borrow(size).Span.Length);
			Assert.AreEqual(size, _pool.Borrow<int>(size).Span.Length);
			Assert.AreEqual(size, _pool.Borrow<float>(size).Span.Length);
			Assert.AreEqual(size, _pool.Borrow<long>(size).Span.Length);
			Assert.AreEqual(size, _pool.Borrow<Vect>(size).Span.Length);
		}
		
		Assert.Catch(() => _pool.Borrow(-1));
		Assert.Catch(() => _pool.Borrow<int>(-1));
		Assert.Catch(() => _pool.Borrow<int>(1, -4));
	}

	[Test]
	public void ShouldCorrectlyBorrowWithCustomElementSize() {
		Assert.AreEqual(8, _pool.Borrow<int>(4, 8).Span.Length);
		Assert.AreEqual(48, _pool.Borrow<byte>(3, 16).Span.Length);
		Assert.AreEqual(6, _pool.Borrow<long>(2, 24).Span.Length);
	}

	[Test]
	public void ShouldCorrectlyBorrowAndCopyData() {
		var input = new Vect[] { new(1f, 2f, 3f), new(4f, 5f, 6f), new(7f, 8f, 9f) };
		var output = _pool.BorrowAndCopy(input).Span;
		Assert.IsTrue(input.SequenceEqual(output));
	}

	[Test]
	public void ShouldCorrectlyMaintainDataIntegrityAcrossMultipleTypes() {
		using var byteMem = _pool.Borrow<byte>(10);
		using var intMem = _pool.Borrow<int>(10);
		using var floatMem = _pool.Borrow<float>(10);
		using var longMem = _pool.Borrow<long>(10);
		using var vectMem = _pool.Borrow<Vect>(10);

		for (var i = 0; i < 10; ++i) {
			byteMem.Span[i] = (byte) i;
			intMem.Span[i] = i * 1000;
			floatMem.Span[i] = i * 1.5f;
			longMem.Span[i] = i * 100_000L;
			vectMem.Span[i] = new Vect(i, i * 2f, i * 3f);
		}

		for (var i = 0; i < 10; ++i) {
			Assert.AreEqual((byte) i, byteMem.Span[i]);
			Assert.AreEqual(i * 1000, intMem.Span[i]);
			Assert.AreEqual(i * 1.5f, floatMem.Span[i]);
			Assert.AreEqual(i * 100_000L, longMem.Span[i]);
			Assert.AreEqual(new Vect(i, i * 2f, i * 3f), vectMem.Span[i]);
		}
	}

	[Test]
	public void ShouldNeverLeakOrCorruptMemory() {
		const int NumIterations = 20_000;

		var rentedBufferArray = new PooledHeapMemory<byte>[Byte.MaxValue];

		for (var i = 0; i < Byte.MaxValue; ++i) {
			rentedBufferArray[i] = _pool.Borrow<byte>(Random.Shared.Next(1, 100));
			rentedBufferArray[i].Span.Fill((byte) i);
		}

		for (var i = 0; i < NumIterations; ++i) {
			var indexToReplace = Random.Shared.Next(Byte.MaxValue);
			var curSpan = rentedBufferArray[indexToReplace].Span;
			for (var b = 0; b < curSpan.Length; ++b) Assert.AreEqual((byte) indexToReplace, curSpan[b]);
			rentedBufferArray[indexToReplace].Dispose();
			rentedBufferArray[indexToReplace] = _pool.Borrow<byte>(Random.Shared.Next(1, 100));
			rentedBufferArray[indexToReplace].Span.Fill((byte) indexToReplace);
		}

		for (var i = 0; i < Byte.MaxValue; ++i) {
			for (var b = 0; b < rentedBufferArray[i].Span.Length; ++b) Assert.AreEqual((byte) i, rentedBufferArray[i].Span[b]);
			rentedBufferArray[i].Dispose();
		}
	}

	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(20d);

	static void RunConcurrently(int threadCount, Action<int> threadBody) {
		using var startSignal = new CountdownEvent(threadCount);
		var errors = new List<Exception>();
		var threads = new Thread[threadCount];

		for (var t = 0; t < threadCount; ++t) {
			var threadIndex = t;
			threads[t] = new Thread(() => {
				try {
					startSignal.Signal();
					startSignal.Wait(WaitTimeout);
					threadBody(threadIndex);
				}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- An escaping exception on a background thread would kill the test host
				catch (Exception e) {
#pragma warning restore CA1031
					lock (errors) errors.Add(e);
				}
			}) { IsBackground = true };
			threads[t].Start();
		}

		foreach (var thread in threads) Assert.IsTrue(thread.Join(WaitTimeout));
		lock (errors) {
			if (errors.Count > 0) Assert.Fail($"{errors.Count} of {threadCount} thread(s) failed. First failure: {errors[0]}");
		}
	}

	static void ThrowIfSpanIsNotUniform(ReadOnlySpan<byte> span, byte expectedValue) {
		for (var i = 0; i < span.Length; ++i) {
			if (span[i] == expectedValue) continue;
			throw new InvalidOperationException(
				$"Borrowed buffer was not exclusively owned by the borrowing thread: expected {expectedValue} " +
				$"at index {i} of {span.Length} but found {span[i]}."
			);
		}
	}

	[Test]
	public void ShouldExposeAStableThreadSafeWrapper() {
		var wrapper = _pool.ThreadSafeWrapper;
		Assert.IsNotNull(wrapper);
		Assert.IsTrue(ReferenceEquals(wrapper, _pool.ThreadSafeWrapper));
	}

	[Test]
	public void ShouldOnlyExposeThreadSafeMembersViaIHeapPool() {
		var methods = typeof(IHeapPool).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		var distinctNames = methods.Select(m => m.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();

		Assert.AreEqual(new[] { "Borrow", "BorrowAndCopy" }, distinctNames);
		Assert.AreEqual(4, methods.Length);
		Assert.IsFalse(
			methods.Any(m => m.Name.Contains("Lease", StringComparison.Ordinal)),
			$"{nameof(IHeapPool)} must not expose span lease creation: it mutates state that {nameof(HeapPool)} does not synchronize."
		);
	}

	[Test, Timeout(60_000)]
	public void ShouldBorrowExclusivelyOwnedBuffersConcurrently() {
		const int ThreadCount = 8;
		const int IterationsPerThread = 500;

		var wrapper = _pool.ThreadSafeWrapper;

		RunConcurrently(ThreadCount, threadIndex => {
			var patternUniqueToThisThread = (byte) (threadIndex + 1);
			for (var i = 0; i < IterationsPerThread; ++i) {
				var memory = wrapper.Borrow<byte>(16 + (i % 8) * 16);
				try {
					memory.Span.Fill(patternUniqueToThisThread);
					Thread.Yield();
					ThrowIfSpanIsNotUniform(memory.Span, patternUniqueToThisThread);
				}
				finally {
					memory.Dispose();
				}
			}
		});
	}

	[Test, Timeout(60_000)]
	public void ShouldBorrowAndCopyConcurrentlyWithoutTearing() {
		const int ThreadCount = 8;
		const int IterationsPerThread = 500;
		const int ElementCount = 32;

		var wrapper = _pool.ThreadSafeWrapper;

		RunConcurrently(ThreadCount, threadIndex => {
			var source = new int[ElementCount];
			for (var i = 0; i < ElementCount; ++i) source[i] = threadIndex * 1000 + i;

			for (var i = 0; i < IterationsPerThread; ++i) {
				var memory = wrapper.BorrowAndCopy<int>(source);
				try {
					Thread.Yield();
					if (!memory.Span.SequenceEqual(source)) {
						throw new InvalidOperationException($"Copied buffer contents did not match the source on thread {threadIndex}.");
					}
				}
				finally {
					memory.Dispose();
				}
			}
		});
	}

	[Test, Timeout(60_000)]
	public void ShouldRemainConsistentWhenBorrowsAndDisposalsInterleaveAcrossThreads() {
		const int ThreadCount = 8;
		const int IterationsPerThread = 500;
		const int ConcurrentlyHeldBufferCount = 4;

		var wrapper = _pool.ThreadSafeWrapper;

		RunConcurrently(ThreadCount, threadIndex => {
			var patternUniqueToThisThread = (byte) (threadIndex + 1);
			var heldBuffers = new PooledHeapMemory<byte>[ConcurrentlyHeldBufferCount];
			var heldBufferCount = 0;

			try {
				for (; heldBufferCount < ConcurrentlyHeldBufferCount; ++heldBufferCount) {
					heldBuffers[heldBufferCount] = wrapper.Borrow<byte>(16 + heldBufferCount * 16);
					heldBuffers[heldBufferCount].Span.Fill(patternUniqueToThisThread);
				}

				for (var i = 0; i < IterationsPerThread; ++i) {
					var slot = i % ConcurrentlyHeldBufferCount;
					ThrowIfSpanIsNotUniform(heldBuffers[slot].Span, patternUniqueToThisThread);
					var replacement = wrapper.Borrow<byte>(16 + (i % 8) * 16);
					replacement.Span.Fill(patternUniqueToThisThread);
					heldBuffers[slot].Dispose();
					heldBuffers[slot] = replacement;
					Thread.Yield();
				}
			}
			finally {
				for (var slot = 0; slot < heldBufferCount; ++slot) heldBuffers[slot].Dispose();
			}
		});
	}
}
