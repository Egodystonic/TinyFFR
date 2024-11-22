// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

[TestFixture]
class ResourceDependencyTrackerTest {
	ResourceDependencyTracker _tracker;

    [SetUp]
    public void SetUpTest() {
		_tracker = new ResourceDependencyTracker();
	}

    [TearDown]
    public void TearDownTest() {
		_tracker.Dispose();
	}

	[Test]
	public void ShouldCorrectlyTrackDependencies() {
		
	}
}