// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Linq;
using Alpha = Egodystonic.TinyFFR.Resources.MockResourceAlpha;
using Bravo = Egodystonic.TinyFFR.Resources.MockResourceBravo;
using AlphaImpl = Egodystonic.TinyFFR.Resources.IMockAlphaResourceImplProvider;
using BravoImpl = Egodystonic.TinyFFR.Resources.IMockBravoResourceImplProvider;

namespace Egodystonic.TinyFFR.Resources;

[TestFixture]
class ResourceDependencyTrackerTest {
	const int NumMockResourcesPerList = 10;
	ResourceDependencyTracker _tracker;
	MockAlphaResourceImplProvider _alphaImplProvider;
	MockBravoResourceImplProvider _bravoImplProvider;
	List<Alpha> _alphaResources;
	List<Bravo> _bravoResources;

    [SetUp]
    public void SetUpTest() {
		_tracker = new ResourceDependencyTracker();
		_alphaImplProvider = new() {
			OnGetNameAsNewStringObject = n => "Alpha-" + n
		};
		_bravoImplProvider = new() {
			OnGetNameAsNewStringObject = n => "Bravo-" + n
		};
		_alphaResources = new();
		_bravoResources = new();
		for (var i = 0; i < NumMockResourcesPerList; ++i) {
			_alphaResources.Add(new Alpha {
				Handle = new((nuint) i),
				Implementation = _alphaImplProvider,
				Name = _alphaImplProvider.GetNameAsNewStringObject((nuint) i)
			});
			_bravoResources.Add(new Bravo {
				Handle = new((nuint) i),
				Implementation = _bravoImplProvider,
				Name = _bravoImplProvider.GetNameAsNewStringObject((nuint) i)
			});
		}
	}

    [TearDown]
    public void TearDownTest() {
		_tracker.Dispose();
	}

	[Test]
	public void ShouldCorrectlyTrackDependencies() {
		void AddDependencyAndAssert<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource {
			_tracker.RegisterDependency(dependent, target);
			Assert.Throws<ResourceDependencyException>(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(target));
			_tracker.DeregisterDependency(dependent, target);
			_tracker.RegisterDependency(dependent, target);
			Assert.DoesNotThrow(() => _tracker.RegisterDependency(dependent, target)); // Check that add/remove is idempotent

			Assert.AreEqual(1,
				_tracker.GetDependents(target).Count(s => s.Handle == dependent.Handle && s.Implementation.Equals(dependent.Implementation))
			);
			Assert.AreEqual(1,
				_tracker.GetTargets(dependent).Count(s => s.Handle == target.Handle && s.Implementation.Equals(target.Implementation))
			);
		}
		void RemoveDependencyAndAssert<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource {
			_tracker.DeregisterDependency(dependent, target);
			_tracker.RegisterDependency(dependent, target);
			_tracker.DeregisterDependency(dependent, target);
			Assert.DoesNotThrow(() => _tracker.DeregisterDependency(dependent, target)); // Check that add/remove is idempotent

			Assert.AreEqual(0,
				_tracker.GetDependents(target).Count(s => s.Handle == dependent.Handle && s.Implementation.Equals(dependent.Implementation))
			);
			Assert.AreEqual(0,
				_tracker.GetTargets(dependent).Count(s => s.Handle == target.Handle && s.Implementation.Equals(target.Implementation))
			);
		}

		void AssertNthDependentsOfGivenType<TTarget, TDependent, TImpl>(TTarget target, params TDependent[] expected)
			where TTarget : IResource
			where TDependent : IResource<TDependent, TImpl>
			where TImpl : class, IResourceImplProvider {
			var actual = new List<TDependent>();
			for (var i = 0; i < expected.Length; ++i) actual.Add(_tracker.GetNthDependentOfGivenType<TTarget, TDependent, TImpl>(target, i));
			Assert.That(actual, Is.EquivalentTo(expected));
		}
		void AssertNthTargetsOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent, params TTarget[] expected)
			where TDependent : IResource
			where TTarget : IResource<TTarget, TImpl>
			where TImpl : class, IResourceImplProvider {
			var actual = new List<TTarget>();
			for (var i = 0; i < expected.Length; ++i) actual.Add(_tracker.GetNthTargetOfGivenType<TDependent, TTarget, TImpl>(dependent, i));
			Assert.That(actual, Is.EquivalentTo(expected));
		}

		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[1]);
		AddDependencyAndAssert(_alphaResources[0], _bravoResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _bravoResources[0]);

		AssertNthDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0], _bravoResources[0], _bravoResources[1], _bravoResources[2]);

		Assert.AreEqual(_alphaResources[0], _tracker.GetNthDependentOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0], 0));
		Assert.AreEqual(_bravoResources[1], _tracker.GetNthDependentOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[0], 0));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0], 1));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[0], 1));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[1], 0));

		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0], 0));
		Assert.Catch(() => _tracker.GetNthTargetOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0], 1));
		Assert.Catch(() => _tracker.GetNthTargetOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[1], 0));
		
		Assert.AreEqual(_alphaResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0], 0));
		Assert.AreEqual(_alphaResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[1], 0));
		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[1], 0));
		AssertNthTargetsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[2], _alphaResources[0], _alphaResources[1]);

		Assert.AreEqual(3, _tracker.GetDependents(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_alphaResources[2]).Count);
		Assert.AreEqual(2, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[2]).Count);

		Assert.AreEqual(1, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetTargets(_alphaResources[2]).Count);
		Assert.AreEqual(1, _tracker.GetTargets(_bravoResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_bravoResources[2]).Count);

		Assert.AreEqual(3, _tracker.GetDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[2]).Count);

		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[2]).Count);

		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetTargetsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetTargetsOfGivenType<Alpha, Alpha, AlphaImpl>(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.GetTargetsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[2]).Count);

		RemoveDependencyAndAssert(_bravoResources[1], _alphaResources[0]);
		Assert.AreEqual(1, _tracker.GetTargets(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.GetDependents(_alphaResources[0]).Count);
		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Bravo, BravoImpl>(_bravoResources[1], 0));
		AssertNthDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0], _bravoResources[0], _bravoResources[2]);

		Assert.Throws<ResourceDependencyException>(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_bravoResources[0]));
		RemoveDependencyAndAssert(_alphaResources[0], _bravoResources[0]);
		RemoveDependencyAndAssert(_bravoResources[1], _bravoResources[0]);
		Assert.DoesNotThrow(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_bravoResources[0]));
	}

	[Test]
	public void DeregistrationOfAllDependenciesShouldWorkAsExpected() {
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[0]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[1]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[2]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[3]);
		_tracker.RegisterDependency(_alphaResources[1], _bravoResources[3]);
		_tracker.RegisterDependency(_alphaResources[1], _bravoResources[4]);

		Assert.AreEqual(4, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[2]).Count);
		Assert.AreEqual(2, _tracker.GetDependents(_bravoResources[3]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[4]).Count);
		_tracker.DeregisterAllDependencies(_alphaResources[0]);
		Assert.AreEqual(0, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[2]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[3]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[4]).Count);
		_tracker.DeregisterAllDependencies(_alphaResources[0]);
		Assert.AreEqual(0, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[2]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[3]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[4]).Count);

		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[0]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[1]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[2]);
		_tracker.RegisterDependency(_alphaResources[0], _bravoResources[3]);
		Assert.AreEqual(4, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[2]).Count);
		Assert.AreEqual(2, _tracker.GetDependents(_bravoResources[3]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[4]).Count);
		_tracker.DeregisterAllDependencies(_alphaResources[0]);
		Assert.AreEqual(0, _tracker.GetTargets(_alphaResources[0]).Count);
		Assert.AreEqual(2, _tracker.GetTargets(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependents(_bravoResources[2]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[3]).Count);
		Assert.AreEqual(1, _tracker.GetDependents(_bravoResources[4]).Count);
	}

	[Test]
	public void ShouldCorrectlyInvalidateIteratorsAfterStateChanges() {
		void AssertIteratorValid<T>(IndirectEnumerable<IResourceDependencyTracker.EnumerationInput, T> iterator) {
			Assert.DoesNotThrow(() => iterator.CopyTo(new T[100]));
			Assert.DoesNotThrow(() => _ = iterator.TryCopyTo(new T[1000]));
			Assert.DoesNotThrow(() => _ = iterator.Count);
			Assert.DoesNotThrow(() => iterator.ElementAt(0));
			Assert.DoesNotThrow(() => _ = iterator.Count());
			Assert.DoesNotThrow(() => _ = iterator[0]);
		}
		void AssertIteratorInvalid<T>(IndirectEnumerable<IResourceDependencyTracker.EnumerationInput, T> iterator) {
			Assert.Catch<InvalidOperationException>(() => iterator.CopyTo(new T[100]));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.TryCopyTo(new T[1000]));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.Count);
			Assert.Catch<InvalidOperationException>(() => iterator.ElementAt(0));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.Count());
			Assert.Catch<InvalidOperationException>(() => _ = iterator[0]);
		}

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);

		var iterator = _tracker.GetDependents(_alphaResources[0]);
		AssertIteratorValid(iterator);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator);

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var iterator2 = _tracker.GetDependentsOfGivenType<Alpha, Bravo, BravoImpl>(_alphaResources[0]);
		AssertIteratorValid(iterator2);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator2);

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var iterator3 = _tracker.GetTargets(_bravoResources[0]);
		AssertIteratorValid(iterator3);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator3);

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var iterator4 = _tracker.GetTargetsOfGivenType<Bravo, Alpha, AlphaImpl>(_bravoResources[0]);
		AssertIteratorValid(iterator4);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator4);
	}

	ResourceStub Stub(int alphaIndex) => ResourceUtils.ToStub(_alphaResources[alphaIndex]);

	ResourceStub[] GetOrderedResources(ResourceStub[] resources, ResourceIdent ignoredDependent = default) {
		var order = new int[resources.Length];
		Assert.IsTrue(_tracker.TryGetDisposalOrder(resources, ignoredDependent, order, out _));
		Assert.That(order, Is.Unique);
		return order.Select(i => resources[i]).ToArray();
	}

	[Test]
	public void DisposalOrderShouldBeReverseOfInputOrderWhenUnconstrained() {
		var resources = new[] { Stub(0), Stub(1), Stub(2), Stub(3) };
		Assert.AreEqual(new[] { Stub(3), Stub(2), Stub(1), Stub(0) }, GetOrderedResources(resources));
		Assert.AreEqual(Array.Empty<ResourceStub>(), GetOrderedResources(Array.Empty<ResourceStub>()));
	}

	[Test]
	public void DisposalOrderShouldRespectDependencyChains() {
		_tracker.RegisterDependency(_alphaResources[1], _alphaResources[0]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[1]);

		Assert.AreEqual(new[] { Stub(2), Stub(1), Stub(0) }, GetOrderedResources(new[] { Stub(0), Stub(1), Stub(2) }));
		Assert.AreEqual(new[] { Stub(2), Stub(1), Stub(0) }, GetOrderedResources(new[] { Stub(2), Stub(1), Stub(0) }));
		Assert.AreEqual(new[] { Stub(2), Stub(1), Stub(0) }, GetOrderedResources(new[] { Stub(1), Stub(0), Stub(2) }));
	}

	[Test]
	public void DisposalOrderShouldRespectDiamondDependencies() {
		_tracker.RegisterDependency(_alphaResources[3], _alphaResources[1]);
		_tracker.RegisterDependency(_alphaResources[3], _alphaResources[2]);
		_tracker.RegisterDependency(_alphaResources[1], _alphaResources[0]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[0]);

		foreach (var input in new[] { new[] { 0, 1, 2, 3 }, new[] { 3, 2, 1, 0 }, new[] { 1, 3, 0, 2 } }) {
			var ordered = GetOrderedResources(input.Select(Stub).ToArray()).ToList();
			Assert.Less(ordered.IndexOf(Stub(3)), ordered.IndexOf(Stub(1)));
			Assert.Less(ordered.IndexOf(Stub(3)), ordered.IndexOf(Stub(2)));
			Assert.Less(ordered.IndexOf(Stub(1)), ordered.IndexOf(Stub(0)));
			Assert.Less(ordered.IndexOf(Stub(2)), ordered.IndexOf(Stub(0)));
		}
	}

	[Test]
	public void DisposalOrderShouldFailForExternalDependentsAndIgnoreTheGivenDependent() {
		_tracker.RegisterDependency(_alphaResources[1], _alphaResources[0]);
		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var order = new int[1];

		Assert.IsFalse(_tracker.TryGetDisposalOrder(new[] { Stub(0) }, ResourceUtils.ToStub(_bravoResources[0]).Ident, order, out var failure));
		Assert.AreEqual(Stub(0), failure.BlockedResource);
		Assert.AreEqual(Stub(1), failure.BlockingDependent);

		_tracker.DeregisterDependency(_alphaResources[1], _alphaResources[0]);
		Assert.IsTrue(_tracker.TryGetDisposalOrder(new[] { Stub(0) }, ResourceUtils.ToStub(_bravoResources[0]).Ident, order, out _));
		Assert.IsFalse(_tracker.TryGetDisposalOrder(new[] { Stub(0) }, default, order, out failure));
		Assert.AreEqual(ResourceUtils.ToStub(_bravoResources[0]), failure.BlockingDependent);
	}

	[Test]
	public void DisposalOrderShouldFailForCycles() {
		_tracker.RegisterDependency(_alphaResources[0], _alphaResources[1]);
		_tracker.RegisterDependency(_alphaResources[1], _alphaResources[2]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[0]);
		var order = new int[4];

		Assert.IsFalse(_tracker.TryGetDisposalOrder(new[] { Stub(0), Stub(1), Stub(2), Stub(3) }, default, order, out var failure));
		Assert.IsNull(failure.BlockingDependent);
		Assert.Contains(failure.BlockedResource, new[] { Stub(0), Stub(1), Stub(2) });
	}

	[Test]
	public void OwnershipShouldExtendPrematureDisposalChecksToOwnedResources() {
		_tracker.RegisterOwnership(_alphaResources[0], _alphaResources[1]);
		_tracker.RegisterOwnership(_alphaResources[0], _alphaResources[2]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[1]);
		Assert.DoesNotThrow(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_alphaResources[0]));

		_tracker.RegisterDependency(_alphaResources[3], _alphaResources[1]);
		var exception = Assert.Throws<ResourceDependencyException>(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_alphaResources[0]));
		StringAssert.Contains("0x0000000000000003", exception!.Message);

		_tracker.DeregisterDependency(_alphaResources[3], _alphaResources[1]);
		Assert.DoesNotThrow(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_alphaResources[0]));

		_tracker.DeregisterAllDependencies(_alphaResources[2]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[0]);
		Assert.Throws<ResourceDependencyException>(() => _tracker.ThrowForPrematureDisposalIfTargetHasDependents(_alphaResources[0]));
	}

	[Test]
	public void DisposalOrderShouldSeeThroughOwnedResources() {
		_tracker.RegisterOwnership(_alphaResources[0], _alphaResources[1]);
		_tracker.RegisterDependency(_alphaResources[2], _alphaResources[1]);

		Assert.AreEqual(new[] { Stub(2), Stub(0) }, GetOrderedResources(new[] { Stub(2), Stub(0) }));
		Assert.AreEqual(new[] { Stub(2), Stub(0) }, GetOrderedResources(new[] { Stub(0), Stub(2) }));
		Assert.AreEqual(new[] { Stub(1), Stub(0) }, GetOrderedResources(new[] { Stub(0), Stub(1) }, ResourceUtils.ToStub(_alphaResources[2]).Ident));

		var order = new int[1];
		Assert.IsFalse(_tracker.TryGetDisposalOrder(new[] { Stub(0) }, default, order, out var failure));
		Assert.AreEqual(Stub(1), failure.BlockedResource);
		Assert.AreEqual(Stub(2), failure.BlockingDependent);
	}
}