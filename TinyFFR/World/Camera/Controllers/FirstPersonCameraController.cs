// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Controls a camera as though it were the eyes of a person walking around on a ground plane.
/// </summary>
/// <remarks>
/// <para>
/// The camera is placed with <see cref="Position"/> and aimed with <see cref="Yaw"/> (turning left and right) and <see cref="Pitch"/> (looking up and down).
/// Movement is confined to the plane at right angles to <see cref="WorldUp"/>, so walking never lifts the camera off the ground however far it is tilted. That is
/// what distinguishes this from <see cref="FreeFlyingCameraController"/>, where movement follows wherever the camera is looking.
/// </para>
/// <para>
/// <see cref="Pitch"/> is clamped each frame so the camera can never tip past vertical and end up upside-down.
/// </para>
/// </remarks>
public sealed class FirstPersonCameraController : ICameraController<FirstPersonCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<FirstPersonCameraController> _controllerPool = new(&New);
	static FirstPersonCameraController New() => new();
	static FirstPersonCameraController ICameraController<FirstPersonCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FirstPersonCameraController));
	FirstPersonCameraController() { }
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
	/// The default value for <see cref="WorldUp"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction WorldUpDefault = Direction.Up;
	/// <summary>
	/// The default value for <see cref="Position"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location PositionDefault = Location.Origin;
	/// <summary>
	/// The default value for <see cref="Yaw"/>: <c>0°</c>.
	/// </summary>
	public static readonly Angle YawDefault = Angle.Zero;
	/// <summary>
	/// The default value for <see cref="Pitch"/>: <c>0°</c>.
	/// </summary>
	public static readonly Angle PitchDefault = Angle.Zero;

	readonly Spring3DBasedCameraSetpoint _positionSetpoint = new();
	readonly CameraEffectStrengthMap _positionSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.1f,
		Standard: 0.2f,
		Strong: 0.3f,
		VeryStrong: 0.4f
	);
	readonly SpringAngleBasedCameraSetpoint _yawSetpoint = new();
	readonly SpringAngleBasedCameraSetpoint _pitchSetpoint = new();
	readonly CameraEffectStrengthMap _rotationSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.03f,
		Mild: 0.06f,
		Standard: 0.1f,
		Strong: 0.14f,
		VeryStrong: 0.2f
	);
	Direction _forwardDir;

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Position"/>.
	/// </summary>
	/// <remarks>
	/// The values here correspond to half-lives of <c>0s</c>, <c>0.05s</c>, <c>0.1s</c>, <c>0.2s</c>, <c>0.3s</c> and <c>0.4s</c> respectively; use
	/// <see cref="SetCustomPositionSmoothingStrength"/> to specify one directly.
	/// </remarks>
	public SmoothingStrength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's rotation towards <see cref="Yaw"/> and <see cref="Pitch"/>.
	/// </summary>
	/// <remarks>
	/// The values here correspond to half-lives of <c>0s</c>, <c>0.03s</c>, <c>0.06s</c>, <c>0.1s</c>, <c>0.14s</c> and <c>0.2s</c> respectively; use
	/// <see cref="SetCustomRotationSmoothingStrength"/> to specify one directly. Rotation is smoothed less than position by default, because lag between moving the
	/// mouse and the view turning is more noticeable than lag in movement.
	/// </remarks>
	public SmoothingStrength RotationSmoothingStrength {
		get => _rotationSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set {
			_yawSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
			_pitchSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
		}
	}
	
	/// <summary>
	/// Which way is "up" for this camera. Defaults to <see cref="WorldUpDefault"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="Yaw"/> turns the camera around this direction and <see cref="Pitch"/> tilts it away from it, whilst movement is confined to the plane at right
	/// angles to it. Note that changing this also re-derives the direction the camera faces at a <see cref="Yaw"/> of <c>0°</c>, so the camera will generally swing
	/// round to a new heading when you do; set it once during setup rather than every frame. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Direction WorldUp {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
			_forwardDir = value.AnyOrthogonal();
		}
	}

	/// <summary>
	/// Where the camera should be in the world. Defaults to <see cref="PositionDefault"/>.
	/// </summary>
	/// <remarks>
	/// This is a target rather than the camera's current location: the camera eases towards it over the following frames according to
	/// <see cref="PositionSmoothingStrength"/>. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Location Position {
		get => _positionSetpoint.TargetValue.AsLocation();
		set {
			if (!value.IsPhysicallyValid) return;
			_positionSetpoint.TargetValue = value.AsVect();
		}
	}
	/// <summary>
	/// How far the camera should be turned to the left, around <see cref="WorldUp"/>. Defaults to <see cref="YawDefault"/>.
	/// </summary>
	/// <remarks>
	/// Increasing this turns the camera to the left; decreasing it turns to the right. This is a target rather than the camera's current heading: the camera eases
	/// towards it according to <see cref="RotationSmoothingStrength"/>. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Angle Yaw {
		get => _yawSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_yawSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// How far the camera should be tilted downward, away from <see cref="WorldUp"/>. Defaults to <see cref="PitchDefault"/>.
	/// </summary>
	/// <remarks>
	/// At <c>0°</c> the camera looks horizontally. <b>Increasing this tilts the camera downward and decreasing it tilts upward</b>, so <c>45°</c> looks down at the
	/// ground and <c>-45°</c> looks up at the sky. The value is clamped to the range <c>-90° &lt;= n &lt;= 90°</c> during <see cref="Progress(float)"/>, so the camera can
	/// never tip past straight up or straight down and end up upside-down. This is a target rather than the camera's current tilt: the camera eases towards it
	/// according to <see cref="RotationSmoothingStrength"/>. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Angle Pitch {
		get => _pitchSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_pitchSetpoint.TargetValue = value;
		}
	}

	/// <summary>
	/// Sets the position smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Position"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the rotation smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining angle to <see cref="Yaw"/> and <see cref="Pitch"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomRotationSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <inheritdoc />
	public void SetGlobalSmoothing(SmoothingStrength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		RotationSmoothingStrength = newSmoothingStrength;
	}

	/// <summary>
	/// Sets <see cref="Position"/>, <see cref="Yaw"/> and <see cref="Pitch"/> and then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// A convenience for applications that compute the camera's placement themselves each frame rather than nudging it with the <c>Adjust</c> methods. Exactly
	/// equivalent to assigning the three properties and then calling <see cref="Progress(float)"/>.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="position">The value to assign to <see cref="Position"/>.</param>
	/// <param name="yaw">The value to assign to <see cref="Yaw"/>.</param>
	/// <param name="pitch">The value to assign to <see cref="Pitch"/>.</param>
	public void Progress(float deltaTime, Location position, Angle yaw, Angle pitch) {
		Position = position;
		Yaw = yaw;
		Pitch = pitch;
		Progress(deltaTime);
	}
	/// <summary>
	/// Sets the fixed constraints this controller works within.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning <see cref="WorldUp"/> directly; offered so that every controller exposes its constraints through a single call.
	/// </remarks>
	/// <param name="worldUp">The value to assign to <see cref="WorldUp"/>.</param>
	public void SetConstraints(Direction worldUp) {
		WorldUp = worldUp;
	}
	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets both smoothing strengths to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		WorldUp = WorldUpDefault;
		_positionSetpoint.Reset(PositionDefault.AsVect());
		_yawSetpoint.Reset(YawDefault);
		_pitchSetpoint.Reset(PitchDefault);
		SetGlobalSmoothing(SmoothingStrength.VeryMild);
	}

	/// <inheritdoc />
	public void Progress(float deltaTime) {
		var curTarget = _pitchSetpoint.TargetValue;
		var diffToLowerBound = curTarget - Angle.QuarterCircle;
		var diffToUpperBound = (Angle.FullCircle - Angle.QuarterCircle) - curTarget;
		if (diffToLowerBound > 0f && diffToUpperBound > 0f) {
			if (diffToUpperBound < diffToLowerBound) _pitchSetpoint.TargetValue = Angle.FullCircle - Angle.QuarterCircle; 
			else _pitchSetpoint.TargetValue = Angle.QuarterCircle; 
		}
		
		_positionSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		
		var currentHorizontalPlaneDir = _forwardDir * (_yawSetpoint.CurrentValue % WorldUp);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(WorldUp, currentHorizontalPlaneDir);
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(currentHorizontalPlaneDir * verticalTiltRot, WorldUp * verticalTiltRot);
	}

	/// <summary>
	/// Adjusts <see cref="Pitch"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Pitch"/> by per second, in degrees.</param>
	public void AdjustPitch(float deltaTime, Angle adjustmentPerSec) => Pitch += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Pitch"/> is adjusted by for each pixel the mouse cursor moves: <c>0.04f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityMouseCursor = 0.04f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How much to adjust <see cref="Pitch"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultPitchSensitivityMouseCursor"/> (<c>0.04f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Pitch += delta * (adjustmentPerPixel ?? DefaultPitchSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="Pitch"/> is adjusted by for each notch the mouse wheel is turned: <c>5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityMouseWheel = 5f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How much to adjust <see cref="Pitch"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultPitchSensitivityMouseWheel"/> (<c>5f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Pitch += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPitchSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	/// <summary>
	/// The default amount <see cref="Pitch"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityControllerStick = 120f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Pitch"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPitchSensitivityControllerStick"/> (<c>120f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Pitch += (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="Pitch"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityControllerTrigger = 120f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Pitch"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPitchSensitivityControllerTrigger"/> (<c>120f</c>) is used.</param>
	/// <param name="rightTriggerPitchesUp">If <see langword="true"/> (the default), the right trigger pitches up and the left trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool rightTriggerPitchesUp = true) {
		ArgumentNullException.ThrowIfNull(input);
		var pitchDownTriggerPosition = rightTriggerPitchesUp ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var pitchUpTriggerPosition = rightTriggerPitchesUp ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPitch(deltaTime, pitchDownTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger)
			- pitchUpTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Pitch"/> is adjusted by per second whilst the chosen key or button is held down: <c>80f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityKeyOrButtonPress = 80f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pitch"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPitchSensitivityKeyOrButtonPress"/> (<c>80f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPitchSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Pitch"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pitch"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPitchSensitivityKeyOrButtonPress"/> (<c>80f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPitch(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPitchSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="Yaw"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Yaw"/> by per second, in degrees.</param>
	public void AdjustYaw(float deltaTime, Angle adjustmentPerSec) => Yaw += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Yaw"/> is adjusted by for each pixel the mouse cursor moves: <c>0.04f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustYawViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultYawSensitivityMouseCursor = 0.04f;
	/// <summary>
	/// Adjusts <see cref="Yaw"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How much to adjust <see cref="Yaw"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultYawSensitivityMouseCursor"/> (<c>0.04f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Yaw += delta * (adjustmentPerPixel ?? DefaultYawSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="Yaw"/> is adjusted by for each notch the mouse wheel is turned: <c>5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustYawViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultYawSensitivityMouseWheel = 5f;
	/// <summary>
	/// Adjusts <see cref="Yaw"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How much to adjust <see cref="Yaw"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultYawSensitivityMouseWheel"/> (<c>5f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Yaw += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultYawSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	/// <summary>
	/// The default amount <see cref="Yaw"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustYawViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultYawSensitivityControllerStick = 120f;
	/// <summary>
	/// Adjusts <see cref="Yaw"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Yaw"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultYawSensitivityControllerStick"/> (<c>120f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Yaw += (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="Yaw"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustYawViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultYawSensitivityControllerTrigger = 120f;
	/// <summary>
	/// Adjusts <see cref="Yaw"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Yaw"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultYawSensitivityControllerTrigger"/> (<c>120f</c>) is used.</param>
	/// <param name="leftTriggerYawsLeft">If <see langword="true"/> (the default), the left trigger yaws left and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerYawsLeft = true) {
		ArgumentNullException.ThrowIfNull(input);
		var yawLeftTriggerPosition = leftTriggerYawsLeft ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var yawRightTriggerPosition = leftTriggerYawsLeft ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustYaw(deltaTime, yawLeftTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger)
			- yawRightTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Yaw"/> is adjusted by per second whilst the chosen key or button is held down: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustYawViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultYawSensitivityKeyOrButtonPress = 120f;
	/// <summary>
	/// Adjusts <see cref="Yaw"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Yaw"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultYawSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustYaw(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultYawSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Yaw"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Yaw"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultYawSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultYawSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Moves the camera a given distance in a given direction across the ground plane.
	/// </summary>
	/// <remarks>
	/// The direction is given as a compass-style angle measured relative to the way the camera is currently facing, so the camera walks where it is looking rather
	/// than in a fixed world direction. <c>0°</c> is the camera's right, <c>90°</c> is straight ahead, <c>180°</c> is its left and <c>270°</c> is behind it. The
	/// movement is always parallel to the plane at right angles to <see cref="WorldUp"/>, so looking up or down does not change where a step takes you.
	/// </remarks>
	/// <param name="polarOrientation">The direction to move in, measured anticlockwise from the camera's right.</param>
	/// <param name="distance">How far to move, in metres.</param>
	public void AdjustPosition(Angle polarOrientation, float distance) {
		var zeroDegreeDir = Camera.GetRelativeOrientationDirection(Orientation.Right).OrthogonalizedAgainst(WorldUp)
			?? Direction.FromDualOrthogonalization(Camera.ViewDirection, WorldUp);

		Position += (zeroDegreeDir * (polarOrientation % WorldUp)) * distance;
	}
	/// <summary>
	/// Moves the camera across the ground plane at a given speed for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// As <see cref="AdjustPosition(Angle, float)"/>, except the distance moved is <paramref name="moveSpeed"/> multiplied by <paramref name="deltaTime"/>, so the
	/// camera travels at the same speed regardless of frame rate.
	/// </remarks>
	/// <param name="polarOrientation">The direction to move in, measured anticlockwise from the camera's right.</param>
	/// <param name="moveSpeed">How fast to move, in metres per second.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	public void AdjustPosition(Angle polarOrientation, float moveSpeed, float deltaTime) {
		AdjustPosition(polarOrientation, moveSpeed * deltaTime);
	}
	/// <summary>
	/// Moves the camera a given distance in one of the four cardinal directions across the ground plane.
	/// </summary>
	/// <remarks>
	/// A convenience over <see cref="AdjustPosition(Angle, float)"/> for the common case of walking forwards, backwards or sideways.
	/// </remarks>
	/// <param name="orientation">Which direction to move the camera in, relative to the way it is currently facing: <see cref="Orientation2D.Up"/> moves forwards, <see cref="Orientation2D.Down"/> backwards, and <see cref="Orientation2D.Left"/> and <see cref="Orientation2D.Right"/> strafe sideways. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <param name="distance">How far to move, in metres.</param>
	public void AdjustPosition(Orientation2D orientation, float distance) {
		AdjustPosition(Angle.From2DPolarAngle(orientation) ?? Angle.Zero, distance);
	}
	/// <summary>
	/// Moves the camera in one of the four cardinal directions across the ground plane, at a given speed, for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// As <see cref="AdjustPosition(Orientation2D, float)"/>, except the distance moved is <paramref name="moveSpeed"/> multiplied by <paramref name="deltaTime"/>,
	/// so the camera travels at the same speed regardless of frame rate.
	/// </remarks>
	/// <param name="orientation">Which direction to move the camera in, relative to the way it is currently facing: <see cref="Orientation2D.Up"/> moves forwards, <see cref="Orientation2D.Down"/> backwards, and <see cref="Orientation2D.Left"/> and <see cref="Orientation2D.Right"/> strafe sideways. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <param name="moveSpeed">How fast to move, in metres per second.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	public void AdjustPosition(Orientation2D orientation, float moveSpeed, float deltaTime) {
		AdjustPosition(orientation, moveSpeed * deltaTime);
	}

	/// <summary>
	/// The default amount <see cref="Position"/> is adjusted by for each pixel the mouse cursor moves: <c>0.0004f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPositionViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPositionSensitivityMouseCursor = 0.0004f;
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="distancePerPixel">How far to adjust <see cref="Position"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseCursor"/> (<c>0.0004f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? distancePerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		AdjustPosition(axis == Axis2D.X ? Orientation2D.Right : Orientation2D.Up, (distancePerPixel ?? DefaultPositionSensitivityMouseCursor) * delta);
	}

	/// <summary>
	/// The default amount <see cref="Position"/> is adjusted by for each notch the mouse wheel is turned: <c>0.05f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPositionViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPositionSensitivityMouseWheel = 0.05f;
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="distancePerWheelIncrement">How far to adjust <see cref="Position"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="positiveOrientation">Which direction a positive input moves the camera in, relative to the way it is currently facing; a negative input moves it the opposite way. <see cref="Orientation2D.Up"/> (the default) means forwards. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? distancePerWheelIncrement = null, Orientation2D positiveOrientation = Orientation2D.Up, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPosition(positiveOrientation, (distancePerWheelIncrement ?? DefaultPositionSensitivityMouseWheel) * input.MouseScrollWheelDelta * (invertMouseControl ? -1f : 1f));
	}

	/// <summary>
	/// The default amount <see cref="Position"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPositionViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPositionSensitivityControllerStick = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxSpeed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the left stick.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxSpeed = null, bool useLeftStick = true) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var angle = stickPosition.GetPolarAngle();
		if (angle == null) return;
		AdjustPosition(angle.Value, (maxSpeed ?? DefaultPositionSensitivityControllerStick) * stickPosition.Displacement, deltaTime);
	}

	/// <summary>
	/// The default amount <see cref="Position"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPositionViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPositionSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxSpeed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerMovesPositive">If <see langword="true"/> (the default), the left trigger moves positive and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <param name="positiveOrientation">Which direction a positive input moves the camera in, relative to the way it is currently facing; a negative input moves it the opposite way. <see cref="Orientation2D.Up"/> (the default) means forwards. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxSpeed = null, bool leftTriggerMovesPositive = true, Orientation2D positiveOrientation = Orientation2D.Up) {
		ArgumentNullException.ThrowIfNull(input);
		var positiveTriggerPosition = leftTriggerMovesPositive ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var negativeTriggerPosition = leftTriggerMovesPositive ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var sensitivity = maxSpeed ?? DefaultPositionSensitivityControllerTrigger;
		AdjustPosition(positiveOrientation, positiveTriggerPosition.GetDisplacementWithDeadzone() * sensitivity - negativeTriggerPosition.GetDisplacementWithDeadzone() * sensitivity, deltaTime);
	}

	/// <summary>
	/// The default amount <see cref="Position"/> is adjusted by per second whilst the chosen key or button is held down: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPositionViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPositionSensitivityKeyOrButtonPress = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Position"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="orientation">Which direction to move the camera in, relative to the way it is currently facing: <see cref="Orientation2D.Up"/> moves forwards, <see cref="Orientation2D.Down"/> backwards, and <see cref="Orientation2D.Left"/> and <see cref="Orientation2D.Right"/> strafe sideways. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Orientation2D orientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPosition(orientation, speed ?? DefaultPositionSensitivityKeyOrButtonPress, deltaTime);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="orientation">Which direction to move the camera in, relative to the way it is currently facing: <see cref="Orientation2D.Up"/> moves forwards, <see cref="Orientation2D.Down"/> backwards, and <see cref="Orientation2D.Left"/> and <see cref="Orientation2D.Right"/> strafe sideways. Note that <see cref="Orientation2D.None"/> is treated as <see cref="Orientation2D.Right"/> rather than producing no movement.</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Orientation2D orientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPosition(orientation, speed ?? DefaultPositionSensitivityKeyOrButtonPress, deltaTime);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Moving the mouse turns the camera (horizontally for <see cref="Yaw"/>, vertically for <see cref="Pitch"/>), and the four arrow keys walk the camera forwards,
	/// backwards and sideways. Call the individual <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, moving the mouse up tilts the camera down instead of up.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, moving the mouse left turns the camera right instead of left.</param>
	/// <param name="pitchAdjustmentPerPixel">How much to adjust <see cref="Pitch"/> by for each pixel of vertical cursor movement. If <see langword="null"/>, <see cref="DefaultPitchSensitivityMouseCursor"/> is used.</param>
	/// <param name="yawAdjustmentPerPixel">How much to adjust <see cref="Yaw"/> by for each pixel of horizontal cursor movement. If <see langword="null"/>, <see cref="DefaultYawSensitivityMouseCursor"/> is used.</param>
	/// <param name="moveSpeed">How fast the arrow keys move the camera, in metres per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? moveSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);

		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation2D.Left, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation2D.Right, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation2D.Up, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation2D.Down, moveSpeed);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The right stick turns the camera (horizontally for <see cref="Yaw"/>, vertically for <see cref="Pitch"/>) and the left stick walks it forwards, backwards and
	/// sideways. Call the individual <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, pushing the stick up tilts the camera down instead of up.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, pushing the stick left turns the camera right instead of left.</param>
	/// <param name="maxPitchAdjustmentPerSec">How much to adjust <see cref="Pitch"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPitchSensitivityControllerStick"/> is used.</param>
	/// <param name="maxYawAdjustmentPerSec">How much to adjust <see cref="Yaw"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultYawSensitivityControllerStick"/> is used.</param>
	/// <param name="maxMoveSpeed">How fast the left stick moves the camera when fully displaced, in metres per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerStick"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxMoveSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);

		AdjustPositionViaControllerStick(input, deltaTime, maxMoveSpeed);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
