// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;
using NSubstitute;

namespace Egodystonic.TinyFFR;

[TestFixture]
class ProgrammedCameraControllerTest {
	const float TestTolerance = 0.001f;
	static readonly ResourceHandle<Camera> TestHandle = new(1);

	ICameraImplProvider _impl = null!;
	Camera _camera;
	ProgrammedCameraController _controller = null!;
	float? _storedFocusDistance;
	CameraExposureParams _storedExposure;
	int _focusDistanceSetCount;
	int _exposureSetCount;

	[SetUp]
	public void SetUpTest() {
		_storedFocusDistance = null;
		_storedExposure = CameraExposurePreset.InsideBrightLighting;
		_focusDistanceSetCount = 0;
		_exposureSetCount = 0;

		_impl = Substitute.For<ICameraImplProvider>();
		_impl.IsDisposed(Arg.Any<ResourceHandle<Camera>>()).Returns(false);
		_impl.When(x => x.SetFocusDistance(Arg.Any<ResourceHandle<Camera>>(), Arg.Any<float?>()))
			.Do(callInfo => {
				_storedFocusDistance = callInfo.Arg<float?>();
				++_focusDistanceSetCount;
			});
		_impl.GetFocusDistance(Arg.Any<ResourceHandle<Camera>>()).Returns(_ => _storedFocusDistance);
		_impl.When(x => x.SetExposure(Arg.Any<ResourceHandle<Camera>>(), Arg.Any<CameraExposureParams>()))
			.Do(callInfo => {
				_storedExposure = callInfo.Arg<CameraExposureParams>();
				++_exposureSetCount;
			});
		_impl.GetExposure(Arg.Any<ResourceHandle<Camera>>()).Returns(_ => _storedExposure);

		_camera = new Camera(TestHandle, _impl);
		_controller = _camera.CreateController<ProgrammedCameraController>();
	}

	[TearDown]
	public void TearDownTest() {
		_controller.Dispose();
		_camera.Dispose();
	}

	[Test]
	public void ShouldInterpolateFocusDistanceBetweenNonNullValues() {
		_storedFocusDistance = 2f;
		_controller.AddFocusDistanceKeyframe(new(2f, InterpolationAlgorithm<Real>.Linear(), 6f));

		_controller.Progress(1f);
		Assert.AreEqual(4f, _storedFocusDistance!.Value, TestTolerance);
		_controller.Progress(1f);
		Assert.AreEqual(6f, _storedFocusDistance!.Value, TestTolerance);
		_controller.Progress(1f);
		Assert.AreEqual(6f, _storedFocusDistance!.Value, TestTolerance);
		Assert.AreEqual(2f, _controller.FocusDistanceTrackLengthSeconds, TestTolerance);
	}

	[Test]
	public void ShouldSwitchFocusDistanceOnlyAtKeyframeEndWhenEitherSideIsNull() {
		_controller.AddFocusDistanceKeyframe(new(2f, InterpolationAlgorithm<Real>.Linear(), 5f));
		_controller.AddFocusDistanceKeyframe(new(2f, InterpolationAlgorithm<Real>.Linear(), null));

		_controller.Progress(1f);
		Assert.IsNull(_storedFocusDistance);
		_controller.Progress(0.9f);
		Assert.IsNull(_storedFocusDistance);
		_controller.Progress(0.1f);
		Assert.AreEqual(5f, _storedFocusDistance!.Value, TestTolerance);

		_controller.Progress(1f);
		Assert.AreEqual(5f, _storedFocusDistance!.Value, TestTolerance);
		_controller.Progress(1f);
		Assert.IsNull(_storedFocusDistance);
	}

	[Test]
	public void ShouldApplyZeroLengthFocusDistanceKeyframeImmediately() {
		_controller.AddFocusDistanceKeyframe(new(0f, InterpolationAlgorithm<Real>.Linear(), 3f));
		_controller.AddFocusDistanceKeyframe(new(4f, InterpolationAlgorithm<Real>.Linear(), 7f));

		_controller.Progress(0f);
		Assert.AreEqual(3f, _storedFocusDistance!.Value, TestTolerance);
		_controller.Progress(2f);
		Assert.AreEqual(5f, _storedFocusDistance!.Value, TestTolerance);
	}

	[Test]
	public void ShouldInterpolateExposureEvenlyInStops() {
		var start = CameraExposurePreset.OutsideMidday.ToExposureParams();
		var end = CameraExposurePreset.OutsideTwilight.ToExposureParams();
		_storedExposure = start;
		_controller.AddExposureKeyframe(new(2f, InterpolationAlgorithm<CameraExposureParams>.Linear(), CameraExposurePreset.OutsideTwilight));

		_controller.Progress(1f);
		Assert.AreEqual((start.Ev100 + end.Ev100) * 0.5f, _storedExposure.Ev100, TestTolerance);
		_controller.Progress(1f);
		Assert.AreEqual(end.Ev100, _storedExposure.Ev100, TestTolerance);
		Assert.AreEqual(2f, _controller.ExposureTrackLengthSeconds, TestTolerance);
	}

	[Test]
	public void ShouldLeaveFocusDistanceAndExposureAloneWhenTracksAreEmpty() {
		_controller.AddFieldOfViewKeyframe(new(1f, InterpolationAlgorithm<Angle>.Linear(), Angle.FromDegrees(60f)));
		_controller.Progress(0.5f);
		_controller.Progress(1f);

		Assert.AreEqual(0, _focusDistanceSetCount);
		Assert.AreEqual(0, _exposureSetCount);
	}

	[Test]
	public void ShouldClearAndResetNewTracks() {
		_controller.AddFocusDistanceKeyframe(new(1f, InterpolationAlgorithm<Real>.Linear(), 3f));
		_controller.AddExposureKeyframe(new(1f, InterpolationAlgorithm<CameraExposureParams>.Linear(), CameraExposurePreset.OutsideOvercast));
		_controller.FocusDistanceTrackWrapping = AnimationWrapStyle.Loop;
		_controller.ExposureTrackWrapping = null;

		_controller.ClearAllKeyframes();
		Assert.AreEqual(0f, _controller.FocusDistanceTrackLengthSeconds);
		Assert.AreEqual(0f, _controller.ExposureTrackLengthSeconds);
		Assert.AreEqual(AnimationWrapStyle.Loop, _controller.FocusDistanceTrackWrapping);

		_controller.ResetParametersToDefault();
		Assert.AreEqual(AnimationWrapStyle.Once, _controller.FocusDistanceTrackWrapping);
		Assert.AreEqual(AnimationWrapStyle.Once, _controller.ExposureTrackWrapping);
	}

	[Test]
	public void ShouldRejectInvalidKeyframes() {
		var focusAlgo = InterpolationAlgorithm<Real>.Linear();
		var exposureAlgo = InterpolationAlgorithm<CameraExposureParams>.Linear();

		Assert.Throws<ArgumentException>(() => _controller.AddFocusDistanceKeyframe(new(1f, focusAlgo, 0f)));
		Assert.Throws<ArgumentException>(() => _controller.AddFocusDistanceKeyframe(new(1f, focusAlgo, -1f)));
		Assert.Throws<ArgumentException>(() => _controller.AddFocusDistanceKeyframe(new(1f, focusAlgo, Single.NaN)));
		Assert.Throws<ArgumentException>(() => _controller.AddFocusDistanceKeyframe(new(1f, focusAlgo, Single.PositiveInfinity)));
		Assert.Throws<ArgumentException>(() => _controller.AddFocusDistanceKeyframe(new(-1f, focusAlgo, 1f)));
		Assert.Throws<InvalidObjectException>(() => _controller.AddFocusDistanceKeyframe(new(1f, default, 1f)));
		Assert.DoesNotThrow(() => _controller.AddFocusDistanceKeyframe(new(1f, focusAlgo, null)));

		Assert.Throws<ArgumentException>(() => _controller.AddExposureKeyframe(new(1f, exposureAlgo, new CameraExposureParams(0f, 1f / 60f, 100f))));
		Assert.Throws<ArgumentException>(() => _controller.AddExposureKeyframe(new(1f, exposureAlgo, new CameraExposureParams(2f, Single.NaN, 100f))));
		Assert.Throws<ArgumentException>(() => _controller.AddExposureKeyframe(new(1f, exposureAlgo, new CameraExposureParams(2f, 1f / 60f, CameraExposureParams.SensitivityMax * 2f))));
		Assert.Throws<ArgumentException>(() => _controller.AddExposureKeyframe(new(Single.NaN, exposureAlgo, CameraExposurePreset.OutsideMidday)));
		Assert.Throws<InvalidObjectException>(() => _controller.AddExposureKeyframe(new(1f, default, CameraExposurePreset.OutsideMidday)));
		Assert.DoesNotThrow(() => _controller.AddExposureKeyframe(new(1f, exposureAlgo, CameraExposurePreset.OutsideStarlight)));
	}
}
