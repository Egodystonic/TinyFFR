// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Environment.Local;

[TestFixture]
class LocalApplicationLoopBuilderTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldPermitDifferentCooperativeTaskTimeFractionsPerLoop() {
		using var factory = new LocalTinyFfrFactory();
		using var loopA = factory.ApplicationLoopBuilder.CreateLoop();
		using var loopB = factory.ApplicationLoopBuilder.CreateLoop();

		loopA.TargetPerFrameAsyncCooperativeTaskTimeFraction = 8f;
		loopB.TargetPerFrameAsyncCooperativeTaskTimeFraction = 0f;

		Assert.AreEqual(8f, loopA.TargetPerFrameAsyncCooperativeTaskTimeFraction);
		Assert.AreEqual(0f, loopB.TargetPerFrameAsyncCooperativeTaskTimeFraction);

		loopB.TargetPerFrameAsyncCooperativeTaskTimeFraction = null;
		Assert.AreEqual(8f, loopA.TargetPerFrameAsyncCooperativeTaskTimeFraction, "Changing one loop's fraction altered another's.");
		Assert.AreEqual(null, loopB.TargetPerFrameAsyncCooperativeTaskTimeFraction);
	}

	[Test]
	public void ShouldDefaultCooperativeTaskTimeFractionFromBuilderConfig() {
		using var factory = new LocalTinyFfrFactory(localLoopBuilderConfig: new LocalApplicationLoopBuilderConfig {
			TargetPerFrameAsyncCooperativeTaskTimeFraction = 3f
		});
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		Assert.AreEqual(3f, loop.TargetPerFrameAsyncCooperativeTaskTimeFraction);
	}

	[Test]
	public void ShouldRejectInvalidCooperativeTaskTimeFraction() {
		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		Assert.Throws<ArgumentOutOfRangeException>(() => loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = -1f);
		Assert.Throws<ArgumentOutOfRangeException>(() => loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = Single.NaN);
		Assert.Throws<ArgumentOutOfRangeException>(() => loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = Single.PositiveInfinity);
	}

	[Test]
	public void ShouldRejectNegativeTargetIterationInterval() {
		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		Assert.Throws<ArgumentOutOfRangeException>(() => loop.TargetIterationInterval = TimeSpan.FromSeconds(-1d));
	}

	[Test]
	public void ShouldRoundTripTargetFrameRateAndIntervalThroughTheRealBuilder() {
		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(new LocalApplicationLoopCreationConfig { FrameRateCapHz = 30 });

		Assert.AreEqual(30, loop.TargetFrameRate);

		loop.TargetFrameRate = 60;
		Assert.AreEqual(60, loop.TargetFrameRate);

		loop.TargetIterationInterval = TimeSpan.Zero;
		Assert.AreEqual(null, loop.TargetFrameRate);
		Assert.AreEqual(TimeSpan.Zero, loop.TargetIterationInterval);
	}
}
