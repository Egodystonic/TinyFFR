// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Orbits a camera around a target on a circle at a configurable height, always pointing at it.
/// </summary>
/// <remarks>
/// <para>
/// The camera's placement is described by three values: <see cref="Angle"/> (how far around the circle it has travelled), <see cref="Distance"/> (the radius of that
/// circle) and <see cref="Height"/> (how far above the target the circle sits). This suits "satellite" or "track-cam" shots that revolve steadily around a fixed
/// point.
/// </para>
/// <para>
/// It is closely related to <see cref="InspectorCameraController"/>; the difference is that this positions the camera by an angle plus a separate height, whereas
/// that one positions it by two angles on a sphere.
/// </para>
/// </remarks>
public sealed class OrbitalCameraController : ICameraController<OrbitalCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<OrbitalCameraController> _controllerPool = new(&New);
	static OrbitalCameraController New() => new();
	static OrbitalCameraController ICameraController<OrbitalCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(OrbitalCameraController));
	OrbitalCameraController() { }
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
	/// The default value for <see cref="Angle"/>: <c>0°</c>.
	/// </summary>
	public static readonly Angle AngleDefault = Angle.Zero;
	/// <summary>
	/// The default value for <see cref="Target"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location TargetDefault = Location.Origin;
	/// <summary>
	/// The default value for <see cref="UpDirection"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction UpDirectionDefault = Direction.Up;
	/// <summary>
	/// The default value for <see cref="ZeroAngleDirection"/>: <see cref="Direction.None"/>, meaning an arbitrary direction perpendicular to <see cref="UpDirection"/> is chosen.
	/// </summary>
	public static readonly Direction ZeroAngleDirectionDefault = Direction.None;
	/// <summary>
	/// The default value for <see cref="AngleRange"/>: <see langword="null"/> (no limit).
	/// </summary>
	public static readonly Angle? AngleRangeDefault = null;
	/// <summary>
	/// The default value for <see cref="MaxHeight"/>: <c>0.5f</c>.
	/// </summary>
	public static readonly float MaxHeightDefault = 0.5f;
	/// <summary>
	/// The default value for <see cref="MinHeight"/>: <c>0.1f</c>.
	/// </summary>
	public static readonly float MinHeightDefault = 0.1f;
	/// <summary>
	/// The default value for <see cref="MaxDistance"/>: <c>2f</c>.
	/// </summary>
	public static readonly float MaxDistanceDefault = 2f;
	/// <summary>
	/// The default value for <see cref="MinDistance"/>: <c>0.6f</c>.
	/// </summary>
	public static readonly float MinDistanceDefault = 0.6f;
	
	readonly SpringAngleBasedCameraSetpoint _angleSetpoint = new();
	readonly CameraEffectStrengthMap _angleSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.4f,
		VeryStrong: 0.65f
	);
	readonly Spring1DBasedCameraSetpoint _heightSetpoint = new();
	readonly CameraEffectStrengthMap _heightSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.10f,
		Standard: 0.15f,
		Strong: 0.25f,
		VeryStrong: 0.4f
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

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Angle"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomAngleSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength AngleSmoothingStrength {
		get => _angleSmoothingStrengthMap.From(_angleSetpoint.HalfLife);
		set => _angleSetpoint.HalfLife = _angleSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Height"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomHeightSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength HeightSmoothingStrength {
		get => _heightSmoothingStrengthMap.From(_heightSetpoint.HalfLife);
		set => _heightSetpoint.HalfLife = _heightSmoothingStrengthMap.From(value);
	}
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
	/// How far the camera is permitted to orbit away from <see cref="ZeroAngleDirection"/>, or <see langword="null"/> for no limit. Defaults to <see cref="AngleRangeDefault"/>.
	/// </summary>
	/// <remarks>
	/// The limit is applied half in each direction, so a value of <c>160°</c> lets the camera orbit <c>80°</c> anticlockwise and <c>80°</c> clockwise from
	/// <see cref="ZeroAngleDirection"/>. Values larger than a full turn are treated as <see langword="null"/>, and non-finite values are ignored rather than throwing.
	/// </remarks>
	public Angle? AngleRange {
		get; 
		set {
			if (!Single.IsFinite(value?.Radians ?? 0f)) return;
			var absVal = value?.Absolute;
			if (absVal > Angle.FullCircle) absVal = null;
			field = absVal;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Angle = Angle;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The lowest the camera may sit above <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MinHeightDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this past <see cref="MaxHeight"/> raises that property to match, and <see cref="Height"/> is re-clamped immediately. Values that are not positive and finite are ignored
	/// rather than throwing.
	/// </remarks>
	public float? MinHeight {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value > MaxHeight) MaxHeight = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Height = Height;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The highest the camera may sit above <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MaxHeightDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this past <see cref="MinHeight"/> lowers that property to match, and <see cref="Height"/> is re-clamped immediately. Values that are not positive and finite are ignored
	/// rather than throwing.
	/// </remarks>
	public float? MaxHeight {
		get;
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value < MinHeight) MinHeight = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Height = Height;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The closest the camera may sit to <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MinDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this past <see cref="MaxDistance"/> raises that property to match, and <see cref="Distance"/> is re-clamped immediately. Values that are not positive and finite are ignored
	/// rather than throwing.
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
	/// The furthest the camera may sit from <see cref="Target"/>, in metres, or <see langword="null"/> for no limit. Must be positive. Defaults to <see cref="MaxDistanceDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this past <see cref="MinDistance"/> lowers that property to match, and <see cref="Distance"/> is re-clamped immediately. Values that are not positive and finite are ignored
	/// rather than throwing.
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
	/// How far above <see cref="Target"/> the camera orbits, in metres.
	/// </summary>
	/// <remarks>
	/// Measured along <see cref="UpDirection"/>. Values are clamped to between <see cref="MinHeight"/> and <see cref="MaxHeight"/> as they are set, so reading this
	/// back may not return what you assigned. This is a target rather than the camera's current height: the camera eases towards it according to
	/// <see cref="HeightSmoothingStrength"/>. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public float Height {
		get => _heightSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value)) return;
			if (value < MinHeight) value = MinHeight.Value;
			else if (value > MaxHeight) value = MaxHeight.Value;
			_heightSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// Which way is "up"; the axis the camera orbits around, and the direction <see cref="Height"/> is measured along. Defaults to <see cref="UpDirectionDefault"/>.
	/// </summary>
	/// <remarks>
	/// Setting this also re-derives <see cref="ZeroAngleDirection"/>, since the two are always kept at right angles to one another. Values that are not physically
	/// valid, and <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction UpDirection {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-orthogonalization
			ZeroAngleDirection = ZeroAngleDirection;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The direction from <see cref="Target"/> towards the camera when <see cref="Angle"/> is <c>0°</c>; i.e. where the camera starts on its orbit. Defaults to <see cref="ZeroAngleDirectionDefault"/>.
	/// </summary>
	/// <remarks>
	/// This is always kept at right angles to <see cref="UpDirection"/>: the value you supply is straightened against it rather than used verbatim, so reading it
	/// back may not return exactly what you set. Supplying a direction parallel to <see cref="UpDirection"/> (or <see cref="Direction.None"/>) leaves an arbitrary
	/// perpendicular direction chosen in its place.
	/// </remarks>
	public Direction ZeroAngleDirection {
		get;
		set {
			field = value.OrthogonalizedAgainst(UpDirection) ?? Direction.None;
			if (!field.IsPhysicallyValidAndNotNone) field = UpDirection.AnyOrthogonal();
		}
	}
	/// <summary>
	/// The radius of the circle the camera orbits on; i.e. how far it sits from <see cref="Target"/>, in metres.
	/// </summary>
	/// <remarks>
	/// Values are clamped to between <see cref="MinDistance"/> and <see cref="MaxDistance"/> as they are set, so reading this back may not return what you assigned.
	/// This is a target rather than the camera's current distance: the camera eases towards it according to <see cref="DistanceSmoothingStrength"/>. Non-finite
	/// values are ignored rather than throwing.
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
	/// How far around its orbit the camera has travelled from <see cref="ZeroAngleDirection"/>. Defaults to <see cref="AngleDefault"/>.
	/// </summary>
	/// <remarks>
	/// Increasing this moves the camera anticlockwise around <see cref="Target"/> when <see cref="UpDirection"/> is pointing towards you; decreasing it moves
	/// clockwise. The value is constrained by <see cref="AngleRange"/> where one is set. This is a target rather than the camera's current angle: the camera eases
	/// towards it according to <see cref="AngleSmoothingStrength"/>. Values that are not physically valid are ignored rather than throwing.
	/// </remarks>
	public Angle Angle {
		get => _angleSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
			if (AngleRange is { } nonNullMaxAngleAbs) {
				if (nonNullMaxAngleAbs <= Angle.Zero) {
					_angleSetpoint.TargetValue = Angle.Zero;
					return;
				}
				var normalized = value.Normalized;
				var half = nonNullMaxAngleAbs * 0.5f;
				var negHalfNorm = (-half).Normalized;
				var amountOver = normalized - half;
				var amountUnder = negHalfNorm - normalized;
				if (amountOver > Angle.Zero && amountUnder > Angle.Zero) {
					value = amountOver > amountUnder ? negHalfNorm : half;
				}
			}
			_angleSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// The point the camera orbits around and looks at. Defaults to <see cref="TargetDefault"/>.
	/// </summary>
	public Location Target { get; set; }

	/// <summary>
	/// Sets the angle smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Angle"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomAngleSmoothingStrength(float smoothingHalfLife) {
		_angleSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the height smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Height"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomHeightSmoothingStrength(float smoothingHalfLife) {
		_heightSetpoint.HalfLife = smoothingHalfLife;
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
		AngleSmoothingStrength = newSmoothingStrength;
		HeightSmoothingStrength = newSmoothingStrength;
		DistanceSmoothingStrength = newSmoothingStrength;
	}

	/// <summary>
	/// Sets <see cref="Angle"/>, <see cref="Height"/> and <see cref="Distance"/> and then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// Exactly equivalent to assigning the three properties and then calling <see cref="Progress(float)"/>.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="angle">The value to assign to <see cref="Angle"/>.</param>
	/// <param name="height">The value to assign to <see cref="Height"/>.</param>
	/// <param name="distance">The value to assign to <see cref="Distance"/>.</param>
	public void Progress(float deltaTime, Angle angle, float height, float distance) {
		Angle = angle;
		Height = height;
		Distance = distance;
		Progress(deltaTime);
	}
	/// <summary>
	/// Sets the fixed constraints this controller works within, all in one call.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning the eight properties individually. Note that <paramref name="zeroAngleDirection"/> is straightened against
	/// <paramref name="upDirection"/>, as described on <see cref="ZeroAngleDirection"/>.
	/// </remarks>
	/// <param name="target">The value to assign to <see cref="Target"/>.</param>
	/// <param name="upDirection">The value to assign to <see cref="UpDirection"/>.</param>
	/// <param name="zeroAngleDirection">The value to assign to <see cref="ZeroAngleDirection"/>.</param>
	/// <param name="angleRange">The value to assign to <see cref="AngleRange"/>.</param>
	/// <param name="minHeight">The value to assign to <see cref="MinHeight"/>.</param>
	/// <param name="maxHeight">The value to assign to <see cref="MaxHeight"/>.</param>
	/// <param name="minDistance">The value to assign to <see cref="MinDistance"/>.</param>
	/// <param name="maxDistance">The value to assign to <see cref="MaxDistance"/>.</param>
	public void SetConstraints(Location target, Direction upDirection, Direction zeroAngleDirection, Angle? angleRange, float? minHeight, float? maxHeight, float? minDistance, float? maxDistance) {
		Target = target;
		UpDirection = upDirection;
		ZeroAngleDirection = zeroAngleDirection;
		AngleRange = angleRange;
		MinHeight = minHeight;
		MaxHeight = maxHeight;
		MinDistance = minDistance;
		MaxDistance = maxDistance;
	}
	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets every smoothing strength to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		AngleRange = AngleRangeDefault;
		MinHeight = MinHeightDefault;
		MaxHeight = MaxHeightDefault;
		MinDistance = MinDistanceDefault;
		MaxDistance = MaxDistanceDefault;
		UpDirection = UpDirectionDefault;
		ZeroAngleDirection = ZeroAngleDirectionDefault;
		Target = TargetDefault;
		_angleSetpoint.Reset(AngleDefault);
		_heightSetpoint.Reset(MinHeightDefault);
		_distanceSetpoint.Reset(MinDistanceDefault);
		SetGlobalSmoothing(SmoothingStrength.VeryMild);
	}

	/// <inheritdoc />
	public void Progress(float deltaTime) {
		_angleSetpoint.Progress(deltaTime);
		_heightSetpoint.Progress(deltaTime);
		_distanceSetpoint.Progress(deltaTime);

		var planarOffset = (_angleSetpoint.CurrentValue % UpDirection) * ZeroAngleDirection * _distanceSetpoint.CurrentValue;
		var heightOffset = UpDirection * _heightSetpoint.CurrentValue;
		Camera.SetPosition(Target + planarOffset + heightOffset);
		Camera.LookAt(Target, UpDirection);
	}

	/// <summary>
	/// Adjusts <see cref="Angle"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Angle"/> by per second, in degrees.</param>
	public void AdjustAngle(float deltaTime, Angle adjustmentPerSec) => Angle += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Angle"/> is adjusted by for each pixel the mouse cursor moves: <c>0.04f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustAngleViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultAngleSensitivityMouseCursor = 0.04f;
	/// <summary>
	/// Adjusts <see cref="Angle"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How much to adjust <see cref="Angle"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultAngleSensitivityMouseCursor"/> (<c>0.04f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Angle += delta * (adjustmentPerPixel ?? DefaultAngleSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="Angle"/> is adjusted by for each notch the mouse wheel is turned: <c>5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustAngleViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultAngleSensitivityMouseWheel = 5f;
	/// <summary>
	/// Adjusts <see cref="Angle"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How much to adjust <see cref="Angle"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultAngleSensitivityMouseWheel"/> (<c>5f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Angle += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultAngleSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	/// <summary>
	/// The default amount <see cref="Angle"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustAngleViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultAngleSensitivityControllerStick = 120f;
	/// <summary>
	/// Adjusts <see cref="Angle"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Angle"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultAngleSensitivityControllerStick"/> (<c>120f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Angle += (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="Angle"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustAngleViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultAngleSensitivityControllerTrigger = 120f;
	/// <summary>
	/// Adjusts <see cref="Angle"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Angle"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultAngleSensitivityControllerTrigger"/> (<c>120f</c>) is used.</param>
	/// <param name="leftTriggerRotatesClockwise">If <see langword="true"/> (the default), the left trigger rotates clockwise and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerRotatesClockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var clockwiseTriggerPosition = leftTriggerRotatesClockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var anticlockwiseTriggerPosition = leftTriggerRotatesClockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustAngle(deltaTime, anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerTrigger)
			- clockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Angle"/> is adjusted by per second whilst the chosen key or button is held down: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustAngleViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultAngleSensitivityKeyOrButtonPress = 120f;
	/// <summary>
	/// Adjusts <see cref="Angle"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Angle"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultAngleSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustAngle(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultAngleSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Angle"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Angle"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultAngleSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAngleViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustAngle(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultAngleSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="Height"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Height"/> by per second, in units.</param>
	public void AdjustHeight(float deltaTime, float adjustmentPerSec) => Height += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Height"/> is adjusted by for each pixel the mouse cursor moves: <c>0.0002f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustHeightViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultHeightSensitivityMouseCursor = 0.0002f;
	/// <summary>
	/// Adjusts <see cref="Height"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="Height"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultHeightSensitivityMouseCursor"/> (<c>0.0002f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Height += delta * (adjustmentPerPixel ?? DefaultHeightSensitivityMouseCursor);
	}

	/// <summary>
	/// The default amount <see cref="Height"/> is adjusted by for each notch the mouse wheel is turned: <c>0.025f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustHeightViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultHeightSensitivityMouseWheel = 0.025f;
	/// <summary>
	/// Adjusts <see cref="Height"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="Height"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultHeightSensitivityMouseWheel"/> (<c>0.025f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Height += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultHeightSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	/// <summary>
	/// The default amount <see cref="Height"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustHeightViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultHeightSensitivityControllerStick = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Height"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Height"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultHeightSensitivityControllerStick"/> (<c>0.5f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Height += (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerStick) * delta;
	}

	/// <summary>
	/// The default amount <see cref="Height"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustHeightViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultHeightSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Height"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Height"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultHeightSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="leftTriggerRaisesHeight">If <see langword="true"/> (the default), the left trigger raises height and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerRaisesHeight = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerRaisesHeight ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerRaisesHeight ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustHeight(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerTrigger));
	}

	/// <summary>
	/// The default amount <see cref="Height"/> is adjusted by per second whilst the chosen key or button is held down: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustHeightViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultHeightSensitivityKeyOrButtonPress = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Height"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Height"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultHeightSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultHeightSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Height"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Height"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultHeightSensitivityKeyOrButtonPress"/> (<c>0.5f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustHeightViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultHeightSensitivityKeyOrButtonPress));
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
	/// Adjusts every parameter of this controller according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Horizontal mouse movement orbits the camera around the target, vertical movement raises and lowers it, and the scroll wheel moves it closer or further away.
	/// Call the individual <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertAngleControl">If <see langword="true"/>, reverses which way the camera orbits for a given mouse movement.</param>
	/// <param name="invertHeightControl">If <see langword="true"/>, reverses which way vertical mouse movement raises and lowers the camera.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, reverses which way the scroll wheel moves the camera.</param>
	/// <param name="angleAdjustmentPerPixel">How much to adjust <see cref="Angle"/> by for each pixel of horizontal cursor movement. If <see langword="null"/>, <see cref="DefaultAngleSensitivityMouseCursor"/> is used.</param>
	/// <param name="heightAdjustmentPerPixel">How far to adjust <see cref="Height"/> by for each pixel of vertical cursor movement. If <see langword="null"/>, <see cref="DefaultHeightSensitivityMouseCursor"/> is used.</param>
	/// <param name="distanceAdjustmentPerWheelIncrement">How far to adjust <see cref="Distance"/> by per wheel notch. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityMouseWheel"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? angleAdjustmentPerPixel = null, float? heightAdjustmentPerPixel = null, float? distanceAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustAngleViaMouseCursor(input, angleAdjustmentPerPixel, invertMouseControl: invertAngleControl);
		AdjustHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
		AdjustDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
	}

	/// <summary>
	/// Adjusts every parameter of this controller according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The right stick orbits the camera around the target and raises and lowers it, whilst the two triggers move it closer and further away. Call the individual
	/// <c>Adjust</c> methods yourself if you want a different mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertAngleControl">If <see langword="true"/>, reverses which way the camera orbits for a given stick displacement.</param>
	/// <param name="invertHeightControl">If <see langword="true"/>, reverses which way the stick raises and lowers the camera.</param>
	/// <param name="invertDistanceControl">If <see langword="true"/>, swaps which trigger moves the camera towards the target and which moves it away.</param>
	/// <param name="maxAngleAdjustmentPerSec">How much to adjust <see cref="Angle"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultAngleSensitivityControllerStick"/> is used.</param>
	/// <param name="maxHeightAdjustmentPerSec">How far to adjust <see cref="Height"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultHeightSensitivityControllerStick"/> is used.</param>
	/// <param name="maxDistanceAdjustmentPerSec">How far to adjust <see cref="Distance"/> by per second at full trigger depression. If <see langword="null"/>, <see cref="DefaultDistanceSensitivityControllerTrigger"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? maxAngleAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxDistanceAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustAngleViaControllerStick(input, deltaTime, maxAngleAdjustmentPerSec, invertStickControl: invertAngleControl);
		AdjustHeightViaControllerStick(input, deltaTime, maxHeightAdjustmentPerSec, invertStickControl: invertHeightControl);
		AdjustDistanceViaControllerTriggers(input, deltaTime, maxDistanceAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
