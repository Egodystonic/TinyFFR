// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Linq;
using Alpha = Egodystonic.TinyFFR.Resources.MockResourceAlpha;
using Bravo = Egodystonic.TinyFFR.Resources.MockResourceBravo;
using Impl = Egodystonic.TinyFFR.Resources.IMockResourceImplProvider;

namespace Egodystonic.TinyFFR.Resources;

[TestFixture]
class ResourceDependencyTrackerTest {
	const int NumMockResourcesPerList = 10;
	ResourceDependencyTracker _tracker;
	MockResourceImplProvider _alphaImplProvider;
	MockResourceImplProvider _bravoImplProvider;
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

		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[1]);
		AddDependencyAndAssert(_alphaResources[0], _bravoResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _bravoResources[0]);

		Assert.AreEqual(_bravoResources[0], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 0));
		Assert.AreEqual(_bravoResources[1], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 1));
		Assert.AreEqual(_bravoResources[2], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 2));

		Assert.AreEqual(_alphaResources[0], _tracker.GetNthDependentOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0], 0));
		Assert.AreEqual(_bravoResources[1], _tracker.GetNthDependentOfGivenType<Bravo, Bravo, Impl>(_bravoResources[0], 0));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0], 1));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Bravo, Impl>(_bravoResources[0], 1));
		Assert.Catch(() => _tracker.GetNthDependentOfGivenType<Bravo, Bravo, Impl>(_bravoResources[1], 0));

		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 0));
		Assert.Catch(() => _tracker.GetNthTargetOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 1));
		Assert.Catch(() => _tracker.GetNthTargetOfGivenType<Alpha, Bravo, Impl>(_alphaResources[1], 0));
		
		Assert.AreEqual(_alphaResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0], 0));
		Assert.AreEqual(_alphaResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, Impl>(_bravoResources[1], 0));
		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Bravo, Impl>(_bravoResources[1], 0));
		Assert.AreEqual(_alphaResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, Impl>(_bravoResources[2], 0));
		Assert.AreEqual(_alphaResources[1], _tracker.GetNthTargetOfGivenType<Bravo, Alpha, Impl>(_bravoResources[2], 1));

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

		Assert.AreEqual(3, _tracker.GetDependentsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[2]).Count);

		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetDependentsOfGivenType<Bravo, Bravo, Impl>(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetDependentsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[2]).Count);

		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0]).Count);
		Assert.AreEqual(0, _tracker.GetTargetsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.GetTargetsOfGivenType<Alpha, Alpha, Impl>(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.GetTargetsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.GetTargetsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[2]).Count);

		RemoveDependencyAndAssert(_bravoResources[1], _alphaResources[0]);
		Assert.AreEqual(1, _tracker.GetTargets(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.GetDependents(_alphaResources[0]).Count);
		Assert.AreEqual(_bravoResources[0], _tracker.GetNthTargetOfGivenType<Bravo, Bravo, Impl>(_bravoResources[1], 0));
		Assert.AreEqual(_bravoResources[0], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 0));
		Assert.AreEqual(_bravoResources[2], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0], 1));

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
		var iterator2 = _tracker.GetDependentsOfGivenType<Alpha, Bravo, Impl>(_alphaResources[0]);
		AssertIteratorValid(iterator2);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator2);

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var iterator3 = _tracker.GetTargets(_bravoResources[0]);
		AssertIteratorValid(iterator3);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator3);

		_tracker.RegisterDependency(_bravoResources[0], _alphaResources[0]);
		var iterator4 = _tracker.GetTargetsOfGivenType<Bravo, Alpha, Impl>(_bravoResources[0]);
		AssertIteratorValid(iterator4);
		_tracker.DeregisterDependency(_bravoResources[0], _alphaResources[0]);
		AssertIteratorInvalid(iterator4);
	}
}