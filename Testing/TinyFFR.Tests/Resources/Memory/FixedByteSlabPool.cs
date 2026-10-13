// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class FixedByteSlabPoolTest {
	const int SlabSizeBytes = 1024 * 1024;
	const long DefaultRetainedBudgetBytes = 512L * 1024 * 1024;
	const int LargestTestedRentalBytes = 1024 * 1024 * 1024;
	FixedByteSlabPool _pool = null!;

	[SetUp]
	public void SetUpTest() => _pool = new FixedByteSlabPool(DefaultRetainedBudgetBytes);

	[TearDown]
	public void TearDownTest() => _pool.Dispose();
	
	int CountLiveSlabs() => CountLiveSlabs(_pool);

	static int CountLiveSlabs(FixedByteSlabPool pool) {
		var recordCount = (int) typeof(FixedByteSlabPool).GetField("_slabRecordMaxConcurrentUsageCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pool)!;
		var freeRecordCount = (int) typeof(FixedByteSlabPool).GetField("_returnedUnusedSlabRecordCounter", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pool)!;
		return recordCount - freeRecordCount;
	}

	[Test]
	public void ShouldRoundRentalsUpToTheirSizeClass() {
		(int Requested, int ExpectedSlotBytes)[] cases = [
			(0, 256), (1, 256), (256, 256), (257, 512), (4_000, 4_096), (4_097, 8_192), (FixedByteSlabPool.MaxSmallSlotSizeBytes, FixedByteSlabPool.MaxSmallSlotSizeBytes),
			(FixedByteSlabPool.MaxSmallSlotSizeBytes + 1, 320 * 1024), (384 * 1024, 384 * 1024), (512 * 1024 + 1, 640 * 1024),
			(8_294_400, 8 * 1024 * 1024), (33_177_600, 32 * 1024 * 1024), (100 * 1024 * 1024, 112 * 1024 * 1024),
			(LargestTestedRentalBytes, LargestTestedRentalBytes)
		];
		foreach (var (requested, expectedSlotBytes) in cases) {
			var rental = _pool.Rent(requested);
			Assert.AreEqual(requested, rental.SizeBytes);
			Assert.AreEqual((long) expectedSlotBytes, _pool.CurrentlyLeasedTotalBytes, $"Rent({requested})");
			_pool.Return(rental);
			Assert.AreEqual(0L, _pool.CurrentlyLeasedTotalBytes);
		}
	}

	[Test]
	public void ShouldHandOutNonOverlappingWritableRentals() {
		var sizes = new[] { 1, 200, 256, 300, 1_000, 5_000, 70_000, 200_000 };
		var rentals = new List<FixedByteSlabPool.SlabRental>();
		for (var i = 0; i < 600; ++i) {
			var rental = _pool.Rent(sizes[i % sizes.Length]);
			rental.AsByteSpan.Fill((byte) i);
			rentals.Add(rental);
		}

		for (var i = 0; i < rentals.Count; ++i) {
			var span = rentals[i].AsByteSpan;
			Assert.AreEqual(-1, span.IndexOfAnyExcept((byte) i), $"Rental {i} was overwritten by another rental.");
		}
		foreach (var rental in rentals) _pool.Return(rental);
		Assert.AreEqual(0L, _pool.CurrentlyLeasedTotalBytes);
	}

	[Test]
	public void ShouldReuseMostRecentlyReturnedSlot() {
		var first = _pool.Rent(1_000);
		var second = _pool.Rent(1_000);
		_pool.Return(first);
		var third = _pool.Rent(900);
		Assert.AreEqual(first.SlotPtr, third.SlotPtr);
		Assert.AreNotEqual(second.SlotPtr, third.SlotPtr);
		_pool.Return(second);
		_pool.Return(third);
	}

	[Test]
	public void ShouldReleaseEmptySlabsBeyondTheRetainedOne() {
		const int SlotSizeBytes = 64 * 1024;
		const int SlabCount = 3;
		var rentals = new List<FixedByteSlabPool.SlabRental>();
		for (var i = 0; i < SlabCount * (SlabSizeBytes / SlotSizeBytes); ++i) rentals.Add(_pool.Rent(SlotSizeBytes));
		Assert.AreEqual(SlabCount, CountLiveSlabs());

		foreach (var rental in rentals) _pool.Return(rental);
		Assert.AreEqual(1, CountLiveSlabs());

		var reRented = _pool.Rent(SlotSizeBytes);
		Assert.AreEqual(1, CountLiveSlabs());
		_pool.Return(reRented);
	}

	[Test]
	public void ShouldRejectInvalidRentalsAndSizes() {
		Assert.Throws<ArgumentOutOfRangeException>(() => _pool.Rent(-1));
		Assert.Throws<ArgumentOutOfRangeException>(() => new FixedByteSlabPool(-1));
		Assert.Throws<InvalidObjectException>(() => _pool.Return(default));

		using var otherPool = new FixedByteSlabPool(DefaultRetainedBudgetBytes);
		var foreign = otherPool.Rent(100);
		var local = _pool.Rent(100);
		Assert.Throws<ArgumentException>(() => _pool.Return(new FixedByteSlabPool.SlabRental(foreign.SlotPtr, foreign.SizeBytes, 0)));
		Assert.Throws<ArgumentException>(() => _pool.Return(new FixedByteSlabPool.SlabRental(local.SlotPtr + 1, local.SizeBytes, 0)));
		_pool.Return(local);
		otherPool.Return(foreign);

		_pool.Dispose();
		Assert.Throws<ObjectDisposedException>(() => _pool.Rent(1));
	}

	[Test]
	public void ShouldWasteAtMostAQuarterAboveTheSmallClasses() {
		for (var requested = FixedByteSlabPool.MaxSmallSlotSizeBytes + 1L; requested <= LargestTestedRentalBytes; requested = requested * 9L / 8L + 4_099L) {
			var rental = _pool.Rent((int) requested);
			Assert.LessOrEqual(_pool.CurrentlyLeasedTotalBytes, requested * 1.25d + 1d, $"Rent({requested}) occupies {_pool.CurrentlyLeasedTotalBytes} bytes.");
			Assert.GreaterOrEqual(_pool.CurrentlyLeasedTotalBytes, requested);
			_pool.Return(rental);
		}
	}

	[Test]
	public void ShouldReuseRetainedLargeSlotsAndWriteTheirWholeLength() {
		const int ReadbackSizeBytes = 3840 * 2160 * 4;
		var first = _pool.Rent(ReadbackSizeBytes);
		first.AsByteSpan.Fill(0xAB);
		Assert.AreEqual(-1, first.AsByteSpan.IndexOfAnyExcept((byte) 0xAB));
		_pool.Return(first);

		var second = _pool.Rent(ReadbackSizeBytes);
		Assert.AreEqual(first.SlotPtr, second.SlotPtr);
		Assert.AreEqual(1, CountLiveSlabs());
		_pool.Return(second);
	}

	[Test]
	public void ShouldRetainAsManyEmptyLargeSlotsAsWereRentedAtOnce() {
		const int LargeRentalBytes = 6 * 1024 * 1024;
		var inFlight = new[] { _pool.Rent(LargeRentalBytes), _pool.Rent(LargeRentalBytes), _pool.Rent(LargeRentalBytes) };
		foreach (var rental in inFlight) _pool.Return(rental);
		Assert.AreEqual(3, CountLiveSlabs());

		for (var frame = 0; frame < 10; ++frame) {
			var a = _pool.Rent(LargeRentalBytes);
			var b = _pool.Rent(LargeRentalBytes);
			var c = _pool.Rent(LargeRentalBytes);
			Assert.AreEqual(3, CountLiveSlabs(), "In-flight large rentals should reuse retained slots rather than allocate.");
			_pool.Return(a);
			_pool.Return(b);
			_pool.Return(c);
		}
		Assert.AreEqual(3, CountLiveSlabs());
	}

	[Test]
	public void ShouldReleaseIdleLargeSlotsWhenRetentionWindowsPassWithoutDemand() {
		const int LargeRentalBytes = 6 * 1024 * 1024;
		var rentals = new[] { _pool.Rent(LargeRentalBytes), _pool.Rent(LargeRentalBytes), _pool.Rent(LargeRentalBytes) };
		foreach (var rental in rentals) _pool.Return(rental);
		Assert.AreEqual(3, CountLiveSlabs());

		_pool.EndRetentionWindow();
		Assert.AreEqual(3, CountLiveSlabs(), "The window in which the slots were used should still be honoured.");

		_pool.Return(_pool.Rent(LargeRentalBytes));
		_pool.EndRetentionWindow();
		Assert.AreEqual(1, CountLiveSlabs(), "Only one slot was needed during the last window.");

		_pool.EndRetentionWindow();
		Assert.AreEqual(0, CountLiveSlabs(), "No slots were needed during the last window.");
	}

	[Test]
	public void ShouldReleaseEmptyLargeSlotsBeyondTheRetainedBudget() {
		const int LargeRentalBytes = 6 * 1024 * 1024;
		const int RentalCount = 40;
		const long RetainedBudgetBytes = 64L * 1024 * 1024;
		using var budgetedPool = new FixedByteSlabPool(RetainedBudgetBytes);
		var rentals = new List<FixedByteSlabPool.SlabRental>();
		for (var i = 0; i < RentalCount; ++i) rentals.Add(budgetedPool.Rent(LargeRentalBytes));
		Assert.AreEqual(RentalCount, CountLiveSlabs(budgetedPool));

		foreach (var rental in rentals) budgetedPool.Return(rental);
		var retained = CountLiveSlabs(budgetedPool);
		Assert.AreEqual((int) (RetainedBudgetBytes / LargeRentalBytes), retained);
		Assert.AreEqual(0L, budgetedPool.CurrentlyLeasedTotalBytes);
	}

	[Test]
	public void ShouldRetainNothingWithAZeroBudget() {
		using var unretainingPool = new FixedByteSlabPool(0L);
		var small = unretainingPool.Rent(300);
		var large = unretainingPool.Rent(6 * 1024 * 1024);
		Assert.AreEqual(2, CountLiveSlabs(unretainingPool));

		unretainingPool.Return(small);
		unretainingPool.Return(large);
		Assert.AreEqual(0, CountLiveSlabs(unretainingPool));
		Assert.AreEqual(0L, unretainingPool.CurrentlyLeasedTotalBytes);
	}

	[Test]
	public void ShouldCountEmptySmallSlabsAgainstTheRetainedBudget() {
		using var budgetedPool = new FixedByteSlabPool(SlabSizeBytes);
		var first = budgetedPool.Rent(300);
		var second = budgetedPool.Rent(1_000);
		Assert.AreEqual(2, CountLiveSlabs(budgetedPool));

		budgetedPool.Return(first);
		budgetedPool.Return(second);
		Assert.AreEqual(1, CountLiveSlabs(budgetedPool), "Only one 1 MiB small slab fits in a 1 MiB budget.");
	}

	[Test]
	public void ShouldNotAllocateManagedMemoryForLargeRentals() {
		_pool.Return(_pool.Rent(5_000_000));
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 200; ++i) _pool.Return(_pool.Rent(5_000_000));
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.AreEqual(0L, allocatedDuring);
	}

	[Test]
	public void ShouldNotAllocateManagedMemoryOnceSlabRecordsExist() {
		var warmup = _pool.Rent(500);
		_pool.Return(warmup);

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 1_000; ++i) _pool.Return(_pool.Rent(500));
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.AreEqual(0L, allocatedDuring);
	}
}
