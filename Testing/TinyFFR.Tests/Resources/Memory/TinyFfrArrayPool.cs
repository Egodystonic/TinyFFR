// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class TinyFfrArrayPoolTest {
	const int BucketLength = 64;
	const int SimultaneousRentalCount = 5_000;

	[SetUp]
	public void SetUpTest() => TinyFfrArrayPool.ReleaseAllPooledMemory();

	[TearDown]
	public void TearDownTest() => TinyFfrArrayPool.ReleaseAllPooledMemory();

	[Test]
	public void ShouldReturnArrayOfAtLeastRequestedLength() {
		foreach (var requested in new[] { 1, 15, 16, 17, 63, 64, 65, 1000 }) {
			var array = TinyFfrArrayPool<int>.Shared.Rent(requested);
			Assert.GreaterOrEqual(array.Length, requested, $"Rent({requested}) returned an array of length {array.Length}.");
			TinyFfrArrayPool<int>.Shared.Return(array);
		}
	}

	[Test]
	public void ShouldReturnEmptyArrayForZeroLengthAndRejectNegativeLength() {
		Assert.That(TinyFfrArrayPool<int>.Shared.Rent(0), Is.SameAs(Array.Empty<int>()));
		Assert.Throws<ArgumentOutOfRangeException>(() => TinyFfrArrayPool<int>.Shared.Rent(-1));
		Assert.Throws<ArgumentNullException>(() => TinyFfrArrayPool<int>.Shared.Return(null!));
		Assert.DoesNotThrow(() => TinyFfrArrayPool<int>.Shared.Return(Array.Empty<int>()));
	}

	[Test]
	public void ShouldReuseTheSameArrayInstanceAcrossRentReturnCycles() {
		var first = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		TinyFfrArrayPool<int>.Shared.Return(first);
		var second = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);

		Assert.That(second, Is.SameAs(first), "Pool did not hand back the array that was just returned to it.");
		TinyFfrArrayPool<int>.Shared.Return(second);
	}

	// This is the defect the type exists to fix: ArrayPool<T>.Shared caches a bounded number of buffers per
	// bucket and discards the rest on Return, so a workload holding many thousands of buffers simultaneously
	// re-allocates nearly all of them next time around.
	[Test]
	public void ShouldRetainEveryBufferWhenFarMoreAreRentedSimultaneouslyThanArrayPoolSharedWouldCache() {
		var buffers = new int[SimultaneousRentalCount][];

		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		for (var i = 0; i < SimultaneousRentalCount; ++i) TinyFfrArrayPool<int>.Shared.Return(buffers[i]);

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		for (var i = 0; i < SimultaneousRentalCount; ++i) TinyFfrArrayPool<int>.Shared.Return(buffers[i]);

		var oneBufferSizeBytes = BucketLength * sizeof(int);
		Assert.Less(
			allocatedDuring,
			oneBufferSizeBytes * 10L,
			$"Re-renting {SimultaneousRentalCount} previously-returned buffers allocated {allocatedDuring} bytes; " +
			$"the pool is discarding returns rather than retaining them."
		);
	}

	// Guards the premise the whole type rests on. If this ever fails, ArrayPool<T>.Shared has gained
	// unbounded retention and TinyFfrArrayPool may no longer be needed.
	[Test]
	public void ArrayPoolSharedDiscardsReturnsBeyondItsBoundedCacheCapacity() {
		var buffers = new int[SimultaneousRentalCount][];

		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = ArrayPool<int>.Shared.Rent(BucketLength);
		for (var i = 0; i < SimultaneousRentalCount; ++i) ArrayPool<int>.Shared.Return(buffers[i]);

		var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = ArrayPool<int>.Shared.Rent(BucketLength);
		var allocatedDuring = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

		for (var i = 0; i < SimultaneousRentalCount; ++i) ArrayPool<int>.Shared.Return(buffers[i]);

		Assert.Greater(
			allocatedDuring,
			(long) SimultaneousRentalCount * BucketLength * sizeof(int) / 2L,
			$"ArrayPool<int>.Shared re-rented {SimultaneousRentalCount} previously-returned buffers using only " +
			$"{allocatedDuring} bytes, i.e. it retained them. The premise behind {nameof(TinyFfrArrayPool)} no longer holds."
		);
	}

	[Test]
	public void ShouldNotAllocateWhenRepeatedlyRentingAndReturning() {
		const int NumCycles = 10_000;

		TinyFfrArrayPool<int>.Shared.Return(TinyFfrArrayPool<int>.Shared.Rent(BucketLength));

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < NumCycles; ++i) {
			var array = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
			array[0] = i;
			TinyFfrArrayPool<int>.Shared.Return(array);
		}
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.Less(allocatedDuring, BucketLength * sizeof(int), $"{NumCycles} rent/return cycles allocated {allocatedDuring} bytes.");
	}

	[Test]
	public void ShouldClearReturnedArrayOnlyWhenAsked() {
		var array = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		Array.Fill(array, 7);
		TinyFfrArrayPool<int>.Shared.Return(array, clearArray: true);
		var cleared = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		Assert.That(cleared, Is.SameAs(array));
		Assert.IsTrue(cleared.All(v => v == 0), "Array was not cleared despite clearArray being true.");

		Array.Fill(cleared, 9);
		TinyFfrArrayPool<int>.Shared.Return(cleared, clearArray: false);
		var uncleared = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		Assert.That(uncleared, Is.SameAs(array));
		Assert.IsTrue(uncleared.All(v => v == 9), "Array was cleared despite clearArray being false.");

		TinyFfrArrayPool<int>.Shared.Return(uncleared);
	}

	[Test]
	public void ShouldSupportRentingAndReturningOnDifferentThreads() {
		var rentedOnWorker = null as int[];
		var worker = new Thread(() => rentedOnWorker = TinyFfrArrayPool<int>.Shared.Rent(BucketLength));
		worker.Start();
		worker.Join();

		Assert.IsNotNull(rentedOnWorker);
		Assert.DoesNotThrow(() => TinyFfrArrayPool<int>.Shared.Return(rentedOnWorker!));

		var rentedHere = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		Assert.That(rentedHere, Is.SameAs(rentedOnWorker), "Buffer returned from another thread was not reusable.");
		TinyFfrArrayPool<int>.Shared.Return(rentedHere);
	}

	[Test]
	public void ShouldNeverHandTheSameArrayToTwoSimultaneousRentersUnderContention() {
		const int ThreadCount = 8;
		const int IterationsPerThread = 4_000;

		var failure = null as string;

		Parallel.For(0, ThreadCount, threadIndex => {
			for (var i = 0; i < IterationsPerThread; ++i) {
				var array = TinyFfrArrayPool<long>.Shared.Rent(BucketLength);
				var sentinel = ((long) threadIndex << 32) | (uint) i;
				for (var slot = 0; slot < BucketLength; ++slot) array[slot] = sentinel;
				Thread.SpinWait(8);
				for (var slot = 0; slot < BucketLength; ++slot) {
					if (array[slot] == sentinel) continue;
					Interlocked.CompareExchange(ref failure, $"Array aliased between renters: slot {slot} held {array[slot]}, expected {sentinel}.", null);
					break;
				}
				TinyFfrArrayPool<long>.Shared.Return(array);
			}
		});

		Assert.IsNull(failure, failure);
	}

	[Test]
	public void ShouldFreeRetainedBuffersWhenReleaseValveIsPulled() {
		var buffers = new int[SimultaneousRentalCount][];
		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		for (var i = 0; i < SimultaneousRentalCount; ++i) TinyFfrArrayPool<int>.Shared.Return(buffers[i]);

		TinyFfrArrayPool.ReleaseAllPooledMemory();

		var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
		for (var i = 0; i < SimultaneousRentalCount; ++i) buffers[i] = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		var allocatedDuring = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

		for (var i = 0; i < SimultaneousRentalCount; ++i) TinyFfrArrayPool<int>.Shared.Return(buffers[i]);

		Assert.Greater(
			allocatedDuring,
			(long) SimultaneousRentalCount * BucketLength * sizeof(int) / 2L,
			"Release valve did not actually free the retained buffers."
		);
	}

	[Test]
	public void ShouldDelegateBuffersLargerThanThresholdToArrayPoolShared() {
		var elementCount = TinyFfrArrayPool.LargeBufferThresholdBytes / sizeof(int) * 4;

		var array = TinyFfrArrayPool<int>.Shared.Rent(elementCount);
		Assert.GreaterOrEqual(array.Length, elementCount);
		TinyFfrArrayPool<int>.Shared.Return(array);

		TinyFfrArrayPool.ReleaseAllPooledMemory();

		var afterRelease = TinyFfrArrayPool<int>.Shared.Rent(elementCount);
		Assert.That(afterRelease, Is.SameAs(array), "Large buffer was not held by ArrayPool<T>.Shared, so it is not being delegated.");
		TinyFfrArrayPool<int>.Shared.Return(afterRelease);
	}

#if DEBUG
	[Test]
	public void ShouldDetectDoubleReturnAndForeignArrays() {
		var array = TinyFfrArrayPool<int>.Shared.Rent(BucketLength);
		TinyFfrArrayPool<int>.Shared.Return(array);
		Assert.Throws<ArgumentException>(() => TinyFfrArrayPool<int>.Shared.Return(array));

		Assert.Throws<ArgumentException>(() => TinyFfrArrayPool<int>.Shared.Return(new int[BucketLength]));
		Assert.Throws<ArgumentException>(() => TinyFfrArrayPool<int>.Shared.Return(new int[BucketLength - 1]));

		TinyFfrArrayPool.ReleaseAllPooledMemory();
	}
#endif
}
