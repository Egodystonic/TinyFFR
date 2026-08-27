// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using NSubstitute;

namespace Egodystonic.TinyFFR.Environment;

[TestFixture]
class ApplicationLoopTest {
	static readonly ResourceHandle<ApplicationLoop> TestHandle = new(1);

	IApplicationLoopImplProvider _impl = null!;
	ApplicationLoop _loop;
	TimeSpan _storedInterval;
	float? _storedFraction;

	[SetUp]
	public void SetUpTest() {
		_storedInterval = TimeSpan.Zero;
		_storedFraction = null;

		_impl = Substitute.For<IApplicationLoopImplProvider>();
		_impl.IsDisposed(Arg.Any<ResourceHandle<ApplicationLoop>>()).Returns(false);

		_impl.When(x => x.SetTargetIterationInterval(Arg.Any<ResourceHandle<ApplicationLoop>>(), Arg.Any<TimeSpan>()))
			.Do(callInfo => _storedInterval = callInfo.Arg<TimeSpan>());
		_impl.GetTargetIterationInterval(Arg.Any<ResourceHandle<ApplicationLoop>>()).Returns(_ => _storedInterval);

		_impl.When(x => x.SetTargetPerFrameAsyncCooperativeTaskTimeFraction(Arg.Any<ResourceHandle<ApplicationLoop>>(), Arg.Any<float?>()))
			.Do(callInfo => _storedFraction = callInfo.Arg<float?>());
		_impl.GetTargetPerFrameAsyncCooperativeTaskTimeFraction(Arg.Any<ResourceHandle<ApplicationLoop>>()).Returns(_ => _storedFraction);

		_loop = new ApplicationLoop(TestHandle, _impl);
	}

	[TearDown]
	public void TearDownTest() {
		_loop.Dispose();
	}

	[Test]
	public void ShouldRoundTripTargetFrameRate() {
		foreach (var frameRate in new[] { 24, 30, 50, 60, 90, 120, 144, 240 }) {
			_loop.TargetFrameRate = frameRate;
			Assert.AreEqual(
				frameRate,
				_loop.TargetFrameRate,
				$"{frameRate}Hz did not round-trip through TargetFrameRate (stored interval was {_storedInterval})."
			);
		}
	}

	[Test]
	public void ShouldTreatNullTargetFrameRateAsUncapped() {
		_loop.TargetFrameRate = 60;
		Assert.AreEqual(60, _loop.TargetFrameRate);

		_loop.TargetFrameRate = null;
		Assert.AreEqual(TimeSpan.Zero, _storedInterval, "A null frame rate should store a zero interval.");
		Assert.AreEqual(null, _loop.TargetFrameRate);
		Assert.AreEqual(TimeSpan.Zero, _loop.TargetIterationInterval);
	}

	[Test]
	public void ShouldConvertTargetFrameRateToExpectedInterval() {
		_loop.TargetFrameRate = 60;
		Assert.AreEqual(TimeSpan.FromSeconds(1d) / 60, _storedInterval);

		_loop.TargetFrameRate = 30;
		Assert.AreEqual(TimeSpan.FromSeconds(1d) / 30, _storedInterval);
	}

	[Test]
	public void ShouldRejectNonPositiveTargetFrameRate() {
		Assert.Throws<ArgumentOutOfRangeException>(() => _loop.TargetFrameRate = 0);
		Assert.Throws<ArgumentOutOfRangeException>(() => _loop.TargetFrameRate = -1);
	}

	[Test]
	public void ShouldForwardTargetIterationIntervalUnchanged() {
		var interval = TimeSpan.FromMilliseconds(12.5d);
		_loop.TargetIterationInterval = interval;

		_impl.Received().SetTargetIterationInterval(TestHandle, interval);
		Assert.AreEqual(interval, _loop.TargetIterationInterval);
	}

	[Test]
	public void ShouldForwardTargetPerFrameAsyncCooperativeTaskTimeFractionUnchanged() {
		foreach (var fraction in new float?[] { 0f, 0.25f, 8f, null }) {
			_loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = fraction;
			Assert.AreEqual(fraction, _loop.TargetPerFrameAsyncCooperativeTaskTimeFraction);
		}

		_impl.Received().SetTargetPerFrameAsyncCooperativeTaskTimeFraction(TestHandle, 0.25f);
	}

	[Test]
	public void ShouldProvideSetterMethodEquivalentsForAllMutableProperties() {
		_loop.SetTargetIterationInterval(TimeSpan.FromMilliseconds(20d));
		Assert.AreEqual(TimeSpan.FromMilliseconds(20d), _loop.TargetIterationInterval);

		_loop.SetTargetFrameRate(120);
		Assert.AreEqual(120, _loop.TargetFrameRate);

		_loop.SetTargetPerFrameAsyncCooperativeTaskTimeFraction(4f);
		Assert.AreEqual(4f, _loop.TargetPerFrameAsyncCooperativeTaskTimeFraction);
	}
}
