// Created on 2026-04-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class ProgrammedCameraController : ICameraController<ProgrammedCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<ProgrammedCameraController> _controllerPool = new(&New);
	static ProgrammedCameraController New() => new();
	static ProgrammedCameraController ICameraController<ProgrammedCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(ProgrammedCameraController));
	ProgrammedCameraController() { }
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		ClearAllKeyframes();
		_controllerPool.Return(this);
	}
	#endregion
	
	#pragma warning disable CA1034 // "Do not nest publicly-visible types" -- These are most correctly namespaced specifically to this controller
	public readonly record struct PositionKeyframe(float LengthSeconds, InterpolationAlgorithm<Location> Algorithm, Location TargetValue) : ITimeKeyedItem {
		internal void ThrowIfInvalid() {
			if (!LengthSeconds.IsNonNegativeAndFinite()) {
				throw new ArgumentException($"Keyframe length must be finite and non-negative (was {LengthSeconds}).", nameof(LengthSeconds));
			}
			Algorithm.ThrowIfNullAlgorithm();
			if (!TargetValue.IsPhysicallyValid) {
				throw new ArgumentException($"Keyframe value must be physically valid (was {TargetValue}).", nameof(TargetValue));
			}
		}

		float ITimeKeyedItem.TimeKeySeconds => LengthSeconds;
	}
	public readonly record struct OrientationKeyframe(float LengthSeconds, InterpolationAlgorithm<Direction> Algorithm, Direction TargetViewDirection, Direction TargetUpDirection) : ITimeKeyedItem {
		internal void ThrowIfInvalid() {
			if (!LengthSeconds.IsNonNegativeAndFinite()) {
				throw new ArgumentException($"Keyframe length must be finite and non-negative (was {LengthSeconds}).", nameof(LengthSeconds));
			}
			Algorithm.ThrowIfNullAlgorithm();
			if (!TargetViewDirection.IsPhysicallyValidAndNotNone) {
				throw new ArgumentException($"Keyframe view direction must be physically valid and not {Direction.None} (was {TargetViewDirection}).", nameof(TargetViewDirection));
			}
			if (!TargetUpDirection.IsPhysicallyValidAndNotNone) {
				throw new ArgumentException($"Keyframe up direction must be physically valid and not {Direction.None} (was {TargetUpDirection}).", nameof(TargetUpDirection));
			}
		}
		
		float ITimeKeyedItem.TimeKeySeconds => LengthSeconds;
	}
	public readonly record struct FieldOfViewKeyframe(float LengthSeconds, InterpolationAlgorithm<Angle> Algorithm, Angle TargetValue) : ITimeKeyedItem {
		internal void ThrowIfInvalid() {
			if (!LengthSeconds.IsNonNegativeAndFinite()) {
				throw new ArgumentException($"Keyframe length must be finite and non-negative (was {LengthSeconds}).", nameof(LengthSeconds));
			}
			Algorithm.ThrowIfNullAlgorithm();
			if (!TargetValue.IsPhysicallyValid || TargetValue < Camera.FieldOfViewMin || TargetValue > Camera.FieldOfViewMax) {
				throw new ArgumentException($"Keyframe value must be physically valid and between {Camera.FieldOfViewMin}-{Camera.FieldOfViewMax} (was {TargetValue}).", nameof(TargetValue));
			}
		}
		
		float ITimeKeyedItem.TimeKeySeconds => LengthSeconds;
	}
	#pragma warning restore CA1034
	#pragma warning disable CA1001 // Warning that KeyframeTrack owns disposable fields without disposing them; but lifetime is app-wide
	sealed class KeyframeTrack<T> where T : struct, ITimeKeyedItem {
		readonly ArrayPoolBackedVector<T> _keyframes = new();
		int _curIndex;
		float _curStartTimestamp;
		
		public float TrackLengthSeconds { get; private set; }
		public AnimationWrapStyle? Wrapping { get; set; }

		public KeyframeTrack() { Clear(); }

		public void Add(T kf) {
			_keyframes.Add(kf);
			TrackLengthSeconds += kf.TimeKeySeconds;
		}
		public void Clear() {
			_keyframes.Clear();
			TrackLengthSeconds = 0f;
			_curIndex = 0;
			_curStartTimestamp = 0f;
		}
		public void Reset() {
			Clear();
			Wrapping = AnimationWrapStyle.Once;
		}
		
		public (T? PrevKeyframe, T Keyframe, float InterpDistance)? GetKeyframeAndInterpolationDistance(float timestampSecs) {
			if (_keyframes.Count == 0) return null;
			timestampSecs = Wrapping?.ApplyToTimePoint(timestampSecs, TrackLengthSeconds) ?? timestampSecs;
			
			if (timestampSecs < _curStartTimestamp) {
				_curIndex = 0;
				_curStartTimestamp = 0;
				while (_keyframes[_curIndex].TimeKeySeconds + _curStartTimestamp < timestampSecs && _curIndex < (_keyframes.Count - 1)) {
					_curStartTimestamp += _keyframes[_curIndex].TimeKeySeconds;
					_curIndex++;
				}
				var len = _keyframes[_curIndex].TimeKeySeconds;
				return ((_curIndex > 0 ? _keyframes[_curIndex - 1] : null), _keyframes[_curIndex], len > 0f ? (timestampSecs - _curStartTimestamp) / len : 1f);
			}

			var timeInToCurKeyframe = timestampSecs - _curStartTimestamp;
			var curKeyframeLength = _keyframes[_curIndex].TimeKeySeconds;
			while (timeInToCurKeyframe > curKeyframeLength && _curIndex < (_keyframes.Count - 1)) {
				_curIndex++;
				_curStartTimestamp += curKeyframeLength;
				timeInToCurKeyframe = timestampSecs - _curStartTimestamp;
				curKeyframeLength = _keyframes[_curIndex].TimeKeySeconds;
			}
			return ((_curIndex > 0 ? _keyframes[_curIndex - 1] : null), _keyframes[_curIndex], curKeyframeLength > 0f ? timeInToCurKeyframe / curKeyframeLength : 1f);
		}
	}
	#pragma warning restore CA1001
	readonly KeyframeTrack<PositionKeyframe> _positionTrack = new();
	readonly KeyframeTrack<OrientationKeyframe> _orientationTrack = new();
	readonly KeyframeTrack<FieldOfViewKeyframe> _fovTrack = new();
	Location _startPosition;
	Direction _startOrientationView;
	Direction _startOrientationUp;
	Angle _startFov;
	
	public AnimationWrapStyle? PositionTrackWrapping {
		get => _positionTrack.Wrapping;
		set => _positionTrack.Wrapping = value;
	}
	public AnimationWrapStyle? OrientationTrackWrapping {
		get => _orientationTrack.Wrapping;
		set => _orientationTrack.Wrapping = value;
	}
	public AnimationWrapStyle? FieldOfViewTrackWrapping {
		get => _fovTrack.Wrapping;
		set => _fovTrack.Wrapping = value;
	}
	
	public float PositionTrackLengthSeconds => _positionTrack.TrackLengthSeconds;
	public float OrientationTrackLengthSeconds => _orientationTrack.TrackLengthSeconds;
	public float FieldOfViewTrackLengthSeconds => _fovTrack.TrackLengthSeconds;
	
	public float CurrentTimestampSeconds { get; set; }

	public void AddPositionKeyframe(PositionKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_positionTrack.Add(keyframe);
	}

	public void AddOrientationKeyframe(OrientationKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_orientationTrack.Add(keyframe);
	}

	public void AddFieldOfViewKeyframe(FieldOfViewKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_fovTrack.Add(keyframe);
	}

	public void ClearAllKeyframes() {
		_positionTrack.Clear();
		_orientationTrack.Clear();
		_fovTrack.Clear();
	}

	void ICameraController.SetGlobalSmoothing(Strength newSmoothingStrength) { /* No-op */ }

	public void ResetParametersToDefault() {
		_positionTrack.Reset();
		_orientationTrack.Reset();
		_fovTrack.Reset();
		CurrentTimestampSeconds = 0f;
		_startPosition = Location.Origin;
		_startOrientationView = Direction.Forward;
		_startOrientationUp = Direction.Up;
		_startFov = CameraCreationConfig.DefaultFieldOfView;
	}

	public void Progress(float deltaTime) {
		if (CurrentTimestampSeconds == 0f) {
			_startPosition = Camera.Position;
			_startOrientationView = Camera.ViewDirection;
			_startOrientationUp = Camera.UpDirection;
			_startFov = Camera.VerticalFieldOfView;
		}
		
		CurrentTimestampSeconds += deltaTime;
		var positionTuple = _positionTrack.GetKeyframeAndInterpolationDistance(CurrentTimestampSeconds);
		var orientationTuple = _orientationTrack.GetKeyframeAndInterpolationDistance(CurrentTimestampSeconds);
		var fovTuple = _fovTrack.GetKeyframeAndInterpolationDistance(CurrentTimestampSeconds);
		
		if (positionTuple is { } p) {
			Camera.SetPosition(p.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(p.PrevKeyframe?.TargetValue ?? _startPosition, p.Keyframe.TargetValue, p.InterpDistance));
		}
		if (orientationTuple is { } o) {
			Camera.SetViewAndUpDirection(
				o.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(o.PrevKeyframe?.TargetViewDirection ?? _startOrientationView, o.Keyframe.TargetViewDirection, o.InterpDistance),
				o.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(o.PrevKeyframe?.TargetUpDirection ?? _startOrientationUp, o.Keyframe.TargetUpDirection, o.InterpDistance)
			);
		}
		if (fovTuple is { } f) {
			var val = f.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(f.PrevKeyframe?.TargetValue ?? _startFov, f.Keyframe.TargetValue, f.InterpDistance);
			Camera.SetVerticalFieldOfView(val.Clamp(Camera.FieldOfViewMin, Camera.FieldOfViewMax));
		}
	}
}
