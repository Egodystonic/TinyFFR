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
/// unrelated, so a camera can be mid-way through a long sweep of movement whilst its field of view snaps between several values. The one exception is an
/// orientation keyframe with a <see cref="OrientationKeyframe.LookAtTarget"/>: it looks at its target from wherever the position track (or anything else) has put
/// the camera on that frame.
/// </para>
/// <para>
/// Position keyframes travel in a straight line by default, or sweep around a point in an arc when given a <see cref="PositionKeyframe.Pivot"/>; together with
/// <see cref="OrientationKeyframe.LookAtTarget"/> this makes orbiting shots straightforward to script.
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
	
	/// <summary>
	/// One step of a scripted camera path: a location to arrive at, how long to take getting there, and how to ease along the way.
	/// </summary>
	/// <remarks>
	/// By default the camera travels in a straight line from the previous keyframe's location to this one's. Supply a <see cref="Pivot"/> to make it sweep around
	/// that point in an arc instead.
	/// </remarks>
	/// <param name="LengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera jump straight to <paramref name="TargetValue"/>.</param>
	/// <param name="Algorithm">How to interpolate from the previous keyframe's location to this one's (for example linearly, or easing in and out).</param>
	/// <param name="TargetValue">Where the camera should be by the time this keyframe finishes. Must be physically valid.</param>
	public readonly record struct PositionKeyframe(float LengthSeconds, InterpolationAlgorithm<Location> Algorithm, Location TargetValue) : ITimeKeyedItem {
		/// <summary>
		/// Creates a keyframe that sweeps the camera around <paramref name="pivot"/> in an arc, rather than moving it in a straight line.
		/// </summary>
		/// <param name="lengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera jump straight to <paramref name="targetValue"/>.</param>
		/// <param name="algorithm">How to ease along the arc from the previous keyframe's location to this one's (for example linearly, or easing in and out).</param>
		/// <param name="targetValue">Where the camera should be by the time this keyframe finishes. Must be physically valid.</param>
		/// <param name="pivot">The point the camera should sweep around on its way to <paramref name="targetValue"/>. Must be physically valid. See <see cref="Pivot"/>.</param>
		public PositionKeyframe(float lengthSeconds, InterpolationAlgorithm<Location> algorithm, Location targetValue, Location pivot) : this(lengthSeconds, algorithm, targetValue) {
			Pivot = pivot;
		}

		/// <summary>
		/// The point the camera sweeps around on its way to <see cref="TargetValue"/>, or <see langword="null"/> to travel in a straight line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When set, the camera's direction from the pivot turns along the shortest arc between where it starts and where it ends, whilst its distance from the
		/// pivot changes gradually from the starting distance to the ending distance; so the arc is circular when both ends are equally far from the pivot. Because
		/// the shortest arc is used, a single keyframe can sweep at most 180°; use several keyframes to sweep further.
		/// </para>
		/// <para>
		/// If the start and end are on exactly opposite sides of the pivot, there is no single shortest arc and the plane of the sweep is chosen arbitrarily;
		/// move the pivot slightly off that line to choose the plane yourself. If either end coincides with the pivot, the camera falls back to travelling in a
		/// straight line.
		/// </para>
		/// <para>
		/// <see cref="Algorithm"/> still controls the easing along the arc. Every built-in algorithm works here; a <see cref="InterpolationAlgorithm{T}.Custom"/>
		/// algorithm is sampled on a straight line from <see cref="Location.Origin"/> to <c>(1, 0, 0)</c>, and the X component of the result is used as the
		/// fraction of the arc travelled.
		/// </para>
		/// </remarks>
		public Location? Pivot { get; init; }

		internal void ThrowIfInvalid() {
			if (!LengthSeconds.IsNonNegativeAndFinite()) {
				throw new ArgumentException($"Keyframe length must be finite and non-negative (was {LengthSeconds}).", nameof(LengthSeconds));
			}
			Algorithm.ThrowIfNullAlgorithm();
			if (!TargetValue.IsPhysicallyValid) {
				throw new ArgumentException($"Keyframe value must be physically valid (was {TargetValue}).", nameof(TargetValue));
			}
			if (Pivot is { IsPhysicallyValid: false } pivot) {
				throw new ArgumentException($"Keyframe pivot must be physically valid (was {pivot}).", nameof(Pivot));
			}
		}

		float ITimeKeyedItem.TimeKeySeconds => LengthSeconds;
	}
	/// <summary>
	/// One step of a scripted camera path: an orientation to arrive at, how long to take getting there, and how to ease along the way.
	/// </summary>
	/// <remarks>
	/// The camera can either turn towards a fixed <see cref="TargetViewDirection"/>, or keep looking at a point in the world by supplying a <see cref="LookAtTarget"/>.
	/// </remarks>
	/// <param name="LengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera snap straight to the target orientation.</param>
	/// <param name="Algorithm">How to interpolate from the previous keyframe's orientation to this one's (for example linearly, or easing in and out).</param>
	/// <param name="TargetViewDirection">Which way the camera should be looking by the time this keyframe finishes. Must be physically valid and not <see cref="Direction.None"/>, unless <see cref="LookAtTarget"/> is set, in which case this value is ignored.</param>
	/// <param name="TargetUpDirection">Which way should be "up" for the camera by the time this keyframe finishes; this is what lets a scripted shot roll. Must be physically valid and not <see cref="Direction.None"/>.</param>
	public readonly record struct OrientationKeyframe(float LengthSeconds, InterpolationAlgorithm<Direction> Algorithm, Direction TargetViewDirection, Direction TargetUpDirection) : ITimeKeyedItem {
		/// <summary>
		/// Creates a keyframe that turns the camera to look at <paramref name="lookAtTarget"/>, and keeps it looking there as the camera moves.
		/// </summary>
		/// <remarks>
		/// <see cref="TargetViewDirection"/> is set to <see cref="Direction.None"/> on keyframes created this way.
		/// </remarks>
		/// <param name="lengthSeconds">How long this keyframe takes to play, in seconds. Must be finite and not negative; a length of <c>0f</c> makes the camera snap straight to looking at <paramref name="lookAtTarget"/>.</param>
		/// <param name="algorithm">How to interpolate from the previous keyframe's orientation to this one's (for example linearly, or easing in and out).</param>
		/// <param name="lookAtTarget">The point the camera should look at. Must be physically valid. See <see cref="LookAtTarget"/>.</param>
		/// <param name="targetUpDirection">Which way should be "up" for the camera by the time this keyframe finishes. Must be physically valid and not <see cref="Direction.None"/>.</param>
		public OrientationKeyframe(float lengthSeconds, InterpolationAlgorithm<Direction> algorithm, Location lookAtTarget, Direction targetUpDirection) : this(lengthSeconds, algorithm, Direction.None, targetUpDirection) {
			LookAtTarget = lookAtTarget;
		}

		/// <summary>
		/// A point the camera should look at, or <see langword="null"/> to turn towards <see cref="TargetViewDirection"/> instead. When set, <see cref="TargetViewDirection"/> is ignored.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The direction to the target is worked out afresh on every <see cref="Progress"/> from the camera's position on that frame (after the position track has
		/// been applied), so the camera keeps looking at the target as it moves. The camera eases from the previous keyframe's view direction towards the target
		/// over the length of this keyframe; if the previous keyframe looks at the same target, the camera stays fixed on it throughout. To lock on to a target
		/// immediately, precede this keyframe with a zero-length keyframe that looks at the same target.
		/// </para>
		/// <para>
		/// Once this keyframe has finished (for example whilst the track holds its final keyframe), the camera continues to look at the target wherever it moves.
		/// On any frame where the camera is exactly at the target, it keeps its current view direction.
		/// </para>
		/// </remarks>
		public Location? LookAtTarget { get; init; }

		internal void ThrowIfInvalid() {
			if (!LengthSeconds.IsNonNegativeAndFinite()) {
				throw new ArgumentException($"Keyframe length must be finite and non-negative (was {LengthSeconds}).", nameof(LengthSeconds));
			}
			Algorithm.ThrowIfNullAlgorithm();
			if (LookAtTarget is { } lookAtTarget) {
				if (!lookAtTarget.IsPhysicallyValid) {
					throw new ArgumentException($"Keyframe look-at target must be physically valid (was {lookAtTarget}).", nameof(LookAtTarget));
				}
			}
			else if (!TargetViewDirection.IsPhysicallyValidAndNotNone) {
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
		
		Location? positionThisFrame = null;
		if (positionTuple is { } p) {
			var position = EvaluatePosition(p.Keyframe, p.PrevKeyframe?.TargetValue ?? _startPosition, p.InterpDistance);
			Camera.SetPosition(position);
			positionThisFrame = position;
		}
		if (orientationTuple is { } o) {
			var startView = o.PrevKeyframe is { } prev
				? ResolveViewDirection(prev.TargetViewDirection, prev.LookAtTarget, positionThisFrame)
				: _startOrientationView;
			var endView = ResolveViewDirection(o.Keyframe.TargetViewDirection, o.Keyframe.LookAtTarget, positionThisFrame);
			Camera.SetViewAndUpDirection(
				o.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(startView, endView, o.InterpDistance),
				o.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(o.PrevKeyframe?.TargetUpDirection ?? _startOrientationUp, o.Keyframe.TargetUpDirection, o.InterpDistance)
			);
		}
		if (fovTuple is { } f) {
			var val = f.Keyframe.Algorithm.UnsafeGetValueSkipNullCheck(f.PrevKeyframe?.TargetValue ?? _startFov, f.Keyframe.TargetValue, f.InterpDistance);
			Camera.SetVerticalFieldOfView(val.Clamp(Camera.FieldOfViewMin, Camera.FieldOfViewMax));
		}
	}
	
	static Location EvaluatePosition(in PositionKeyframe keyframe, Location startPosition, float interpDistance) {
		if (keyframe.Pivot is not { } pivot) return keyframe.Algorithm.UnsafeGetValueSkipNullCheck(startPosition, keyframe.TargetValue, interpDistance);

		var startVect = startPosition - pivot;
		var endVect = keyframe.TargetValue - pivot;
		var startDirection = startVect.Direction;
		var endDirection = endVect.Direction;
		if (startDirection == Direction.None || endDirection == Direction.None) {
			return keyframe.Algorithm.UnsafeGetValueSkipNullCheck(startPosition, keyframe.TargetValue, interpDistance);
		}

		var easedDistance = keyframe.Algorithm.UnsafeGetValueSkipNullCheck(Location.Origin, PivotEasingProbeEnd, interpDistance).X;
		var startLength = startVect.Length;
		var length = startLength + (endVect.Length - startLength) * easedDistance;
		return pivot + Direction.Interpolate(startDirection, endDirection, easedDistance) * length;
	}
	static readonly Location PivotEasingProbeEnd = new(1f, 0f, 0f);

	Direction ResolveViewDirection(Direction viewDirection, Location? lookAtTarget, Location? positionThisFrame) {
		if (lookAtTarget is not { } target) return viewDirection;
		var result = (positionThisFrame ?? Camera.Position).DirectionTo(target);
		return result == Direction.None ? Camera.ViewDirection : result;
	}

	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) { /* no-op */ }
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) { /* no-op */ }
}
