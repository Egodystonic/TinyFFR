// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Controls a camera that flies freely through space in any direction, unconstrained by a ground plane.
/// </summary>
/// <remarks>
/// <para>
/// The camera is placed with <see cref="Position"/> and aimed with <see cref="Yaw"/> (turning left and right) and <see cref="Pitch"/> (tilting up and down). Unlike
/// <see cref="FirstPersonCameraController"/>, movement is not confined to a plane: the camera can be moved in any direction, including along whichever way it
/// happens to be looking, which makes this the natural choice for spectator, debug and fly-through cameras.
/// </para>
/// <para>
/// By default <see cref="Pitch"/> is clamped so the camera cannot tip past vertical; set <see cref="AllowUpsideDownFlip"/> to lift that restriction.
/// </para>
/// </remarks>
public sealed class FreeFlyingCameraController : ICameraController<FreeFlyingCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<FreeFlyingCameraController> _controllerPool = new(&New);
	static FreeFlyingCameraController New() => new();
	static FreeFlyingCameraController ICameraController<FreeFlyingCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FreeFlyingCameraController));
	FreeFlyingCameraController() { }
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
	/// <summary>
	/// The default value for <see cref="WorldUp"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction WorldUpDefault = Direction.Up;
	/// <summary>
	/// The default value for <see cref="WorldForward"/>: <see cref="Direction.Forward"/>.
	/// </summary>
	public static readonly Direction WorldForwardDefault = Direction.Forward;
	/// <summary>
	/// The default value for <see cref="AllowUpsideDownFlip"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool AllowUpsideDownFlipDefault = false;

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

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Position"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomPositionSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's rotation towards <see cref="Yaw"/> and <see cref="Pitch"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomRotationSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength RotationSmoothingStrength {
		get => _rotationSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set {
			_yawSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
			_pitchSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
		}
	}
	
	/// <summary>
	/// The direction the camera faces when <see cref="Yaw"/> and <see cref="Pitch"/> are both <c>0°</c>. Defaults to <see cref="WorldForwardDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this also re-derives <see cref="WorldUp"/>, since the two are always kept at right angles to one another. Values that are not physically valid, and
	/// <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction WorldForward {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			WorldUp = WorldUp;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// Which way is "up" for this camera; the axis <see cref="Yaw"/> turns it around. Defaults to <see cref="WorldUpDefault"/>.
	/// </summary>
	/// <remarks>
	/// This is always kept at right angles to <see cref="WorldForward"/>: the value you supply is straightened against it rather than used verbatim, so reading this
	/// back may not return exactly what you set. If you supply a direction parallel to <see cref="WorldForward"/> (leaving no valid "up" to derive), an arbitrary
	/// perpendicular direction is chosen instead.
	/// </remarks>
	public Direction WorldUp {
		get;
		set {
			field = value.OrthogonalizedAgainst(WorldForward) ?? Direction.None;
			if (!field.IsPhysicallyValidAndNotNone) field = WorldForward.AnyOrthogonal();
		}
	}
	/// <summary>
	/// Whether the camera is allowed to tilt past vertical and end up upside-down. Defaults to <see cref="AllowUpsideDownFlipDefault"/>.
	/// </summary>
	/// <remarks>
	/// When <see langword="false"/>, <see cref="Pitch"/> is clamped to the range <c>-90° &lt;= n &lt;= 90°</c> during <see cref="Progress(float)"/>, which is what most
	/// applications want. When <see langword="true"/> the camera may rotate freely through the vertical, as an aircraft or spacecraft would.
	/// </remarks>
	public bool AllowUpsideDownFlip { get; set; }
	
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
	/// At <c>0°</c> the camera looks along <see cref="WorldForward"/>. <b>Increasing this tilts the camera downward and decreasing it tilts upward</b>, so
	/// <c>45°</c> looks down and <c>-45°</c> looks up. Unless <see cref="AllowUpsideDownFlip"/> is <see langword="true"/>, the value is clamped to
	/// <c>-90° &lt;= n &lt;= 90°</c> during <see cref="Progress(float)"/>. This is a target rather than the camera's current tilt: the camera eases towards it according to
	/// <see cref="RotationSmoothingStrength"/>. Values that are not physically valid are ignored rather than throwing.
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
	/// A convenience for applications that compute the camera's placement themselves each frame. Exactly equivalent to assigning the three properties and then
	/// calling <see cref="Progress(float)"/>.
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
	/// Sets the fixed constraints this controller works within, all in one call.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning the three properties individually. Note that <paramref name="worldUp"/> is straightened against <paramref name="worldForward"/>, as
	/// described on <see cref="WorldUp"/>.
	/// </remarks>
	/// <param name="worldForward">The value to assign to <see cref="WorldForward"/>.</param>
	/// <param name="worldUp">The value to assign to <see cref="WorldUp"/>.</param>
	/// <param name="allowUpsideDownFlip">The value to assign to <see cref="AllowUpsideDownFlip"/>.</param>
	public void SetConstraints(Direction worldForward, Direction worldUp, bool allowUpsideDownFlip) {
		WorldForward = worldForward;
		WorldUp = worldUp;
		AllowUpsideDownFlip = allowUpsideDownFlip;
	}
	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets both smoothing strengths to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		WorldForward = WorldForwardDefault;
		WorldUp = WorldUpDefault;
		AllowUpsideDownFlip = AllowUpsideDownFlipDefault;
		_positionSetpoint.Reset(PositionDefault.AsVect());
		_yawSetpoint.Reset(YawDefault);
		_pitchSetpoint.Reset(PitchDefault);
		SetGlobalSmoothing(SmoothingStrength.VeryMild);
	}

	/// <inheritdoc />
	public void Progress(float deltaTime) {
		if (!AllowUpsideDownFlip) {
			var curTarget = _pitchSetpoint.TargetValue;
			var diffToLowerBound = curTarget - Angle.QuarterCircle;
			var diffToUpperBound = (Angle.FullCircle - Angle.QuarterCircle) - curTarget;
			if (diffToLowerBound > 0f && diffToUpperBound > 0f) {
				if (diffToUpperBound < diffToLowerBound) _pitchSetpoint.TargetValue = Angle.FullCircle - Angle.QuarterCircle; 
				else _pitchSetpoint.TargetValue = Angle.QuarterCircle; 
			} 
		}
		
		_positionSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		
		var currentHorizontalPlaneDir = WorldForward * (_yawSetpoint.CurrentValue % WorldUp);
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
	/// The default amount <see cref="Pitch"/> is adjusted by per second whilst the chosen key or button is held down: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPitchViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPitchSensitivityKeyOrButtonPress = 120f;
	/// <summary>
	/// Adjusts <see cref="Pitch"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pitch"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPitchSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
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
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pitch"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPitchSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
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
	/// Moves the camera a given distance in a direction expressed relative to the way it is currently facing.
	/// </summary>
	/// <param name="cameraRelativeOrientation">Which direction to move in, relative to the camera's own orientation (so <see cref="Orientation.Forward"/> moves the camera the way it is looking).</param>
	/// <param name="distance">How far to move, in metres.</param>
	public void AdjustPosition(Orientation cameraRelativeOrientation, float distance) => Position += Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * distance;
	/// <summary>
	/// Moves the camera at a given speed, in a direction expressed relative to the way it is currently facing, for the duration of a single frame.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="cameraRelativeOrientation">Which direction to move in, relative to the camera's own orientation (so <see cref="Orientation.Forward"/> moves the camera the way it is looking).</param>
	/// <param name="speed">How fast to move, in metres per second.</param>
	public void AdjustPosition(float deltaTime, Orientation cameraRelativeOrientation, float speed) => AdjustPosition(deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * speed);
	/// <summary>
	/// Moves the camera at a given velocity, expressed in world space, for the duration of a single frame.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to move the camera per second, along each world axis.</param>
	public void AdjustPosition(float deltaTime, Vect adjustmentPerSec) => Position += adjustmentPerSec * deltaTime;

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
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseCursor"/> (<c>0.0004f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Orientation cameraRelativeOrientation, float? speed = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaMouseCursor(input, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityMouseCursor), invertMouseControl, axis);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="Position"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseCursor"/> (<c>0.0004f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Vect adjustmentPerPixel, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Position += delta * adjustmentPerPixel;
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
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Orientation cameraRelativeOrientation, float? speed = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaMouseWheel(input, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityMouseWheel), invertMouseControl);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="Position"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultPositionSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Vect adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Position += input.MouseScrollWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f : 1f);
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
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="maxSpeed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the left stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Orientation cameraRelativeOrientation, float? maxSpeed = null, bool useLeftStick = true, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaControllerStick(input, deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (maxSpeed ?? DefaultPositionSensitivityControllerStick), useLeftStick, invertStickControl, axis);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Position"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the left stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Vect maxAdjustmentPerSec, bool useLeftStick = true, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Position += maxAdjustmentPerSec * delta;
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
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="maxSpeed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerMovesPositive">If <see langword="true"/> (the default), the left trigger moves positive and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Orientation cameraRelativeOrientation, float? maxSpeed = null, bool leftTriggerMovesPositive = true) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaControllerTriggers(input, deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (maxSpeed ?? DefaultPositionSensitivityControllerTrigger), leftTriggerMovesPositive);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Position"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPositionSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerMovesPositive">If <see langword="true"/> (the default), the left trigger moves positive and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Vect maxAdjustmentPerSec, bool leftTriggerMovesPositive = true) {
		ArgumentNullException.ThrowIfNull(input);
		var positiveTriggerPosition = leftTriggerMovesPositive ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var negativeTriggerPosition = leftTriggerMovesPositive ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPosition(deltaTime, positiveTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - negativeTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
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
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Orientation cameraRelativeOrientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaKeyPress(input, deltaTime, keyToTestFor, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Position"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Vect adjustmentPerSec) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPosition(deltaTime, adjustmentPerSec);
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="cameraRelativeOrientation">Which direction to move the camera in, expressed relative to the direction it is currently facing (so <see cref="Orientation.Forward"/> moves the camera the way it is looking, and <see cref="Orientation.Up"/> moves it towards its own up direction).</param>
	/// <param name="speed">How far to adjust <see cref="Position"/> by per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Orientation cameraRelativeOrientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaButtonPress(input, deltaTime, buttonToTestFor, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Position"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Position"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Vect adjustmentPerSec) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPosition(deltaTime, adjustmentPerSec);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Moving the mouse turns the camera (horizontally for <see cref="Yaw"/>, vertically for <see cref="Pitch"/>); the four arrow keys fly it forwards, backwards
	/// and sideways; and right shift and right control fly it up and down. Call the individual <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, moving the mouse up tilts the camera down instead of up.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, moving the mouse left turns the camera right instead of left.</param>
	/// <param name="invertUpDownPositionalControl">If <see langword="true"/>, swaps the two keys that fly the camera up and down.</param>
	/// <param name="pitchAdjustmentPerPixel">How much to adjust <see cref="Pitch"/> by for each pixel of vertical cursor movement. If <see langword="null"/>, <see cref="DefaultPitchSensitivityMouseCursor"/> is used.</param>
	/// <param name="yawAdjustmentPerPixel">How much to adjust <see cref="Yaw"/> by for each pixel of horizontal cursor movement. If <see langword="null"/>, <see cref="DefaultYawSensitivityMouseCursor"/> is used.</param>
	/// <param name="positionAdjustmentSpeed">How fast the movement keys fly the camera, in metres per second. If <see langword="null"/>, <see cref="DefaultPositionSensitivityKeyOrButtonPress"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertUpDownPositionalControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? positionAdjustmentSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);

		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation.Left, positionAdjustmentSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation.Right, positionAdjustmentSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation.Forward, positionAdjustmentSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation.Backward, positionAdjustmentSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.RightShift, invertUpDownPositionalControl ? Orientation.Down : Orientation.Up, positionAdjustmentSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.RightControl, invertUpDownPositionalControl ? Orientation.Up : Orientation.Down, positionAdjustmentSpeed);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The right stick turns the camera (horizontally for <see cref="Yaw"/>, vertically for <see cref="Pitch"/>); the left stick flies it forwards, backwards and
	/// sideways; and the two triggers fly it up and down. Call the individual <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, pushing the stick up tilts the camera down instead of up.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, pushing the stick left turns the camera right instead of left.</param>
	/// <param name="invertUpDownPositionalControl">If <see langword="true"/>, swaps which trigger flies the camera up and which flies it down.</param>
	/// <param name="maxPitchAdjustmentPerSec">How much to adjust <see cref="Pitch"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPitchSensitivityControllerStick"/> is used.</param>
	/// <param name="maxYawAdjustmentPerSec">How much to adjust <see cref="Yaw"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultYawSensitivityControllerStick"/> is used.</param>
	/// <param name="maxPositionAdjustmentSpeed">How fast the sticks and triggers fly the camera when fully displaced, in metres per second. If <see langword="null"/>, the relevant <c>DefaultPositionSensitivity</c> constant is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertUpDownPositionalControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxPositionAdjustmentSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);

		AdjustPositionViaControllerStick(input, deltaTime, Orientation.Forward, maxPositionAdjustmentSpeed, axis: Axis2D.Y);
		AdjustPositionViaControllerStick(input, deltaTime, Orientation.Right, maxPositionAdjustmentSpeed, axis: Axis2D.X);
		AdjustPositionViaControllerTriggers(input, deltaTime, invertUpDownPositionalControl ? Orientation.Up : Orientation.Down, maxPositionAdjustmentSpeed);
	}

	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
