// Created on 2026-04-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Plays a camera along a pre-scripted path, driven by keyframes rather than by user input.
/// </summary>
/// <remarks>
/// <para>
/// Where the other controllers turn live input in to camera movement, this one plays back movement decided in advance, which makes it the right choice for
/// cinematics, cut-scenes, fly-throughs and replays.
/// </para>
/// <para>
/// The camera is driven by three independent tracks (position, orientation and field of view), each a sequence of keyframes. A keyframe names a value to reach, how
/// long to take getting there, and how to ease between the two. The tracks advance together on one clock (<see cref="CurrentTimestampSeconds"/>) but are otherwise
/// unrelated, so a camera can be mid-way through a long sweep of movement whilst its field of view snaps between several values.
/// </para>
/// <para>
/// A track with no keyframes leaves that aspect of the camera alone entirely, so it is perfectly reasonable to script only the position and control the orientation
/// by some other means.
/// </para>
/// </remarks>
public sealed class ProgrammedCameraController : ICameraController<ProgrammedCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<ProgrammedCameraController> _controllerPool = new(&New);
	static ProgrammedCameraController New() => new();
	static ProgrammedCameraController ICameraController<ProgrammedCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(ProgrammedCameraController));
	ProgrammedCameraController() { }
	/// <summary>
	/// Discards every keyframe, detaches this controller from its <see cref="Camera"/>, and returns it to the pool it was rented from.
	/// </summary>
	/// <remarks>
	/// The camera itself is not disposed and stays wherever the script last left it; only this controller stops working. Controllers are pooled and reused, so any
	/// further use of this instance after disposal will throw.
	/// </remarks>
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		ClearAllKeyframes();
		_controllerPool.Return(this);
	}
	#endregion
	
	#pragma warning disable CA1034 // "Do not nest publicly-visible types" -- These are most correctly namespaced specifically to this controller
	/// <summary>
	/// One step of a scripted camera path: a location to arrive at, how long to take getting there, and how to ease along the way.
	/// </summary>
	/// <param name="LengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera jump straight to <paramref name="TargetValue"/>.</param>
	/// <param name="Algorithm">How to interpolate from the previous keyframe's location to this one's (for example linearly, or easing in and out).</param>
	/// <param name="TargetValue">Where the camera should be by the time this keyframe finishes. Must be physically valid.</param>
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
	/// <summary>
	/// One step of a scripted camera path: an orientation to arrive at, how long to take getting there, and how to ease along the way.
	/// </summary>
	/// <param name="LengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera snap straight to the target orientation.</param>
	/// <param name="Algorithm">How to interpolate from the previous keyframe's orientation to this one's (for example linearly, or easing in and out).</param>
	/// <param name="TargetViewDirection">Which way the camera should be looking by the time this keyframe finishes. Must be physically valid and not <see cref="Direction.None"/>.</param>
	/// <param name="TargetUpDirection">Which way should be "up" for the camera by the time this keyframe finishes; this is what lets a scripted shot roll. Must be physically valid and not <see cref="Direction.None"/>.</param>
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
	/// <summary>
	/// One step of a scripted camera path: a field of view to arrive at, how long to take getting there, and how to ease along the way.
	/// </summary>
	/// <param name="LengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the field of view jump straight to <paramref name="TargetValue"/>.</param>
	/// <param name="Algorithm">How to interpolate from the previous keyframe's field of view to this one's (for example linearly, or easing in and out).</param>
	/// <param name="TargetValue">The vertical field of view the camera should have by the time this keyframe finishes. Must be physically valid and between <see cref="Camera.FieldOfViewMin"/> and <see cref="Camera.FieldOfViewMax"/>.</param>
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
	
	/// <summary>
	/// What happens when the position track reaches its end, or <see langword="null"/> to let the clock run past the end without wrapping.
	/// </summary>
	/// <remarks>
	/// Each track wraps independently, so a short looping position track can run alongside a longer one on another track.
	/// </remarks>
	public AnimationWrapStyle? PositionTrackWrapping {
		get => _positionTrack.Wrapping;
		set => _positionTrack.Wrapping = value;
	}
	/// <summary>
	/// What happens when the orientation track reaches its end, or <see langword="null"/> to let the clock run past the end without wrapping.
	/// </summary>
	/// <remarks>
	/// Each track wraps independently, so a short looping orientation track can run alongside a longer one on another track.
	/// </remarks>
	public AnimationWrapStyle? OrientationTrackWrapping {
		get => _orientationTrack.Wrapping;
		set => _orientationTrack.Wrapping = value;
	}
	/// <summary>
	/// What happens when the field of view track reaches its end, or <see langword="null"/> to let the clock run past the end without wrapping.
	/// </summary>
	/// <remarks>
	/// Each track wraps independently, so a short looping field of view track can run alongside a longer one on another track.
	/// </remarks>
	public AnimationWrapStyle? FieldOfViewTrackWrapping {
		get => _fovTrack.Wrapping;
		set => _fovTrack.Wrapping = value;
	}
	
	/// <summary>
	/// The combined length of every keyframe on the position track, in seconds; i.e. how long that track takes to play through once.
	/// </summary>
	public float PositionTrackLengthSeconds => _positionTrack.TrackLengthSeconds;
	/// <summary>
	/// The combined length of every keyframe on the orientation track, in seconds; i.e. how long that track takes to play through once.
	/// </summary>
	public float OrientationTrackLengthSeconds => _orientationTrack.TrackLengthSeconds;
	/// <summary>
	/// The combined length of every keyframe on the field of view track, in seconds; i.e. how long that track takes to play through once.
	/// </summary>
	public float FieldOfViewTrackLengthSeconds => _fovTrack.TrackLengthSeconds;
	
	/// <summary>
	/// How far through the script playback currently is, in seconds.
	/// </summary>
	/// <remarks>
	/// This advances by <c>deltaTime</c> on every <see cref="Progress(float)"/>. Setting it directly seeks the script, which is how you scrub, restart or skip ahead;
	/// setting it back to <c>0f</c> restarts playback and re-captures the camera's current placement as the starting point that the first keyframe of each track
	/// interpolates away from.
	/// </remarks>
	public float CurrentTimestampSeconds { get; set; }

	/// <summary>
	/// Appends a keyframe to the end of the position track.
	/// </summary>
	/// <remarks>
	/// Keyframes play in the order they are added, and each one's <see cref="PositionKeyframe.LengthSeconds"/> is the time taken to reach it from the one before. The first
	/// keyframe on a track interpolates from whatever the camera's position was when playback began.
	/// </remarks>
	/// <param name="keyframe">The keyframe to append. Its values must satisfy the constraints described on <see cref="PositionKeyframe"/>.</param>
	/// <exception cref="ArgumentException">Thrown when any of <paramref name="keyframe"/>'s values is outside its permitted range.</exception>
	public void AddPositionKeyframe(PositionKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_positionTrack.Add(keyframe);
	}

	/// <summary>
	/// Appends a keyframe to the end of the orientation track.
	/// </summary>
	/// <remarks>
	/// Keyframes play in the order they are added, and each one's <see cref="OrientationKeyframe.LengthSeconds"/> is the time taken to reach it from the one before. The first
	/// keyframe on a track interpolates from whatever the camera's orientation was when playback began.
	/// </remarks>
	/// <param name="keyframe">The keyframe to append. Its values must satisfy the constraints described on <see cref="OrientationKeyframe"/>.</param>
	/// <exception cref="ArgumentException">Thrown when any of <paramref name="keyframe"/>'s values is outside its permitted range.</exception>
	public void AddOrientationKeyframe(OrientationKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_orientationTrack.Add(keyframe);
	}

	/// <summary>
	/// Appends a keyframe to the end of the field of view track.
	/// </summary>
	/// <remarks>
	/// Keyframes play in the order they are added, and each one's <see cref="FieldOfViewKeyframe.LengthSeconds"/> is the time taken to reach it from the one before. The first
	/// keyframe on a track interpolates from whatever the camera's field of view was when playback began.
	/// </remarks>
	/// <param name="keyframe">The keyframe to append. Its values must satisfy the constraints described on <see cref="FieldOfViewKeyframe"/>.</param>
	/// <exception cref="ArgumentException">Thrown when any of <paramref name="keyframe"/>'s values is outside its permitted range.</exception>
	public void AddFieldOfViewKeyframe(FieldOfViewKeyframe keyframe) {
		keyframe.ThrowIfInvalid();
		_fovTrack.Add(keyframe);
	}

	/// <summary>
	/// Removes every keyframe from all three tracks, leaving the camera wherever it currently is.
	/// </summary>
	/// <remarks>
	/// This does not reset <see cref="CurrentTimestampSeconds"/>; set that to <c>0f</c> as well if you intend to script a fresh sequence.
	/// </remarks>
	public void ClearAllKeyframes() {
		_positionTrack.Clear();
		_orientationTrack.Clear();
		_fovTrack.Clear();
	}

	void ICameraController.SetGlobalSmoothing(SmoothingStrength newSmoothingStrength) { /* No-op */ }

	/// <inheritdoc />
	/// <remarks>
	/// For this controller that means discarding every keyframe, returning all three tracks to <see cref="AnimationWrapStyle.Once"/>, and rewinding
	/// <see cref="CurrentTimestampSeconds"/> to <c>0f</c>.
	/// </remarks>
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

	/// <inheritdoc />
	/// <remarks>
	/// Advances <see cref="CurrentTimestampSeconds"/> and applies each track's interpolated value to the camera. On the frame where the timestamp is <c>0f</c> the
	/// camera's current placement is captured as the starting point that the first keyframe of each track interpolates away from, so position the camera before
	/// beginning playback rather than after.
	/// </remarks>
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
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) { /* no-op */ }
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) { /* no-op */ }
}
