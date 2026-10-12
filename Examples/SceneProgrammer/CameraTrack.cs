using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

enum EasingStyle {
	Linear,
	Natural,
	ExtraSmooth,
	EaseIn,
	EaseOut,
	Overshoot
}

sealed partial class CameraKeyframe : ObservableObject {
	public Location Position { get; set; }
	public Direction ViewDirection { get; set; }
	public Direction UpDirection { get; set; }

	[ObservableProperty] public partial int Number { get; set; }
	[ObservableProperty] public partial decimal DurationSeconds { get; set; } = 3m;
	[ObservableProperty] public partial EasingStyle Easing { get; set; } = EasingStyle.Natural;
	[ObservableProperty] public partial decimal FieldOfViewDegrees { get; set; }
	[ObservableProperty] public partial LookAtOption? LookAt { get; set; } = LookAtOption.None;
	[ObservableProperty] public partial Location? LookAtPoint { get; set; }
	[ObservableProperty] public partial Location? PivotPoint { get; set; }
	[ObservableProperty] public partial bool DepthOfFieldEnabled { get; set; }
	[ObservableProperty] public partial decimal FocusDistance { get; set; } = 5m;
	[ObservableProperty] public partial CameraExposurePreset ExposurePreset { get; set; }
	[ObservableProperty] public partial decimal ExposureBrightness { get; set; } = 1m;

	public SceneObject? LookAtTarget => LookAt?.Target;
	public Location? LookAtLocation => LookAt == LookAtOption.Point ? LookAtPoint : LookAtTarget?.CalculateWorldBoundingBox().Position;
	public bool CanHavePivot => Number > 1;
	public string? LookAtPointDescription => LookAt == LookAtOption.Point && LookAtPoint is { } point ? $"Looking at {Describe(point)}" : null;
	public string? PivotDescription => PivotPoint is { } pivot && CanHavePivot ? $"Sweeping around {Describe(pivot)}" : null;

	public string Summary {
		get {
			var lookAt = LookAtLocation != null ? $"  → {LookAt!.Name}" : "";
			var pivot = PivotPoint != null && CanHavePivot ? "  ↻ pivot" : "";
			return Number == 1 ? $"#1  Start{lookAt}" : $"#{Number}  {DurationSeconds:0.##}s  {Easing}{lookAt}{pivot}";
		}
	}

	public CameraKeyframe(Camera camera, CameraExposurePreset exposurePreset) {
		CaptureFrom(camera);
		DepthOfFieldEnabled = camera.FocusDistance != null;
		if (camera.FocusDistance is { } focusDistance) FocusDistance = Math.Round((decimal) focusDistance, 2);
		ExposurePreset = exposurePreset;
	}

	public void CaptureFrom(Camera camera) {
		Position = camera.Position;
		ViewDirection = camera.ViewDirection;
		UpDirection = camera.UpDirection;
		FieldOfViewDegrees = Math.Round((decimal) camera.VerticalFieldOfView.Degrees, 1);
		OnPropertyChanged(nameof(Position));
	}

	public Direction GetViewDirection() => LookAtLocation is { } target ? Position.DirectionTo(target) : ViewDirection;

	public float? GetFocusDistance() => DepthOfFieldEnabled ? MathF.Max((float) FocusDistance, 0.01f) : null;

	public CameraExposureParams GetExposure() {
		return (ExposurePreset.ToExposureParams() * (float) ExposureBrightness).Clamp(
			new CameraExposureParams(CameraExposureParams.ApertureMin, CameraExposureParams.ShutterSpeedMin, CameraExposureParams.SensitivityMin),
			new CameraExposureParams(CameraExposureParams.ApertureMax, CameraExposureParams.ShutterSpeedMax, CameraExposureParams.SensitivityMax)
		);
	}

	partial void OnDurationSecondsChanged(decimal value) => OnPropertyChanged(nameof(Summary));
	partial void OnEasingChanged(EasingStyle value) => OnPropertyChanged(nameof(Summary));

	partial void OnNumberChanged(int value) {
		OnPropertyChanged(nameof(Summary));
		OnPropertyChanged(nameof(CanHavePivot));
		OnPropertyChanged(nameof(PivotDescription));
	}

	partial void OnLookAtChanged(LookAtOption? value) {
		OnPropertyChanged(nameof(Summary));
		OnPropertyChanged(nameof(LookAtPointDescription));
	}

	partial void OnLookAtPointChanged(Location? value) {
		OnPropertyChanged(nameof(Summary));
		OnPropertyChanged(nameof(LookAtPointDescription));
	}

	partial void OnPivotPointChanged(Location? value) {
		OnPropertyChanged(nameof(Summary));
		OnPropertyChanged(nameof(PivotDescription));
	}

	static string Describe(Location location) => $"({location.X:0.##}, {location.Y:0.##}, {location.Z:0.##})";
}

sealed class CameraTrack {
	public ObservableCollection<CameraKeyframe> Keyframes { get; } = new();
	public AnimationWrapStyle Wrapping { get; set; } = AnimationWrapStyle.Once;

	public float LengthSeconds {
		get {
			var result = 0f;
			for (var i = 1; i < Keyframes.Count; ++i) result += (float) Keyframes[i].DurationSeconds;
			return result;
		}
	}

	public float PlaybackLengthSeconds => Wrapping switch {
		AnimationWrapStyle.Once => LengthSeconds,
		AnimationWrapStyle.OncePingPonged => LengthSeconds * 2f,
		_ => Single.PositiveInfinity
	};

	public void ApplyTo(ProgrammedCameraController controller) {
		controller.ResetParametersToDefault();
		if (Keyframes.Count == 0) return;

		var camera = controller.Camera;
		var start = Keyframes[0];
		camera.Position = start.Position;
		camera.SetViewAndUpDirection(start.GetViewDirection(), start.UpDirection);
		camera.SetVerticalFieldOfView(Angle.FromDegrees((float) start.FieldOfViewDegrees));
		camera.FocusDistance = start.GetFocusDistance();
		camera.Exposure = start.GetExposure();

		for (var i = 1; i < Keyframes.Count; ++i) {
			var keyframe = Keyframes[i];
			var length = (float) keyframe.DurationSeconds;
			controller.AddPositionKeyframe(keyframe.PivotPoint is { } pivot
				? new(length, GetAlgorithm<Location>(keyframe.Easing), keyframe.Position, pivot)
				: new(length, GetAlgorithm<Location>(keyframe.Easing), keyframe.Position)
			);
			controller.AddOrientationKeyframe(keyframe.LookAtLocation is { } target
				? new(length, GetAlgorithm<Direction>(keyframe.Easing), target, keyframe.UpDirection)
				: new(length, GetAlgorithm<Direction>(keyframe.Easing), keyframe.ViewDirection, keyframe.UpDirection)
			);
			controller.AddFieldOfViewKeyframe(new(length, GetAlgorithm<Angle>(keyframe.Easing), Angle.FromDegrees((float) keyframe.FieldOfViewDegrees)));
			controller.AddFocusDistanceKeyframe(new(length, GetAlgorithm<Real>(keyframe.Easing), keyframe.GetFocusDistance()));
			controller.AddExposureKeyframe(new(length, GetAlgorithm<CameraExposureParams>(keyframe.Easing), keyframe.GetExposure()));
		}

		controller.PositionTrackWrapping = Wrapping;
		controller.OrientationTrackWrapping = Wrapping;
		controller.FieldOfViewTrackWrapping = Wrapping;
		controller.FocusDistanceTrackWrapping = Wrapping;
		controller.ExposureTrackWrapping = Wrapping;
		controller.Progress(0f);
	}

	public void Renumber() {
		for (var i = 0; i < Keyframes.Count; ++i) Keyframes[i].Number = i + 1;
	}

	static InterpolationAlgorithm<T> GetAlgorithm<T>(EasingStyle easing) where T : IInterpolatable<T> {
		return easing switch {
			EasingStyle.Linear => InterpolationAlgorithm<T>.Linear(),
			EasingStyle.ExtraSmooth => InterpolationAlgorithm<T>.Natural(additionalSmoothing: true),
			EasingStyle.EaseIn => InterpolationAlgorithm<T>.AccelerateFromSlow(),
			EasingStyle.EaseOut => InterpolationAlgorithm<T>.DecelerateFromFast(),
			EasingStyle.Overshoot => InterpolationAlgorithm<T>.DecelerateFromFastWithOvershoot(),
			_ => InterpolationAlgorithm<T>.Natural()
		};
	}
}
