// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Orbits a camera around a fixed target on the surface of an imaginary sphere, always pointing inward at it.
/// </summary>
/// <remarks>
/// <para>
/// This is the "model viewer" camera: <see cref="Yaw"/> and <see cref="Pitch"/> choose which direction the object is viewed from, and <see cref="Distance"/> chooses
/// how far away. Because the camera is always aimed at <see cref="Target"/>, the object stays centred no matter which angle it is seen from, which is what makes
/// this the natural choice for inspecting a single object.
/// </para>
/// <para>
/// It is closely related to <see cref="OrbitalCameraController"/>; the difference is that this positions the camera by two angles on a sphere, whereas that one
/// positions it by an angle around a circle plus a separate height.
/// </para>
/// </remarks>
public sealed class InspectorCameraController : ICameraController<InspectorCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<InspectorCameraController> _controllerPool = new(&New);
	static InspectorCameraController New() => new();
	static InspectorCameraController ICameraController<InspectorCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(InspectorCameraController));
	InspectorCameraController() { }
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
	/// The default value for <see cref="MinDistance"/>: <c>0.6f</c>.
	/// </summary>
	public static readonly float MinDistanceDefault = 0.6f;
	/// <summary>
	/// The default value for <see cref="MaxDistance"/>: <c>2f</c>.
	/// </summary>
	public static readonly float MaxDistanceDefault = 2f;
	/// <summary>
	/// The default value for <see cref="Distance"/>: the same as <see cref="MinDistanceDefault"/>.
	/// </summary>
	public static readonly float DistanceDefault = MinDistanceDefault;
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
	/// The default value for <see cref="Target"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location TargetDefault = Location.Origin;
	/// <summary>
	/// The default value for <see cref="AllowUpsideDownFlip"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool AllowUpsideDownFlipDefault = false;
	
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
	readonly Spring1DBasedCameraSetpoint _distanceSetpoint = new();
	readonly CameraEffectStrengthMap _distanceSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.15f,
		Mild: 0.25f,
		Standard: 0.4f,
		Strong: 0.65f,
		VeryStrong: 0.9f
	);
	Direction _worldForward;

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Distance"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomDistanceSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength DistanceSmoothingStrength {
		get => _distanceSmoothingStrengthMap.From(_distanceSetpoint.HalfLife);
		set => _distanceSetpoint.HalfLife = _distanceSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Yaw"/> and <see cref="Pitch"/>.
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
	/// The closest the camera may come to <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MinDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this above <see cref="MaxDistance"/> raises that property to match, and <see cref="Distance"/> is re-clamped immediately. Values that are not
	/// positive and finite are ignored rather than throwing.
	/// </remarks>
	public float? MinDistance {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value > MaxDistance) MaxDistance = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Distance = Distance;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The furthest the camera may get from <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MaxDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this below <see cref="MinDistance"/> lowers that property to match, and <see cref="Distance"/> is re-clamped immediately. Values that are not
	/// positive and finite are ignored rather than throwing.
	/// </remarks>
	public float? MaxDistance {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value < MinDistance) MinDistance = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Distance = Distance;
#pragma warning restore CA2245
		}
	}
	
	/// <summary>
	/// Which way is "up"; the axis the camera yaws around, and the reference for what tilting via <see cref="Pitch"/> means. Defaults to <see cref="WorldUpDefault"/>.
	/// </summary>
	/// <remarks>
	/// Changing this also re-derives the direction the camera views the target from at a <see cref="Yaw"/> of <c>0°</c>, so the camera will generally swing round to
	/// a new position when you do; set it once during setup rather than every frame. Values that are not physically valid, and <see cref="Direction.None"/>, are
	/// ignored rather than throwing.
	/// </remarks>
	public Direction WorldUp {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
			_worldForward = value.AnyOrthogonal();
		}
	}
	/// <summary>
	/// Whether the camera is allowed to pass over the top of the target and end up upside-down. Defaults to <see cref="AllowUpsideDownFlipDefault"/>.
	/// </summary>
	/// <remarks>
	/// When <see langword="false"/>, <see cref="Pitch"/> is clamped to the range <c>-90° &lt;= n &lt;= 90°</c> during <see cref="Progress(float)"/>, so the camera stops at
	/// directly above and directly below the target rather than tumbling over.
	/// </remarks>
	public bool AllowUpsideDownFlip { get; set; }
	
	/// <summary>
	/// How far the camera should be from <see cref="Target"/>, in metres. Defaults to <see cref="DistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Values are clamped to between <see cref="MinDistance"/> and <see cref="MaxDistance"/> as they are set, so reading this back may not return what you assigned.
	/// This is a target rather than the camera's current distance: the camera eases towards it according to <see cref="DistanceSmoothingStrength"/>. Negative and
	/// non-finite values are ignored rather than throwing.
	/// </remarks>
	public float Distance {
		get => _distanceSetpoint.TargetValue;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			if (value < MinDistance) value = MinDistance.Value;
			else if (value > MaxDistance) value = MaxDistance.Value;
			_distanceSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// Which direction around <see cref="WorldUp"/> the camera views <see cref="Target"/> from. Defaults to <see cref="YawDefault"/>.
	/// </summary>
	/// <remarks>
	/// Increasing this moves the camera anticlockwise around the target when <see cref="WorldUp"/> is pointing towards you; decreasing it moves clockwise. This is
	/// a target rather than the camera's current angle: the camera eases towards it according to <see cref="RotationSmoothingStrength"/>. Values that are not
	/// physically valid are ignored rather than throwing.
	/// </remarks>
	public Angle Yaw {
		get => _yawSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_yawSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// How far above or below <see cref="Target"/> the camera views it from. Defaults to <see cref="PitchDefault"/>.
	/// </summary>
	/// <remarks>
	/// At <c>0°</c> the camera is level with the target. <b>Increasing this raises the camera above the target so that it looks down at it, and decreasing it lowers
	/// the camera below the target so that it looks up</b> — note that this is the opposite sense to the <c>Pitch</c> of
	/// <see cref="FirstPersonCameraController"/> and <see cref="FreeFlyingCameraController"/>, where the angle aims the camera rather than placing it. Unless
	/// <see cref="AllowUpsideDownFlip"/> is <see langword="true"/>, the value is clamped to <c>-90° &lt;= n &lt;= 90°</c>
	/// during <see cref="Progress(float)"/>. This is a target rather than the camera's current angle: the camera eases towards it according to
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
	/// The point the camera orbits around and looks at. Defaults to <see cref="TargetDefault"/>.
	/// </summary>
	public Location Target { get; set; }
	
	/// <summary>
	/// Sets <see cref="Yaw"/>, <see cref="Pitch"/> and <see cref="Distance"/> and then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// Exactly equivalent to assigning the three properties and then calling <see cref="Progress(float)"/>.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="yaw">The value to assign to <see cref="Yaw"/>.</param>
	/// <param name="pitch">The value to assign to <see cref="Pitch"/>.</param>
	/// <param name="distance">The value to assign to <see cref="Distance"/>.</param>
	public void Progress(float deltaTime, Angle yaw, Angle pitch, float distance) {
		Yaw = yaw;
		Pitch = pitch;
		Distance = distance;
		Progress(deltaTime);
	}
	/// <summary>
	/// Sets the fixed constraints this controller works within, all in one call.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning the five properties individually.
	/// </remarks>
	/// <param name="target">The value to assign to <see cref="Target"/>.</param>
	/// <param name="worldUp">The value to assign to <see cref="WorldUp"/>.</param>
	/// <param name="allowUpsideDownFlip">The value to assign to <see cref="AllowUpsideDownFlip"/>.</param>
	/// <param name="minDistance">The value to assign to <see cref="MinDistance"/>.</param>
	/// <param name="maxDistance">The value to assign to <see cref="MaxDistance"/>.</param>
	public void SetConstraints(Location target, Direction worldUp, bool allowUpsideDownFlip, float? minDistance, float? maxDistance) {
		Target = target;
		WorldUp = worldUp;
		AllowUpsideDownFlip = allowUpsideDownFlip;
		MinDistance = minDistance;
		MaxDistance = maxDistance;
	}
	/// <summary>
	/// Frames the camera to inspect an object of the given size, keeping the current <see cref="WorldUp"/>.
	/// </summary>
	/// <remarks>
	/// A convenience for the common case of wanting to look at one object without working out sensible distances by hand; see the other overload for what it picks.
	/// </remarks>
	/// <param name="boundingBox">A box enclosing the object to be inspected.</param>
	public void SetConstraints(PositionedCuboid boundingBox) => SetConstraints(boundingBox, WorldUp);
	/// <summary>
	/// Frames the camera to inspect an object of the given size.
	/// </summary>
	/// <remarks>
	/// The camera is aimed at the centre of the smallest sphere enclosing <paramref name="boundingBox"/> and placed at one and a half times that sphere's radius
	/// away, free to move between roughly the object's own half-width and three times the radius. This generally fills the frame sensibly whatever the object's
	/// size, which is why it is easier than setting <see cref="Target"/>, <see cref="MinDistance"/>, <see cref="MaxDistance"/> and <see cref="Distance"/> by hand.
	/// </remarks>
	/// <param name="boundingBox">A box enclosing the object to be inspected.</param>
	/// <param name="worldUp">The value to assign to <see cref="WorldUp"/>.</param>
	public void SetConstraints(PositionedCuboid boundingBox, Direction worldUp) {
		var enclosingSphere = boundingBox.SmallestEnclosingSphere;
		Target = enclosingSphere.Position;
		WorldUp = worldUp;
		MinDistance = Single.Min(enclosingSphere.Radius * 1f, boundingBox.SmallestHalfExtent);
		MaxDistance = enclosingSphere.Radius * 3f;
		Distance = enclosingSphere.Radius * 1.5f;
	}

	/// <summary>
	/// Sets the rotation smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining angle to <see cref="Yaw"/> and <see cref="Pitch"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomRotationSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the distance smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Distance"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomDistanceSmoothingStrength(float smoothingHalfLife) {
		_distanceSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <inheritdoc />
	public void SetGlobalSmoothing(SmoothingStrength newSmoothingStrength) {
		RotationSmoothingStrength = newSmoothingStrength;
		DistanceSmoothingStrength = newSmoothingStrength;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets both smoothing strengths to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		MinDistance = MinDistanceDefault;
		MaxDistance = MaxDistanceDefault;
		WorldUp = WorldUpDefault;
		AllowUpsideDownFlip = AllowUpsideDownFlipDefault;
		Target = TargetDefault;
		_yawSetpoint.Reset(YawDefault);
		_pitchSetpoint.Reset(PitchDefault);
		_distanceSetpoint.Reset(DistanceDefault);
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
		
		_distanceSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		
		var currentHorizontalPlaneDir = _worldForward * (_yawSetpoint.CurrentValue % WorldUp);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(WorldUp, currentHorizontalPlaneDir);
		var viewDir = currentHorizontalPlaneDir * verticalTiltRot;
		
		Camera.SetPosition(Target - viewDir * _distanceSetpoint.CurrentValue);
		Camera.SetViewAndUpDirection(viewDir, WorldUp * verticalTiltRot);
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
		Pitch += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPitchSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
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
		} * (invertStickControl ? -deltaTime : deltaTime);

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
	/// <param name="leftTriggerPitchesUp">If <see langword="true"/> (the default), the left trigger pitches up and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPitchViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerPitchesUp = true) {
		ArgumentNullException.ThrowIfNull(input);
		var pitchUpTriggerPosition = leftTriggerPitchesUp ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var pitchDownTriggerPosition = leftTriggerPitchesUp ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPitch(deltaTime, pitchUpTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger)
			- pitchDownTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger));
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
		Yaw += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultYawSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
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
		} * (invertStickControl ? -deltaTime : deltaTime);

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
	/// <param name="leftTriggerYawsClockwise">If <see langword="true"/> (the default), the left trigger yaws clockwise and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustYawViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerYawsClockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var yawClockwiseTriggerPosition = leftTriggerYawsClockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var yawAnticlockwiseTriggerPosition = leftTriggerYawsClockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustYaw(deltaTime, yawAnticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger)
			- yawClockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger));
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
	/// Adjusts <see cref="Distance"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by per second, in units.</param>
	public void AdjustDistance(float deltaTime, float adjustmentPerSec) => Distance += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by for each pixel the mouse cursor moves: <c>0.002f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistanceViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistanceSensitivityMouseCursor = 0.002f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="Distance"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityMouseCursor"/> (<c>0.002f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Distance += delta * (adjustmentPerPixel ?? DefaultDistanceSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by for each notch the mouse wheel is turned: <c>0.045f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistanceViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistanceSensitivityMouseWheel = 0.045f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="Distance"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityMouseWheel"/> (<c>0.045f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Distance += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultDistanceSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistanceViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistanceSensitivityControllerStick = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Distance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Distance += (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistanceViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistanceSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Distance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerIncreasesDistance">If <see langword="true"/> (the default), the left trigger increases distance and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustDistance(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst the chosen key or button is held down: <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistanceViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistanceSensitivityKeyOrButtonPress = 1f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistanceSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Distance"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityKeyOrButtonPress"/> (<c>1f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistanceViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistanceSensitivityKeyOrButtonPress));
	}
	
	/// <summary>
	/// Adjusts <see cref="Distance"/> by a fraction of the range between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
	/// </summary>
	/// <remarks>
	/// Useful because a fixed step in metres feels very different for a small object and a large one, whereas a proportional step feels the same for both. If either
	/// limit is <see langword="null"/> there is no range to take a fraction of, and <paramref name="adjustment"/> is applied as a plain distance in metres instead.
	/// </remarks>
	/// <param name="adjustment">The fraction of the distance range to move by, where <c>1f</c> is the whole range. May be negative, to move closer.</param>
	public void AdjustDistancePercentage(float adjustment) {
		if (MaxDistance is not { } max || MinDistance is not { } min) {
			Distance += adjustment;
			return;
		}
		Distance += (adjustment * (max - min));
	}
	/// <summary>
	/// Adjusts <see cref="Distance"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by per second, in units.</param>
	public void AdjustDistancePercentage(float deltaTime, float adjustmentPerSec) => AdjustDistancePercentage(adjustmentPerSec * deltaTime);

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by for each pixel the mouse cursor moves: <c>0.02f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistancePercentageViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistancePercentageSensitivityMouseCursor = 0.02f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="Distance"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityMouseCursor"/> (<c>0.02f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		AdjustDistancePercentage(delta * (adjustmentPerPixel ?? DefaultDistancePercentageSensitivityMouseCursor));
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by for each notch the mouse wheel is turned: <c>0.05f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistancePercentageViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistancePercentageSensitivityMouseWheel = 0.05f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="Distance"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityMouseWheel"/> (<c>0.05f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustDistancePercentage(input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultDistancePercentageSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f));
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.3333f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistancePercentageViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistancePercentageSensitivityControllerStick = 0.3333f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Distance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityControllerStick"/> (<c>0.3333f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		AdjustDistancePercentage((maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerStick) * delta);
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.3333f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistancePercentageViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistancePercentageSensitivityControllerTrigger = 0.3333f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Distance"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityControllerTrigger"/> (<c>0.3333f</c>) is used.</param>
	/// <param name="leftTriggerIncreasesDistance">If <see langword="true"/> (the default), the left trigger increases distance and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustDistancePercentage(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Distance"/> is adjusted by per second whilst the chosen key or button is held down: <c>0.3333f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustDistancePercentageViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultDistancePercentageSensitivityKeyOrButtonPress = 0.3333f;
	/// <summary>
	/// Adjusts <see cref="Distance"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityKeyOrButtonPress"/> (<c>0.3333f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustDistancePercentage(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistancePercentageSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Distance"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Distance"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityKeyOrButtonPress"/> (<c>0.3333f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustDistancePercentageViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustDistancePercentage(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistancePercentageSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Moving the mouse orbits the camera around the target, and the scroll wheel moves it closer or further away. Call the individual <c>Adjust</c> methods
	/// yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, reverses which way vertical mouse movement moves the camera.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, reverses which way horizontal mouse movement moves the camera.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, reverses which way the scroll wheel moves the camera.</param>
	/// <param name="pitchAdjustmentPerPixel">How much to adjust <see cref="Pitch"/> by for each pixel of vertical cursor movement. If <see langword="null"/>, <see cref="DefaultPitchSensitivityMouseCursor"/> is used.</param>
	/// <param name="yawAdjustmentPerPixel">How much to adjust <see cref="Yaw"/> by for each pixel of horizontal cursor movement. If <see langword="null"/>, <see cref="DefaultYawSensitivityMouseCursor"/> is used.</param>
	/// <param name="distancePercentageAdjustmentPerWheelIncrement">What fraction of the distance range to move by per wheel notch. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityMouseWheel"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? distancePercentageAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);
		AdjustDistancePercentageViaMouseWheel(input, distancePercentageAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The right stick orbits the camera around the target, and the two triggers move it closer and further away. Call the individual <c>Adjust</c> methods yourself
	/// if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPitchControl">If <see langword="true"/>, reverses which way the stick moves the camera vertically.</param>
	/// <param name="invertYawControl">If <see langword="true"/>, reverses which way the stick moves the camera horizontally.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, swaps which trigger moves the camera towards the target and which moves it away.</param>
	/// <param name="maxPitchAdjustmentPerSec">How much to adjust <see cref="Pitch"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultPitchSensitivityControllerStick"/> is used.</param>
	/// <param name="maxYawAdjustmentPerSec">How much to adjust <see cref="Yaw"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultYawSensitivityControllerStick"/> is used.</param>
	/// <param name="maxDistancePercentageAdjustmentPerSec">What fraction of the distance range to move by per second at full trigger depression. If <see langword="null"/>, <see cref="DefaultDistancePercentageSensitivityControllerTrigger"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxDistancePercentageAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);
		AdjustDistancePercentageViaControllerTriggers(input, deltaTime, maxDistancePercentageAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
