// Created on 2026-04-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Follows a moving target from behind, keeping the camera at a set distance, height and sideways offset from it.
/// </summary>
/// <remarks>
/// <para>
/// This is the classic third-person "chase camera" used for vehicles and characters. Rather than positioning the camera yourself, you tell the controller where the
/// followed object is each frame (via <see cref="Target"/>, and optionally <see cref="TargetForward"/> and <see cref="TargetUp"/>) and it places the camera behind
/// and above it, aimed at a point slightly ahead of it.
/// </para>
/// <para>
/// The remaining properties shape the framing: <see cref="FollowDistance"/>, <see cref="FollowHeight"/> and <see cref="FollowLateralOffset"/> set where the camera
/// sits relative to the target, whilst <see cref="LookaheadDistance"/> and the two view-shift multipliers set where it looks. Set them once for a fixed chase
/// camera, or adjust them each frame for a more dynamic one.
/// </para>
/// </remarks>
public sealed class FollowCameraController : ICameraController<FollowCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<FollowCameraController> _controllerPool = new(&New);
	static FollowCameraController New() => new();
	static FollowCameraController ICameraController<FollowCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FollowCameraController));
	FollowCameraController() { }
	/// <summary>
	/// Disposes this controller and detaches it from the target <see cref="Camera"/>.
	/// </summary>
	/// <remarks>
	/// The camera itself is not disposed and keeps whatever position and orientation it was last given; only this controller stops working.
	/// </remarks>
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		_controllerPool.Return(this);
	}
	#endregion
	
	/// <summary>
	/// The default value for <see cref="Target"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location TargetDefault = Location.Origin;
	/// <summary>
	/// The default value for <see cref="TargetForward"/>: <see cref="Direction.Forward"/>.
	/// </summary>
	public static readonly Direction TargetForwardDefault = Direction.Forward;
	/// <summary>
	/// The default value for <see cref="TargetUp"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction TargetUpDefault = Direction.Up;
	/// <summary>
	/// The default value for <see cref="FollowDistance"/>: <c>0.6f</c>.
	/// </summary>
	public static readonly float FollowDistanceDefault = 0.6f;
	/// <summary>
	/// The default value for <see cref="FollowHeight"/>: <c>0.3f</c>.
	/// </summary>
	public static readonly float FollowHeightDefault = 0.3f;
	/// <summary>
	/// The default value for <see cref="FollowLateralOffset"/>: <c>0.4f</c>.
	/// </summary>
	public static readonly float FollowLateralOffsetDefault = 0.4f;
	/// <summary>
	/// The default value for <see cref="LookaheadDistance"/>: <c>2.4f</c>.
	/// </summary>
	public static readonly float LookaheadDistanceDefault = 2.4f;
	/// <summary>
	/// The default value for <see cref="HeightViewShiftMultiplier"/>: <c>0.44f</c>.
	/// </summary>
	public static readonly float HeightViewShiftMultiplierDefault = 0.44f;
	/// <summary>
	/// The default value for <see cref="LateralOffsetViewShiftMultiplier"/>: <c>0.28f</c>.
	/// </summary>
	public static readonly float LateralOffsetViewShiftMultiplierDefault = 0.28f;

	readonly Spring3DBasedCameraSetpoint _positionRelativeSetpoint = new();
	readonly CameraEffectStrengthMap _positionSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.1f,
		Standard: 0.2f,
		Strong: 0.3f,
		VeryStrong: 0.4f
	);
	
	readonly Spring3DBasedCameraSetpoint _lookRelativeSetpoint = new();
	readonly CameraEffectStrengthMap _trackingSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.5f,
		Mild: 0.7f,
		Standard: 1f,
		Strong: 1.4f,
		VeryStrong: 2f
	);

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards its position behind the target.
	/// </summary>
	/// <remarks>
	/// This is what stops the camera snapping rigidly to the target and gives a chase camera its sense of weight. Use
	/// <see cref="SetCustomPositionSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionRelativeSetpoint.HalfLife);
		set => _positionRelativeSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the point the camera is aimed at.
	/// </summary>
	/// <remarks>
	/// Smoothing the aim separately from the position lets the camera swing round to face a turning target more gently than it follows it. Use
	/// <see cref="SetCustomTrackingSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength TrackingSmoothingStrength {
		get => _trackingSmoothingStrengthMap.From(_lookRelativeSetpoint.HalfLife);
		set => _lookRelativeSetpoint.HalfLife = _trackingSmoothingStrengthMap.From(value);
	}

	/// <summary>
	/// Where the followed object currently is. Defaults to <see cref="TargetDefault"/>.
	/// </summary>
	/// <remarks>
	/// Update this every frame to track a moving object. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Location Target {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
		}
	}
	/// <summary>
	/// Which way the followed object is currently facing; the camera sits behind it along this direction. Defaults to <see cref="TargetForwardDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this also re-derives <see cref="TargetUp"/>, since the two are always kept at right angles to one another. Values that are not physically valid, and
	/// <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction TargetForward {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			TargetUp = TargetUp;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// Which way is "up" for the followed object; the camera is raised along this direction. Defaults to <see cref="TargetUpDefault"/>.
	/// </summary>
	/// <remarks>
	/// This is always kept at right angles to <see cref="TargetForward"/>: the value you supply is straightened against it rather than used verbatim, so reading it
	/// back may not return exactly what you set. If you supply a direction parallel to <see cref="TargetForward"/>, an arbitrary perpendicular direction is chosen
	/// instead. Values that are not physically valid, and <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction TargetUp {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value.OrthogonalizedAgainst(TargetForward) ?? TargetForward.AnyOrthogonal();
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}

	/// <summary>
	/// How far behind the target the camera sits, in metres. Must not be negative. Defaults to <see cref="FollowDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Measured backwards along <see cref="TargetForward"/>. Non-finite and negative values are ignored rather than throwing.
	/// </remarks>
	public float FollowDistance {
		get;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdatePositionOffset();
		}
	}
	/// <summary>
	/// How far above the target the camera sits, in metres. Defaults to <see cref="FollowHeightDefault"/>.
	/// </summary>
	/// <remarks>
	/// Measured along <see cref="TargetUp"/>; negative values place the camera below the target instead. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public float FollowHeight {
		get;
		set {
			if (!Single.IsFinite(value)) return;
			field = value;
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}
	/// <summary>
	/// How far to one side of the target the camera sits, in metres. Defaults to <see cref="FollowLateralOffsetDefault"/>.
	/// </summary>
	/// <remarks>
	/// Offsetting the camera sideways is what gives an over-the-shoulder framing rather than one directly behind the target. Negative values place the camera on the
	/// opposite side. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public float FollowLateralOffset {
		get;
		set {
			if (!Single.IsFinite(value)) return;
			field = value;
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}
	/// <summary>
	/// How much the point the camera looks at shifts sideways when <see cref="FollowLateralOffset"/> changes. Must not be negative. Defaults to <see cref="LateralOffsetViewShiftMultiplierDefault"/>.
	/// </summary>
	/// <remarks>
	/// At <c>1f</c> the camera looks exactly as far to the side as it has been offset, which keeps the target in the same place on screen. Lower values let the
	/// target drift towards the edge of the frame as the offset grows; <c>0f</c> keeps the aim unaffected by the offset entirely. Non-finite and negative values are
	/// ignored rather than throwing.
	/// </remarks>
	public float LateralOffsetViewShiftMultiplier {
		get; 
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}
	/// <summary>
	/// How much the point the camera looks at shifts vertically when <see cref="FollowHeight"/> changes. Must not be negative. Defaults to <see cref="HeightViewShiftMultiplierDefault"/>.
	/// </summary>
	/// <remarks>
	/// At <c>1f</c> the camera looks exactly as far above or below the target as it has been raised or lowered. Lower values leave the camera looking more level as
	/// its height changes; <c>0f</c> keeps the aim unaffected by the height entirely. Non-finite and negative values are ignored rather than throwing.
	/// </remarks>
	public float HeightViewShiftMultiplier {
		get; 
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}
	/// <summary>
	/// How far in front of the target, along <see cref="TargetForward"/>, the camera aims. Must not be negative. Defaults to <see cref="LookaheadDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Aiming ahead of the target rather than straight at it shows more of where the target is going than where it has been, which is what gives a racing-game
	/// camera its feel. A value of <c>0f</c> aims directly at the target. Non-finite and negative values are ignored rather than throwing.
	/// </remarks>
	public float LookaheadDistance {
		get;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}

	/// <summary>
	/// Sets the position smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to its target position. <c>0f</c> disables smoothing.</param>
	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionRelativeSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the tracking smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to its aim point. <c>0f</c> disables smoothing.</param>
	public void SetCustomTrackingSmoothingStrength(float smoothingHalfLife) {
		_lookRelativeSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <inheritdoc />
	public void SetGlobalSmoothing(SmoothingStrength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		TrackingSmoothingStrength = newSmoothingStrength;
	}

	/// <summary>
	/// Updates where the followed object is and then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// The usual way to drive this controller each frame. Exactly equivalent to assigning the three target properties and then calling <see cref="Progress(float)"/>.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="target">The value to assign to <see cref="Target"/>.</param>
	/// <param name="targetForward">The value to assign to <see cref="TargetForward"/>.</param>
	/// <param name="targetUp">The value to assign to <see cref="TargetUp"/>.</param>
	public void Progress(float deltaTime, Location target, Direction targetForward, Direction targetUp) {
		Target = target;
		TargetForward = targetForward;
		TargetUp = targetUp;
		Progress(deltaTime);
	}
	/// <summary>
	/// Updates where the followed object is and how the camera is framed relative to it, then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// As the shorter overload, but also sets the three framing properties. This is useful for a camera that pulls back or rises as the target speeds up.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="target">The value to assign to <see cref="Target"/>.</param>
	/// <param name="targetForward">The value to assign to <see cref="TargetForward"/>.</param>
	/// <param name="targetUp">The value to assign to <see cref="TargetUp"/>.</param>
	/// <param name="followDistance">The value to assign to <see cref="FollowDistance"/>.</param>
	/// <param name="followHeight">The value to assign to <see cref="FollowHeight"/>.</param>
	/// <param name="followLateralOffset">The value to assign to <see cref="FollowLateralOffset"/>.</param>
	public void Progress(float deltaTime, Location target, Direction targetForward, Direction targetUp, float followDistance, float followHeight, float followLateralOffset) {
		Target = target;
		TargetForward = targetForward;
		TargetUp = targetUp;
		FollowDistance = followDistance;
		FollowHeight = followHeight;
		FollowLateralOffset = followLateralOffset;
		Progress(deltaTime);
	}
	/// <summary>
	/// Sets the three properties that govern where the camera aims, all in one call.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning the three properties individually.
	/// </remarks>
	/// <param name="lookaheadDistance">The value to assign to <see cref="LookaheadDistance"/>.</param>
	/// <param name="heightViewShiftMultiplier">The value to assign to <see cref="HeightViewShiftMultiplier"/>.</param>
	/// <param name="lateralOffsetViewShiftMultiplier">The value to assign to <see cref="LateralOffsetViewShiftMultiplier"/>.</param>
	public void SetConstraints(float lookaheadDistance, float heightViewShiftMultiplier, float lateralOffsetViewShiftMultiplier) {
		LookaheadDistance = lookaheadDistance;
		HeightViewShiftMultiplier = heightViewShiftMultiplier;
		LateralOffsetViewShiftMultiplier = lateralOffsetViewShiftMultiplier;
	}
	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets both smoothing strengths to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		LateralOffsetViewShiftMultiplier = LateralOffsetViewShiftMultiplierDefault;
		HeightViewShiftMultiplier = HeightViewShiftMultiplierDefault;
		LookaheadDistance = LookaheadDistanceDefault;
		Target = TargetDefault;
		TargetForward = TargetForwardDefault;
		TargetUp = TargetUpDefault;
		FollowDistance = FollowDistanceDefault;
		FollowHeight = FollowHeightDefault;
		FollowLateralOffset = FollowLateralOffsetDefault;
		_positionRelativeSetpoint.Reset(_positionRelativeSetpoint.TargetValue);
		_lookRelativeSetpoint.Reset(_lookRelativeSetpoint.TargetValue);
		SetGlobalSmoothing(SmoothingStrength.VeryMild);
	}
	
	void UpdatePositionOffset() {
		_positionRelativeSetpoint.TargetValue =
			(TargetForward * -FollowDistance)
			+ (TargetUp * FollowHeight)
			+ (Direction.FromDualOrthogonalization(TargetUp, TargetForward) * FollowLateralOffset);
	}
	
	void UpdateLookSetpoint() {
		_lookRelativeSetpoint.TargetValue =
			(TargetForward * LookaheadDistance)
			+ (TargetUp * FollowHeight * HeightViewShiftMultiplier)
			+ (Direction.FromDualOrthogonalization(TargetUp, TargetForward) * FollowLateralOffset * LateralOffsetViewShiftMultiplier);
	}

	/// <inheritdoc />
	public void Progress(float deltaTime) {
		_positionRelativeSetpoint.Progress(deltaTime);
		_lookRelativeSetpoint.Progress(deltaTime);

		Camera.SetPosition(Target + _positionRelativeSetpoint.CurrentValue);
		Camera.LookAt(Target + _lookRelativeSetpoint.CurrentValue, TargetUp);
	}

	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by per second, in units.</param>
	public void AdjustFollowDistance(float deltaTime, float adjustmentPerSec) => FollowDistance += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="FollowDistance"/> is adjusted by for each pixel the mouse cursor moves: <c>0.002f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowDistanceViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowDistanceSensitivityMouseCursor = 0.002f;
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="FollowDistance"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityMouseCursor"/> (<c>0.002f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowDistance += delta * (adjustmentPerPixel ?? DefaultFollowDistanceSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="FollowDistance"/> is adjusted by for each notch the mouse wheel is turned: <c>0.05f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowDistanceViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowDistanceSensitivityMouseWheel = 0.05f;
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="FollowDistance"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowDistance += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowDistanceSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	/// <summary>
	/// The default amount <see cref="FollowDistance"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowDistanceViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowDistanceSensitivityControllerStick = 0.5f;
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		FollowDistance += (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="FollowDistance"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowDistanceViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowDistanceSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerIncreasesDistance">If <see langword="true"/> (the default), the left trigger increases distance and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustFollowDistance(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="FollowDistance"/> is adjusted by per second whilst the chosen key or button is held down: <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowDistanceViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowDistanceSensitivityKeyOrButtonPress = 1f;
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowDistanceSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="FollowDistance"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowDistanceViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowDistanceSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by per second, in units.</param>
	public void AdjustFollowHeight(float deltaTime, float adjustmentPerSec) => FollowHeight += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="FollowHeight"/> is adjusted by for each pixel the mouse cursor moves: <c>0.0004f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowHeightViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowHeightSensitivityMouseCursor = 0.0004f;
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="FollowHeight"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityMouseCursor"/> (<c>0.0004f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		FollowHeight += delta * (adjustmentPerPixel ?? DefaultFollowHeightSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="FollowHeight"/> is adjusted by for each notch the mouse wheel is turned: <c>0.05f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowHeightViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowHeightSensitivityMouseWheel = 0.05f;
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="FollowHeight"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowHeight += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowHeightSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	/// <summary>
	/// The default amount <see cref="FollowHeight"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowHeightViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowHeightSensitivityControllerStick = 0.5f;
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowHeight += (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="FollowHeight"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowHeightViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowHeightSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="rightTriggerRaisesHeight">If <see langword="true"/> (the default), the right trigger raises height and the left trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool rightTriggerRaisesHeight = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = rightTriggerRaisesHeight ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var decreasingTriggerPosition = rightTriggerRaisesHeight ? input.LeftTriggerPosition : input.RightTriggerPosition;
		AdjustFollowHeight(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="FollowHeight"/> is adjusted by per second whilst the chosen key or button is held down: <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowHeightViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowHeightSensitivityKeyOrButtonPress = 1f;
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowHeightSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="FollowHeight"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowHeightViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowHeightSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by per second, in units.</param>
	public void AdjustFollowLateralOffset(float deltaTime, float adjustmentPerSec) => FollowLateralOffset += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="FollowLateralOffset"/> is adjusted by for each pixel the mouse cursor moves: <c>DefaultFollowHeightSensitivityMouseCursor</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowLateralOffsetViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowLateralOffsetSensitivityMouseCursor = DefaultFollowHeightSensitivityMouseCursor;
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="FollowLateralOffset"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityMouseCursor"/> (<c>DefaultFollowHeightSensitivityMouseCursor</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => -input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowLateralOffset += delta * (adjustmentPerPixel ?? DefaultFollowLateralOffsetSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="FollowLateralOffset"/> is adjusted by for each notch the mouse wheel is turned: <c>DefaultFollowHeightSensitivityMouseWheel</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowLateralOffsetViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowLateralOffsetSensitivityMouseWheel = DefaultFollowHeightSensitivityMouseWheel;
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="FollowLateralOffset"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityMouseWheel"/> (<c>DefaultFollowHeightSensitivityMouseWheel</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowLateralOffset += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowLateralOffsetSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	/// <summary>
	/// The default amount <see cref="FollowLateralOffset"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>DefaultFollowHeightSensitivityControllerStick</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowLateralOffsetViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowLateralOffsetSensitivityControllerStick = DefaultFollowHeightSensitivityControllerStick;
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityControllerStick"/> (<c>DefaultFollowHeightSensitivityControllerStick</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		FollowLateralOffset += (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="FollowLateralOffset"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>DefaultFollowHeightSensitivityControllerTrigger</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowLateralOffsetViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowLateralOffsetSensitivityControllerTrigger = DefaultFollowHeightSensitivityControllerTrigger;
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityControllerTrigger"/> (<c>DefaultFollowHeightSensitivityControllerTrigger</c>) is used.</param>
	/// <param name="leftTriggerOffsetsLeft">If <see langword="true"/> (the default), the left trigger offsets left and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerOffsetsLeft = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerOffsetsLeft ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerOffsetsLeft ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustFollowLateralOffset(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="FollowLateralOffset"/> is adjusted by per second whilst the chosen key or button is held down: <c>DefaultFollowHeightSensitivityKeyOrButtonPress</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustFollowLateralOffsetViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultFollowLateralOffsetSensitivityKeyOrButtonPress = DefaultFollowHeightSensitivityKeyOrButtonPress;
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityKeyOrButtonPress"/> (<c>DefaultFollowHeightSensitivityKeyOrButtonPress</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowLateralOffset(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="FollowLateralOffset"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityKeyOrButtonPress"/> (<c>DefaultFollowHeightSensitivityKeyOrButtonPress</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustFollowLateralOffsetViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowLateralOffset(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts this controller's framing properties according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Vertical mouse movement raises and lowers the camera, horizontal mouse movement shifts it sideways, and the scroll wheel moves it closer to or further from
	/// the target. Note that this adjusts only the framing; you must still set <see cref="Target"/> yourself each frame. Call the individual <c>Adjust</c> methods
	/// if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, reverses which way the scroll wheel moves the camera.</param>
	/// <param name="invertHeightControl">If <see langword="true"/>, reverses which way vertical mouse movement moves the camera.</param>
	/// <param name="invertLateralControl">If <see langword="true"/>, reverses which way horizontal mouse movement moves the camera.</param>
	/// <param name="distanceAdjustmentPerWheelIncrement">How far to adjust <see cref="FollowDistance"/> by per wheel notch. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityMouseWheel"/> is used.</param>
	/// <param name="heightAdjustmentPerPixel">How far to adjust <see cref="FollowHeight"/> by per pixel of cursor movement. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityMouseCursor"/> is used.</param>
	/// <param name="lateralAdjustmentPerPixel">How far to adjust <see cref="FollowLateralOffset"/> by per pixel of cursor movement. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityMouseCursor"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? distanceAdjustmentPerWheelIncrement = null, float? heightAdjustmentPerPixel = null, float? lateralAdjustmentPerPixel = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustFollowHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
		AdjustFollowDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
		AdjustFollowLateralOffsetViaMouseCursor(input, lateralAdjustmentPerPixel, invertMouseControl: invertLateralControl);
	}

	/// <summary>
	/// Adjusts this controller's framing properties according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The triggers move the camera closer to and further from the target, whilst the right stick raises and lowers it and shifts it sideways. Note that this
	/// adjusts only the framing; you must still set <see cref="Target"/> yourself each frame. Call the individual <c>Adjust</c> methods if you want a different
	/// mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, swaps which trigger moves the camera towards the target and which moves it away.</param>
	/// <param name="invertHeightControl">If <see langword="true"/>, reverses which way the stick raises and lowers the camera.</param>
	/// <param name="invertLateralControl">If <see langword="true"/>, reverses which way the stick shifts the camera sideways.</param>
	/// <param name="maxDistanceAdjustmentPerSec">How far to adjust <see cref="FollowDistance"/> by per second at full trigger depression. If <see langword="null"/>, <see cref="DefaultFollowDistanceSensitivityControllerTrigger"/> is used.</param>
	/// <param name="maxHeightAdjustmentPerSec">How far to adjust <see cref="FollowHeight"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultFollowHeightSensitivityControllerStick"/> is used.</param>
	/// <param name="maxLateralAdjustmentPerSec">How far to adjust <see cref="FollowLateralOffset"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultFollowLateralOffsetSensitivityControllerStick"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? maxDistanceAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxLateralAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustFollowDistanceViaControllerTriggers(input, deltaTime, maxDistanceAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
		AdjustFollowHeightViaControllerStick(input, deltaTime, maxHeightAdjustmentPerSec, invertStickControl: invertHeightControl);
		AdjustFollowLateralOffsetViaControllerStick(input, deltaTime, maxLateralAdjustmentPerSec, invertStickControl: invertLateralControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
