// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Alpha = Egodystonic.TinyFFR.Resources.MockResourceAlpha;
using AlphaHandle = Egodystonic.TinyFFR.Resources.MockHandleAlpha;
using Bravo = Egodystonic.TinyFFR.Resources.MockResourceBravo;
using BravoHandle = Egodystonic.TinyFFR.Resources.MockHandleBravo;
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
			OnRawHandleGetName = n => "Alpha-" + n
		};
		_bravoImplProvider = new() {
			OnRawHandleGetName = n => "Bravo-" + n
		};
		_alphaResources = new();
		_bravoResources = new();
		for (var i = 0; i < NumMockResourcesPerList; ++i) {
			_alphaResources.Add(new Alpha {
				Handle = AlphaHandle.CreateFromInteger((nuint) i),
				Implementation = _alphaImplProvider,
				Name = _alphaImplProvider.RawHandleGetName((nuint) i)
			});
			_bravoResources.Add(new Bravo {
				Handle = BravoHandle.CreateFromInteger((nuint) i),
				Implementation = _bravoImplProvider,
				Name = _bravoImplProvider.RawHandleGetName((nuint) i)
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

			Assert.AreEqual(1,
				_tracker.EnumerateDependents(target).Count(s => s.Handle == dependent.Handle && s.Implementation.Equals(dependent.Implementation))
			);
			Assert.AreEqual(1,
				_tracker.EnumerateTargets(dependent).Count(s => s.Handle == target.Handle && s.Implementation.Equals(target.Implementation))
			);
		}
		void RemoveDependencyAndAssert<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource {
			_tracker.DeregisterDependency(dependent, target);

			Assert.AreEqual(0,
				_tracker.EnumerateDependents(target).Count(s => s.Handle == dependent.Handle && s.Implementation.Equals(dependent.Implementation))
			);
			Assert.AreEqual(0,
				_tracker.EnumerateTargets(dependent).Count(s => s.Handle == target.Handle && s.Implementation.Equals(target.Implementation))
			);
		}

		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[0], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[0]);
		AddDependencyAndAssert(_bravoResources[2], _alphaResources[1]);
		AddDependencyAndAssert(_alphaResources[0], _bravoResources[0]);
		AddDependencyAndAssert(_bravoResources[1], _bravoResources[0]);

		Assert.AreEqual(_bravoResources[0], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[0], 0));
		Assert.AreEqual(_bravoResources[1], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[0], 1));
		Assert.AreEqual(_bravoResources[2], _tracker.GetNthDependentOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[0], 2));

		Assert.AreEqual(_alphaResources[0], _tracker.GetNthDependentOfGivenType<Bravo, Alpha, AlphaHandle, Impl>(_bravoResources[0], 0));
		Assert.AreEqual(_bravoResources[1], _tracker.GetNthDependentOfGivenType<Bravo, Bravo, BravoHandle, Impl>(_bravoResources[0], 0));

		Assert.AreEqual(3, _tracker.EnumerateDependents(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.EnumerateDependents(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependents(_alphaResources[2]).Count);
		Assert.AreEqual(2, _tracker.EnumerateDependents(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependents(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependents(_bravoResources[2]).Count);

		Assert.AreEqual(1, _tracker.EnumerateTargets(_alphaResources[0]).Count);
		Assert.AreEqual(0, _tracker.EnumerateTargets(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.EnumerateTargets(_alphaResources[2]).Count);
		Assert.AreEqual(1, _tracker.EnumerateTargets(_bravoResources[0]).Count);
		Assert.AreEqual(2, _tracker.EnumerateTargets(_bravoResources[1]).Count);
		Assert.AreEqual(2, _tracker.EnumerateTargets(_bravoResources[2]).Count);

		Assert.AreEqual(3, _tracker.EnumerateDependentsOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[0]).Count);
		Assert.AreEqual(1, _tracker.EnumerateDependentsOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[1]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependentsOfGivenType<Alpha, Bravo, BravoHandle, Impl>(_alphaResources[2]).Count);

		Assert.AreEqual(1, _tracker.EnumerateDependentsOfGivenType<Bravo, Alpha, AlphaHandle, Impl>(_bravoResources[0]).Count);
		Assert.AreEqual(1, _tracker.EnumerateDependentsOfGivenType<Bravo, Bravo, BravoHandle, Impl>(_bravoResources[0]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependentsOfGivenType<Bravo, Alpha, AlphaHandle, Impl>(_bravoResources[1]).Count);
		Assert.AreEqual(0, _tracker.EnumerateDependentsOfGivenType<Bravo, Alpha, AlphaHandle, Impl>(_bravoResources[2]).Count);
	}
}