// Created on 2026-07-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class ResourceHandleBasedSpanLeaseTrackerTest {
	static readonly ResourceHandle HandleA = new(1U);
	static readonly ResourceHandle HandleB = new(2U);
	static readonly ResourceHandle HandleC = new(3U);

	List<(ResourceHandle Handle, nuint LeaseId, int RentalsRemaining)> _recordedDisposals = null!;
	List<(ResourceHandle Handle, string Arg, int RentalsRemaining)> _recordedArgDisposals = null!;

	[SetUp]
	public void SetUpTest() {
		_recordedDisposals = new();
		_recordedArgDisposals = new();
	}

	[TearDown]
	public void TearDownTest() { }

	static void RecordDisposal(object? arg, ResourceHandle handle, nuint leaseId, int rentalsRemaining) {
		((List<(ResourceHandle, nuint, int)>) arg!).Add((handle, leaseId, rentalsRemaining));
	}

	static void RecordArgDisposal(object? arg, ResourceHandle handle, string leaseArg, int rentalsRemaining) {
		((List<(ResourceHandle, string, int)>) arg!).Add((handle, leaseArg, rentalsRemaining));
	}

	[Test]
	public void ShouldThrowOnNonPositiveMaxRentalsPerHandle() {
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ResourceHandleBasedSpanLeaseTracker<int>(0, false, false));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ResourceHandleBasedSpanLeaseTracker<int>(-1, false, false));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ResourceHandleBasedSpanLeaseTracker<int>(Int32.MinValue, false, false));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ResourceHandleBasedSpanLeaseTracker<int, string>(0, false, false, &RecordArgDisposal, null));

		new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false).Dispose();
		new ResourceHandleBasedSpanLeaseTracker<int>(1, false, false).Dispose();
	}

	[Test]
	public void ShouldCorrectlyTrackActiveRentalCountsPerHandle() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false);
		var arr = new int[4];

		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleA));

		var leaseA1 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		var leaseA2 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		var leaseB1 = tracker.CreateScopedLeaseOrThrow(HandleB, (ReadOnlySpan<int>) arr);

		Assert.AreEqual(2, tracker.GetActiveRentalsCount(HandleA));
		Assert.AreEqual(1, tracker.GetActiveRentalsCount(HandleB));
		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleC));

		leaseA1.Dispose();
		Assert.AreEqual(1, tracker.GetActiveRentalsCount(HandleA));
		Assert.AreEqual(1, tracker.GetActiveRentalsCount(HandleB));

		leaseA2.Dispose();
		leaseB1.Dispose();
		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleA));
		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleB));
	}

	[Test]
	public void ShouldCorrectlyWrapGivenSpansInLeases() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false);
		var arr = new[] { 10, 20, 30, 40 };

		var writableLease = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.AreEqual(4, writableLease.Span.Length);
		writableLease.Span[2] = 300;
		Assert.AreEqual(300, arr[2]);

		var readOnlyLease = tracker.CreateScopedLeaseOrThrow(HandleA, (ReadOnlySpan<int>) arr);
		Assert.AreEqual(4, readOnlyLease.Span.Length);
		Assert.AreEqual(300, readOnlyLease.Span[2]);

		Assert.AreNotEqual(writableLease.LeaseId, readOnlyLease.LeaseId);

		writableLease.Dispose();
		readOnlyLease.Dispose();
	}

	[Test]
	public void ShouldEnforceMaxRentalsPerHandle() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(2, false, false);
		var arr = new int[4];

		var leaseA1 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		var leaseA2 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.Throws<InvalidOperationException>(() => _ = tracker.CreateScopedLeaseOrThrow(HandleA, new int[1].AsSpan()));

		var leaseB1 = tracker.CreateScopedLeaseOrThrow(HandleB, arr.AsSpan());

		leaseA1.Dispose();
		var leaseA3 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());

		leaseA2.Dispose();
		leaseA3.Dispose();
		leaseB1.Dispose();

		using var unlimitedTracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false);
		for (var i = 0; i < 10; ++i) _ = unlimitedTracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.AreEqual(10, unlimitedTracker.GetActiveRentalsCount(HandleA));
	}

	[Test]
	public void ShouldThrowIfAnyActiveRentalsOnlyWhenRentalsAreActive() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false);
		var arr = new int[4];

		Assert.DoesNotThrow(() => tracker.ThrowIfAnyActiveRentals(HandleA, "TestType", "TestName"));

		var lease = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.Throws<ResourceDependencyException>(() => tracker.ThrowIfAnyActiveRentals(HandleA, "TestType", "TestName"));
		Assert.DoesNotThrow(() => tracker.ThrowIfAnyActiveRentals(HandleB, "TestType", "TestName"));

		lease.Dispose();
		Assert.DoesNotThrow(() => tracker.ThrowIfAnyActiveRentals(HandleA, "TestType", "TestName"));
	}

	[Test]
	public void ShouldInvokeDisposalCallbackWithCorrectArguments() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false, &RecordDisposal, _recordedDisposals);
		var arr = new int[4];

		var leaseA1 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		var leaseA2 = tracker.CreateScopedLeaseOrThrow(HandleA, (ReadOnlySpan<int>) arr);
		var leaseB1 = tracker.CreateScopedLeaseOrThrow(HandleB, arr.AsSpan());

		Assert.AreEqual(0, _recordedDisposals.Count);

		leaseA1.Dispose();
		Assert.AreEqual(1, _recordedDisposals.Count);
		Assert.AreEqual((HandleA, leaseA1.LeaseId, 1), _recordedDisposals[0]);

		leaseA2.Dispose();
		Assert.AreEqual(2, _recordedDisposals.Count);
		Assert.AreEqual((HandleA, leaseA2.LeaseId, 0), _recordedDisposals[1]);

		leaseB1.Dispose();
		Assert.AreEqual(3, _recordedDisposals.Count);
		Assert.AreEqual((HandleB, leaseB1.LeaseId, 0), _recordedDisposals[2]);
	}

	[Test]
	public void ShouldIgnoreRepeatedLeaseDisposal() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false, &RecordDisposal, _recordedDisposals);
		var arr = new int[4];

		var lease = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		lease.Dispose();
		lease.Dispose();
		Assert.AreEqual(1, _recordedDisposals.Count);
		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleA));

		var secondLease = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.AreEqual(1, tracker.GetActiveRentalsCount(HandleA));
		secondLease.Dispose();
	}

	[Test]
	public void ShouldThrowOnTrackerDisposalWhenConfiguredAndRentalsAreActive() {
		var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, true, false);
		var arr = new int[4];

		var lease = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan());
		Assert.Throws<InvalidOperationException>(() => tracker.Dispose());

		lease.Dispose();
		Assert.DoesNotThrow(() => tracker.Dispose());
	}

	[Test]
	public void ShouldNotThrowOnTrackerDisposalWhenNotConfigured() {
		var tracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, false, false);
		_ = tracker.CreateScopedLeaseOrThrow(HandleA, new int[1].AsSpan());
		Assert.DoesNotThrow(() => tracker.Dispose());

		var failFastConfiguredTracker = new ResourceHandleBasedSpanLeaseTracker<int>(null, true, true);
		Assert.DoesNotThrow(() => failFastConfiguredTracker.Dispose());
	}

	[Test]
	public void ShouldPassArgumentsThroughToDisposalCallbackInBinaryTrackerVariant() {
		using var tracker = new ResourceHandleBasedSpanLeaseTracker<int, string>(null, false, false, &RecordArgDisposal, _recordedArgDisposals);
		var arr = new int[4];

		var leaseA1 = tracker.CreateScopedLeaseOrThrow(HandleA, arr.AsSpan(), "alpha");
		var leaseA2 = tracker.CreateScopedLeaseOrThrow(HandleA, (ReadOnlySpan<int>) arr, "bravo");
		var untrackedLease = tracker.CreateScopedLeaseOrThrow(HandleB, arr.AsSpan());

		Assert.AreEqual(2, tracker.GetActiveRentalsCount(HandleA));
		Assert.AreEqual(1, tracker.GetActiveRentalsCount(HandleB));
		Assert.Throws<ResourceDependencyException>(() => tracker.ThrowIfAnyActiveRentals(HandleA, "TestType", "TestName"));

		untrackedLease.Dispose();
		Assert.AreEqual(0, tracker.GetActiveRentalsCount(HandleB));
		Assert.AreEqual(0, _recordedArgDisposals.Count);

		leaseA1.Dispose();
		Assert.AreEqual(1, _recordedArgDisposals.Count);
		Assert.AreEqual((HandleA, "alpha", 1), _recordedArgDisposals[0]);

		leaseA2.Dispose();
		Assert.AreEqual(2, _recordedArgDisposals.Count);
		Assert.AreEqual((HandleA, "bravo", 0), _recordedArgDisposals[1]);

		Assert.DoesNotThrow(() => tracker.ThrowIfAnyActiveRentals(HandleA, "TestType", "TestName"));
	}

	[Test]
	public void ShouldThrowOnBinaryTrackerDisposalWhenConfiguredAndRentalsAreActive() {
		var tracker = new ResourceHandleBasedSpanLeaseTracker<int, string>(null, true, false, &RecordArgDisposal, _recordedArgDisposals);
		_ = tracker.CreateScopedLeaseOrThrow(HandleA, new int[1].AsSpan(), "alpha");
		Assert.Throws<InvalidOperationException>(() => tracker.Dispose());
	}
}
