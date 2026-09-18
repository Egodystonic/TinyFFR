// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Holds a camera at a fixed position and aims it by panning, tilting and zooming, in the manner of a mounted security or television camera.
/// </summary>
/// <remarks>
/// Unlike the other controllers, this one never moves the camera: <see cref="Position"/> is set once and the camera stays there. What changes is where it looks
/// (<see cref="Pan"/> swings it left and right, <see cref="Tilt"/> swings it up and down) and how tightly it is framed, via <see cref="Zoom"/>. Each of the three is
/// bounded, so the camera can be restricted to a realistic range of movement.
/// </remarks>
public sealed class PanTiltZoomCameraController : ICameraController<PanTiltZoomCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<PanTiltZoomCameraController> _controllerPool = new(&New);
	static PanTiltZoomCameraController New() => new();
	static PanTiltZoomCameraController ICameraController<PanTiltZoomCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	/// <inheritdoc />
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(PanTiltZoomCameraController));
	PanTiltZoomCameraController() { }
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
	/// The default value for <see cref="Pan"/>: <c>0°</c>.
	/// </summary>
	public static readonly Angle PanDefault = Angle.Zero;
	/// <summary>
	/// The default value for <see cref="Tilt"/>: <c>0°</c>.
	/// </summary>
	public static readonly Angle TiltDefault = Angle.Zero;
	/// <summary>
	/// The default value for <see cref="Zoom"/>: <c>0.5f</c> (halfway between fully zoomed out and fully zoomed in).
	/// </summary>
	public static readonly float ZoomDefault = 0.5f;
	/// <summary>
	/// The default value for <see cref="Position"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location PositionDefault = Location.Origin;
	/// <summary>
	/// The default value for <see cref="UpDirection"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction UpDirectionDefault = Direction.Up;
	/// <summary>
	/// The default value for <see cref="ZeroPanTiltDirection"/>: <see cref="Direction.Forward"/>.
	/// </summary>
	public static readonly Direction ZeroPanTiltDirectionDefault = Direction.Forward;
	/// <summary>
	/// The default value for <see cref="PanRange"/>: <c>160°</c>.
	/// </summary>
	public static readonly Angle? PanRangeDefault = 160f;
	/// <summary>
	/// The default value for <see cref="MaxTiltUp"/>: <c>35°</c>.
	/// </summary>
	public static readonly Angle MaxTiltUpDefault = 35f;
	/// <summary>
	/// The default value for <see cref="MaxTiltDown"/>: <c>55°</c>.
	/// </summary>
	public static readonly Angle MaxTiltDownDefault = 55f;
	/// <summary>
	/// The default value for <see cref="MaxZoomInFov"/>: <c>15°</c>.
	/// </summary>
	public static readonly Angle MaxZoomInFovDefault = 15f;
	/// <summary>
	/// The default value for <see cref="MaxZoomOutFov"/>: <c>90°</c>.
	/// </summary>
	public static readonly Angle MaxZoomOutFovDefault = 90f;
	
	readonly SpringAngleBasedCameraSetpoint _panSetpoint = new();
	readonly CameraEffectStrengthMap _panSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.4f,
		VeryStrong: 0.65f
	);
	readonly SpringAngleBasedCameraSetpoint _tiltSetpoint = new();
	readonly CameraEffectStrengthMap _tiltSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.5f,
		VeryStrong: 0.65f
	);
	readonly SpringAngleBasedCameraSetpoint _zoomSetpoint = new();
	readonly CameraEffectStrengthMap _zoomSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.1f,
		Mild: 0.15f,
		Standard: 0.2f,
		Strong: 0.3f,
		VeryStrong: 0.5f
	);

	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Pan"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomPanSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength PanSmoothingStrength {
		get => _panSmoothingStrengthMap.From(_panSetpoint.HalfLife);
		set => _panSetpoint.HalfLife = _panSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Tilt"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomTiltSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength TiltSmoothingStrength {
		get => _tiltSmoothingStrengthMap.From(_tiltSetpoint.HalfLife);
		set => _tiltSetpoint.HalfLife = _tiltSmoothingStrengthMap.From(value);
	}
	/// <summary>
	/// How heavily this controller smooths the camera's movement towards <see cref="Zoom"/>.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetCustomZoomSmoothingStrength"/> to specify a half-life directly instead of picking one of these presets.
	/// </remarks>
	public SmoothingStrength ZoomSmoothingStrength {
		get => _zoomSmoothingStrengthMap.From(_zoomSetpoint.HalfLife);
		set => _zoomSetpoint.HalfLife = _zoomSmoothingStrengthMap.From(value);
	}
	
	/// <summary>
	/// How far the camera is permitted to pan away from <see cref="ZeroPanTiltDirection"/>, or <see langword="null"/> for no limit. Defaults to <see cref="PanRangeDefault"/>.
	/// </summary>
	/// <remarks>
	/// The limit is applied half in each direction, so a value of <c>160°</c> lets the camera pan <c>80°</c> to the left and <c>80°</c> to the right of
	/// <see cref="ZeroPanTiltDirection"/>. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public Angle? PanRange {
		get; 
		set {
			if (!Single.IsFinite(value?.Radians ?? 0f)) return;
			var absVal = value?.Absolute;
			if (absVal > Angle.FullCircle) absVal = null;
			field = absVal;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Pan = Pan;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// How far above <see cref="ZeroPanTiltDirection"/> the camera is permitted to tilt. Clamped to <c>0° &lt;= n &lt;= 180°</c>. Defaults to <see cref="MaxTiltUpDefault"/>.
	/// </summary>
	/// <remarks>
	/// This bounds the positive end of <see cref="Tilt"/>. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public Angle MaxTiltUp {
		get; 
		set {
			if (!Single.IsFinite(value.Radians)) return;
			field = value.Clamp(Angle.Zero, Angle.HalfCircle);
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Tilt = Tilt;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// How far below <see cref="ZeroPanTiltDirection"/> the camera is permitted to tilt. Clamped to <c>0° &lt;= n &lt;= 180°</c>. Defaults to <see cref="MaxTiltDownDefault"/>.
	/// </summary>
	/// <remarks>
	/// This bounds the negative end of <see cref="Tilt"/>; it is given as a positive magnitude rather than a negative angle. Non-finite values are ignored rather
	/// than throwing.
	/// </remarks>
	public Angle MaxTiltDown {
		get; 
		set {
			if (!Single.IsFinite(value.Radians)) return;
			field = value.Clamp(Angle.Zero, Angle.HalfCircle);
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Tilt = Tilt;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The camera's vertical field of view when <see cref="Zoom"/> is <c>1f</c>; i.e. how tightly it can zoom in. Must be positive. Defaults to <see cref="MaxZoomInFovDefault"/>.
	/// </summary>
	/// <remarks>
	/// A narrower field of view means a more magnified image, so this is the <i>smaller</i> of the two field-of-view bounds. Setting it larger than
	/// <see cref="MaxZoomOutFov"/> raises that property to match. Values that are not positive and finite are ignored rather than throwing.
	/// </remarks>
	public Angle MaxZoomInFov {
		get; 
		set {
			if (!value.Radians.IsPositiveAndFinite()) return;
			field = value;
			if (value > MaxZoomOutFov) MaxZoomOutFov = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Zoom = Zoom;
#pragma warning restore CA2245
		}
	}
	/// <summary>
	/// The camera's vertical field of view when <see cref="Zoom"/> is <c>0f</c>; i.e. how far it can zoom out. Must be positive. Defaults to <see cref="MaxZoomOutFovDefault"/>.
	/// </summary>
	/// <remarks>
	/// A wider field of view takes in more of the scene, so this is the <i>larger</i> of the two field-of-view bounds. Setting it smaller than
	/// <see cref="MaxZoomInFov"/> lowers that property to match. Values that are not positive and finite are ignored rather than throwing.
	/// </remarks>
	public Angle MaxZoomOutFov {
		get; 
		set {
			if (!value.Radians.IsPositiveAndFinite()) return;
			field = value;
			if (value < MaxZoomInFov) MaxZoomInFov = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Zoom = Zoom;
#pragma warning restore CA2245
		}
	}
	
	/// <summary>
	/// How far the camera is turned to the left of <see cref="ZeroPanTiltDirection"/>, around <see cref="UpDirection"/>. Defaults to <see cref="PanDefault"/>.
	/// </summary>
	/// <remarks>
	/// Increasing this turns the camera to the left; decreasing it turns to the right. The value is constrained by <see cref="PanRange"/> where one is set. This is
	/// a target rather than the camera's current heading: the camera eases towards it according to <see cref="PanSmoothingStrength"/>. Non-finite values are ignored
	/// rather than throwing.
	/// </remarks>
	public Angle Pan {
		get => _panSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
			if (PanRange is { } nonNullRange) {
				var normalized = value.Normalized;
				var half = nonNullRange * 0.5f;
				var negHalfNorm = (-half).Normalized;
				var amountOver = normalized - half;
				var amountUnder = negHalfNorm - normalized;
				if (amountOver > Angle.Zero && amountUnder > Angle.Zero) {
					value = amountOver > amountUnder ? negHalfNorm : half;
				}
			}
			_panSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// How far the camera is tilted upward from <see cref="ZeroPanTiltDirection"/>, towards <see cref="UpDirection"/>. Defaults to <see cref="TiltDefault"/>.
	/// </summary>
	/// <remarks>
	/// <b>Increasing this tilts the camera upward and decreasing it tilts downward</b>. Note that this is the opposite sense to the <c>Pitch</c> of
	/// <see cref="FirstPersonCameraController"/> and <see cref="FreeFlyingCameraController"/>. Values are clamped to between the negative of
	/// <see cref="MaxTiltDown"/> and <see cref="MaxTiltUp"/> as they are set, so reading this back may not return what you assigned; the value read back is
	/// normalized to the range <c>-180° &lt; n &lt;= 180°</c>. This is a target rather than the camera's current tilt: the camera eases towards it according to
	/// <see cref="TiltSmoothingStrength"/>. Non-finite values are ignored rather than throwing.
	/// </remarks>
	public Angle Tilt {
		get {
			var normalized = _tiltSetpoint.TargetValue;
			return normalized > Angle.HalfCircle ? normalized - Angle.FullCircle : normalized;
		}
		set {
			if (!Single.IsFinite(value.Radians)) return;
			value = value.Clamp(-MaxTiltDown, MaxTiltUp);
			_tiltSetpoint.TargetValue = value;
		}
	}
	/// <summary>
	/// How far the camera is zoomed in, from <c>0f</c> (fully zoomed out) to <c>1f</c> (fully zoomed in). Defaults to <see cref="ZoomDefault"/>.
	/// </summary>
	/// <remarks>
	/// Zooming works by narrowing the camera's field of view rather than by moving it, exactly as a physical zoom lens does. This value is a position between
	/// <see cref="MaxZoomOutFov"/> and <see cref="MaxZoomInFov"/> rather than an angle itself, which keeps the control meaningful whatever those two bounds are set
	/// to. Values are clamped to <c>0f &lt;= n &lt;= 1f</c>, and non-finite values are ignored rather than throwing.
	/// </remarks>
	public float Zoom {
		get => _zoomSetpoint.TargetValue.RemapRange(new Pair<Angle, Angle>(MaxZoomOutFov, MaxZoomInFov), new Pair<Angle, Angle>(Angle.FromRadians(0f), Angle.FromRadians(1f))).Radians;
		set {
			if (!Single.IsFinite(value)) return;
			_zoomSetpoint.TargetValue = Angle.FromRadians(((Real) value).Clamp(0f, 1f).RemapRange(new Pair<Real, Real>(0f, 1f), new Pair<Real, Real>(MaxZoomOutFov.Radians, MaxZoomInFov.Radians)));
		}
	}
	/// <summary>
	/// The direction the camera points when <see cref="Pan"/> and <see cref="Tilt"/> are both <c>0°</c>; i.e. the middle of its range of movement. Defaults to <see cref="ZeroPanTiltDirectionDefault"/>.
	/// </summary>
	/// <remarks>
	/// Values that are not physically valid, and <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction ZeroPanTiltDirection {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
		}
	}
	/// <summary>
	/// Which way is "up" for this camera; the axis it pans around, and the direction it tilts towards as <see cref="Tilt"/> increases. Defaults to <see cref="UpDirectionDefault"/>.
	/// </summary>
	/// <remarks>
	/// Values that are not physically valid, and <see cref="Direction.None"/>, are ignored rather than throwing.
	/// </remarks>
	public Direction UpDirection {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
		}
	}
	/// <summary>
	/// Where the camera is mounted. Defaults to <see cref="PositionDefault"/>.
	/// </summary>
	/// <remarks>
	/// This controller never moves the camera, so unlike the other controllers' position properties this is a fixed mounting point rather than a target the camera
	/// eases towards; it takes effect immediately on the next <see cref="Progress(float)"/>.
	/// </remarks>
	public Location Position { get; set; }

	/// <summary>
	/// Sets the pan smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Pan"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomPanSmoothingStrength(float smoothingHalfLife) {
		_panSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the tilt smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Tilt"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomTiltSmoothingStrength(float smoothingHalfLife) {
		_tiltSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <summary>
	/// Sets the zoom smoothing half-life directly, instead of picking one of the <see cref="SmoothingStrength"/> presets.
	/// </summary>
	/// <param name="smoothingHalfLife">The time, in seconds, the camera should take to cover half the remaining distance to <see cref="Zoom"/>. <c>0f</c> disables smoothing.</param>
	public void SetCustomZoomSmoothingStrength(float smoothingHalfLife) {
		_zoomSetpoint.HalfLife = smoothingHalfLife;
	}
	/// <inheritdoc />
	public void SetGlobalSmoothing(SmoothingStrength newSmoothingStrength) {
		PanSmoothingStrength = newSmoothingStrength;
		ZoomSmoothingStrength = newSmoothingStrength;
		TiltSmoothingStrength = newSmoothingStrength;
	}

	/// <summary>
	/// Sets <see cref="Pan"/>, <see cref="Tilt"/> and <see cref="Zoom"/> and then advances this controller, all in one call.
	/// </summary>
	/// <remarks>
	/// Exactly equivalent to assigning the three properties and then calling <see cref="Progress(float)"/>.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="pan">The value to assign to <see cref="Pan"/>.</param>
	/// <param name="tilt">The value to assign to <see cref="Tilt"/>.</param>
	/// <param name="zoom">The value to assign to <see cref="Zoom"/>.</param>
	public void Progress(float deltaTime, Angle pan, Angle tilt, float zoom) {
		Pan = pan;
		Tilt = tilt;
		Zoom = zoom;
		Progress(deltaTime);
	}
	/// <summary>
	/// Sets the camera's mounting point and its permitted range of movement, all in one call.
	/// </summary>
	/// <remarks>
	/// Equivalent to assigning the eight properties individually.
	/// </remarks>
	/// <param name="position">The value to assign to <see cref="Position"/>.</param>
	/// <param name="upDirection">The value to assign to <see cref="UpDirection"/>.</param>
	/// <param name="zeroPanTiltDirection">The value to assign to <see cref="ZeroPanTiltDirection"/>.</param>
	/// <param name="panRange">The value to assign to <see cref="PanRange"/>.</param>
	/// <param name="maxTiltUp">The value to assign to <see cref="MaxTiltUp"/>.</param>
	/// <param name="maxTiltDown">The value to assign to <see cref="MaxTiltDown"/>.</param>
	/// <param name="maxZoomInFov">The value to assign to <see cref="MaxZoomInFov"/>.</param>
	/// <param name="maxZoomOutFov">The value to assign to <see cref="MaxZoomOutFov"/>.</param>
	public void SetConstraints(Location position, Direction upDirection, Direction zeroPanTiltDirection, Angle? panRange, Angle maxTiltUp, Angle maxTiltDown, Angle maxZoomInFov, Angle maxZoomOutFov) {
		Position = position;
		UpDirection = upDirection;
		ZeroPanTiltDirection = zeroPanTiltDirection;
		PanRange = panRange;
		MaxTiltUp = maxTiltUp;
		MaxTiltDown = maxTiltDown;
		MaxZoomInFov = maxZoomInFov;
		MaxZoomOutFov = maxZoomOutFov;
	}
	/// <inheritdoc />
	/// <remarks>
	/// Note that this sets every smoothing strength to <see cref="SmoothingStrength.VeryMild"/> rather than to <see cref="SmoothingStrength.None"/>.
	/// </remarks>
	public void ResetParametersToDefault() {
		PanRange = PanRangeDefault;
		MaxTiltUp = MaxTiltUpDefault;
		MaxTiltDown = MaxTiltDownDefault;
		MaxZoomInFov = MaxZoomInFovDefault;
		MaxZoomOutFov = MaxZoomOutFovDefault;
		ZeroPanTiltDirection = ZeroPanTiltDirectionDefault;
		UpDirection = UpDirectionDefault;
		Position = PositionDefault;
		_panSetpoint.Reset(PanDefault);
		_tiltSetpoint.Reset(TiltDefault);
		_zoomSetpoint.Reset((MaxZoomOutFovDefault - MaxZoomInFovDefault) * ZoomDefault + MaxZoomInFovDefault);
		SetGlobalSmoothing(SmoothingStrength.VeryMild);
	}

	/// <inheritdoc />
	public void Progress(float deltaTime) {
		_panSetpoint.Progress(deltaTime);
		_tiltSetpoint.Progress(deltaTime);
		_zoomSetpoint.Progress(deltaTime);
		
		var viewDir = ZeroPanTiltDirection;
		viewDir *= (UpDirection % _panSetpoint.CurrentValue);
		var tiltRot = Direction.FromDualOrthogonalization(viewDir, UpDirection) % _tiltSetpoint.CurrentValue;
		Camera.SetPosition(Position);
		Camera.SetViewAndUpDirection(viewDir * tiltRot, UpDirection * tiltRot);
		Camera.SetVerticalFieldOfView(_zoomSetpoint.CurrentValue);
	}

	/// <summary>
	/// Adjusts <see cref="Pan"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Pan"/> by per second, in degrees.</param>
	public void AdjustPan(float deltaTime, Angle adjustmentPerSec) => Pan += adjustmentPerSec * deltaTime;

	/// <summary>
	/// The default amount <see cref="Pan"/> is adjusted by for each pixel the mouse cursor moves: <c>0.04f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPanViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPanSensitivityMouseCursor = 0.04f;
	/// <summary>
	/// Adjusts <see cref="Pan"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How much to adjust <see cref="Pan"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultPanSensitivityMouseCursor"/> (<c>0.04f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Pan += delta * (adjustmentPerPixel ?? DefaultPanSensitivityMouseCursor);
	}
	/// <summary>
	/// The default amount <see cref="Pan"/> is adjusted by for each notch the mouse wheel is turned: <c>5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPanViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPanSensitivityMouseWheel = 5f;
	/// <summary>
	/// Adjusts <see cref="Pan"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How much to adjust <see cref="Pan"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultPanSensitivityMouseWheel"/> (<c>5f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Pan += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPanSensitivityMouseWheel) * (invertMouseControl ? -1f: 1f);
	}

	/// <summary>
	/// The default amount <see cref="Pan"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPanViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPanSensitivityControllerStick = 120f;
	/// <summary>
	/// Adjusts <see cref="Pan"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Pan"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPanSensitivityControllerStick"/> (<c>120f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.X"/> (left/right).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Pan += (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerStick) * delta;
	}
	
	/// <summary>
	/// The default amount <see cref="Pan"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPanViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPanSensitivityControllerTrigger = 120f;
	/// <summary>
	/// Adjusts <see cref="Pan"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Pan"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultPanSensitivityControllerTrigger"/> (<c>120f</c>) is used.</param>
	/// <param name="leftTriggerPansAnticlockwise">If <see langword="true"/> (the default), the left trigger pans anticlockwise and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerPansAnticlockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var anticlockwiseTriggerPosition = leftTriggerPansAnticlockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var clockwiseTriggerPosition = leftTriggerPansAnticlockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPan(deltaTime, anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerTrigger) 
			- clockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerTrigger));
	}
	/// <summary>
	/// The default amount <see cref="Pan"/> is adjusted by per second whilst the chosen key or button is held down: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustPanViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultPanSensitivityKeyOrButtonPress = 120f;
	/// <summary>
	/// Adjusts <see cref="Pan"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pan"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPanSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPan(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPanSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Pan"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Pan"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultPanSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustPanViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPan(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPanSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="Tilt"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Tilt"/> by per second, in degrees.</param>
	public void AdjustTilt(float deltaTime, Angle adjustmentPerSec) => Tilt += adjustmentPerSec * deltaTime;
	
	/// <summary>
	/// The default amount <see cref="Tilt"/> is adjusted by for each pixel the mouse cursor moves: <c>0.04f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustTiltViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultTiltSensitivityMouseCursor = 0.04f;
	/// <summary>
	/// Adjusts <see cref="Tilt"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How much to adjust <see cref="Tilt"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultTiltSensitivityMouseCursor"/> (<c>0.04f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Tilt += delta * (adjustmentPerPixel ?? DefaultTiltSensitivityMouseCursor);
	}
	
	/// <summary>
	/// The default amount <see cref="Tilt"/> is adjusted by for each notch the mouse wheel is turned: <c>5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustTiltViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultTiltSensitivityMouseWheel = 5f;
	/// <summary>
	/// Adjusts <see cref="Tilt"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How much to adjust <see cref="Tilt"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultTiltSensitivityMouseWheel"/> (<c>5f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Tilt += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultTiltSensitivityMouseWheel) * (invertMouseControl ? 1f: -1f);
	}
	
	/// <summary>
	/// The default amount <see cref="Tilt"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>80f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustTiltViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultTiltSensitivityControllerStick = 80f;
	/// <summary>
	/// Adjusts <see cref="Tilt"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Tilt"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultTiltSensitivityControllerStick"/> (<c>80f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Tilt += (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerStick) * delta;
	}
	
	/// <summary>
	/// The default amount <see cref="Tilt"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustTiltViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultTiltSensitivityControllerTrigger = 120f;
	/// <summary>
	/// Adjusts <see cref="Tilt"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How much to adjust <see cref="Tilt"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultTiltSensitivityControllerTrigger"/> (<c>120f</c>) is used.</param>
	/// <param name="leftTriggerTiltsUpward">If <see langword="true"/> (the default), the left trigger tilts upward and the right trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerTiltsUpward = true) {
		ArgumentNullException.ThrowIfNull(input);
		var upwardTiltTriggerPosition = leftTriggerTiltsUpward ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var downwardTiltTriggerPosition = leftTriggerTiltsUpward ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustTilt(deltaTime, upwardTiltTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerTrigger)
			- downwardTiltTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerTrigger));
	}
	
	/// <summary>
	/// The default amount <see cref="Tilt"/> is adjusted by per second whilst the chosen key or button is held down: <c>120f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustTiltViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultTiltSensitivityKeyOrButtonPress = 120f;
	/// <summary>
	/// Adjusts <see cref="Tilt"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Tilt"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultTiltSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustTilt(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultTiltSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Tilt"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How much to adjust <see cref="Tilt"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultTiltSensitivityKeyOrButtonPress"/> (<c>120f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustTiltViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustTilt(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultTiltSensitivityKeyOrButtonPress));
	}

	/// <summary>
	/// Adjusts <see cref="Zoom"/> at a steady rate for the duration of a single frame.
	/// </summary>
	/// <remarks>
	/// The adjustment applied is <paramref name="adjustmentPerSec"/> multiplied by <paramref name="deltaTime"/>, so the rate of change stays the same regardless of frame rate.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Zoom"/> by per second, in units.</param>
	public void AdjustZoom(float deltaTime, float adjustmentPerSec) => Zoom += adjustmentPerSec * deltaTime;
	
	/// <summary>
	/// The default amount <see cref="Zoom"/> is adjusted by for each pixel the mouse cursor moves: <c>0.0002f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustZoomViaMouseCursor</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultZoomSensitivityMouseCursor = 0.0002f;
	/// <summary>
	/// Adjusts <see cref="Zoom"/> according to how far the mouse cursor moved this frame.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerPixel">How far to adjust <see cref="Zoom"/> by for each pixel the cursor moves along the chosen axis. If <see langword="null"/>, <see cref="DefaultZoomSensitivityMouseCursor"/> (<c>0.0002f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Zoom += delta * (adjustmentPerPixel ?? DefaultZoomSensitivityMouseCursor);
	}
	
	/// <summary>
	/// The default amount <see cref="Zoom"/> is adjusted by for each notch the mouse wheel is turned: <c>0.025f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustZoomViaMouseWheel</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultZoomSensitivityMouseWheel = 0.025f;
	/// <summary>
	/// Adjusts <see cref="Zoom"/> according to how far the mouse wheel was turned this frame.
	/// </summary>
	/// <remarks>
	/// Wheel movement is reported in whole notches, so this adjusts in discrete steps rather than continuously.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="adjustmentPerWheelIncrement">How far to adjust <see cref="Zoom"/> by for each notch the wheel is turned. If <see langword="null"/>, <see cref="DefaultZoomSensitivityMouseWheel"/> (<c>0.025f</c>) is used.</param>
	/// <param name="invertMouseControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Zoom += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultZoomSensitivityMouseWheel) * (invertMouseControl ? 1f: -1f);
	}
		
	/// <summary>
	/// The default amount <see cref="Zoom"/> is adjusted by per second whilst a game controller stick is fully displaced: <c>0.33f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustZoomViaControllerStick</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultZoomSensitivityControllerStick = 0.33f;
	/// <summary>
	/// Adjusts <see cref="Zoom"/> according to how far a game controller stick is currently displaced.
	/// </summary>
	/// <remarks>
	/// The stick's deadzone is respected, so a stick resting at centre produces no adjustment.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Zoom"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultZoomSensitivityControllerStick"/> (<c>0.33f</c>) is used.</param>
	/// <param name="useLeftStick">If <see langword="true"/>, the left stick is read; otherwise the right stick is read. Defaults to the right stick.</param>
	/// <param name="invertStickControl">If <see langword="true"/>, the adjustment is applied in the opposite direction.</param>
	/// <param name="axis">Which axis of movement to read. Defaults to <see cref="Axis2D.Y"/> (up/down).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Zoom += (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerStick) * delta;
	}
	
	/// <summary>
	/// The default amount <see cref="Zoom"/> is adjusted by per second whilst a game controller trigger is fully depressed: <c>0.5f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustZoomViaControllerTriggers</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultZoomSensitivityControllerTrigger = 0.5f;
	/// <summary>
	/// Adjusts <see cref="Zoom"/> according to how far the game controller's triggers are currently depressed, with each trigger driving one direction.
	/// </summary>
	/// <remarks>
	/// Each trigger's deadzone is respected, so triggers at rest produce no adjustment. Because the two triggers are read independently, holding both at once cancels out.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="maxAdjustmentPerSec">How far to adjust <see cref="Zoom"/> by per second when the stick is fully displaced. If <see langword="null"/>, <see cref="DefaultZoomSensitivityControllerTrigger"/> (<c>0.5f</c>) is used.</param>
	/// <param name="rightTriggerZoomsIn">If <see langword="true"/> (the default), the right trigger zooms in and the left trigger does the opposite; if <see langword="false"/>, the two triggers are swapped.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool rightTriggerZoomsIn = true) {
		ArgumentNullException.ThrowIfNull(input);
		var zoomInTriggerPosition = rightTriggerZoomsIn ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var zoomOutTriggerPosition = rightTriggerZoomsIn ? input.LeftTriggerPosition : input.RightTriggerPosition;
		AdjustZoom(deltaTime, zoomInTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerTrigger)
			- zoomOutTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerTrigger));
	}
	
	/// <summary>
	/// The default amount <see cref="Zoom"/> is adjusted by per second whilst the chosen key or button is held down: <c>0.33f</c>.
	/// </summary>
	/// <remarks>
	/// Used by the <c>AdjustZoomViaKeyPress and ViaButtonPress</c> methods whenever no explicit sensitivity is supplied.
	/// </remarks>
	public const float DefaultZoomSensitivityKeyOrButtonPress = 0.33f;
	/// <summary>
	/// Adjusts <see cref="Zoom"/> for as long as a given key is held down.
	/// </summary>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="keyToTestFor">The key which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Zoom"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultZoomSensitivityKeyOrButtonPress"/> (<c>0.33f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustZoom(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultZoomSensitivityKeyOrButtonPress));
	}
	/// <summary>
	/// Adjusts <see cref="Zoom"/> for as long as a given game controller button is held down.
	/// </summary>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="buttonToTestFor">The button which, whilst held down, applies the adjustment.</param>
	/// <param name="reverse">If <see langword="true"/>, the adjustment is applied in the opposite direction. This lets a pair of opposing keys or buttons be set up by calling this method twice, once with <see langword="false"/> and once with <see langword="true"/>.</param>
	/// <param name="adjustmentPerSec">How far to adjust <see cref="Zoom"/> by for each second the key or button is held down. If <see langword="null"/>, <see cref="DefaultZoomSensitivityKeyOrButtonPress"/> (<c>0.33f</c>) is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustZoomViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustZoom(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultZoomSensitivityKeyOrButtonPress));
	}
	
	/// <summary>
	/// Adjusts every parameter of this controller according to its default keyboard and mouse scheme.
	/// </summary>
	/// <remarks>
	/// Moving the mouse pans and tilts the camera, and the scroll wheel zooms in and out. Call the individual <c>Adjust</c> methods yourself if you want a different
	/// mapping.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPanControl">If <see langword="true"/>, reverses which way horizontal mouse movement pans the camera.</param>
	/// <param name="invertTiltControl">If <see langword="true"/>, reverses which way vertical mouse movement tilts the camera.</param>
	/// <param name="invertZoomControl">If <see langword="true"/>, reverses which way the scroll wheel zooms.</param>
	/// <param name="panAdjustmentPerPixel">How much to adjust <see cref="Pan"/> by for each pixel of horizontal cursor movement. If <see langword="null"/>, <see cref="DefaultPanSensitivityMouseCursor"/> is used.</param>
	/// <param name="tiltAdjustmentPerPixel">How much to adjust <see cref="Tilt"/> by for each pixel of vertical cursor movement. If <see langword="null"/>, <see cref="DefaultTiltSensitivityMouseCursor"/> is used.</param>
	/// <param name="zoomAdjustmentPerWheelIncrement">How far to adjust <see cref="Zoom"/> by per wheel notch. If <see langword="null"/>, <see cref="DefaultZoomSensitivityMouseWheel"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? panAdjustmentPerPixel = null, Angle? tiltAdjustmentPerPixel = null, float? zoomAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPanViaMouseCursor(input, panAdjustmentPerPixel, invertMouseControl: invertPanControl);
		AdjustTiltViaMouseCursor(input, tiltAdjustmentPerPixel, invertMouseControl: invertTiltControl);
		AdjustZoomViaMouseWheel(input, zoomAdjustmentPerWheelIncrement, invertMouseControl: invertZoomControl);
	}
	
	/// <summary>
	/// Adjusts every parameter of this controller according to its default game controller scheme.
	/// </summary>
	/// <remarks>
	/// The right stick pans and tilts the camera, and the two triggers zoom in and out. Call the individual <c>Adjust</c> methods yourself if you want a different
	/// mapping.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <param name="invertPanControl">If <see langword="true"/>, reverses which way the stick pans the camera.</param>
	/// <param name="invertTiltControl">If <see langword="true"/>, reverses which way the stick tilts the camera.</param>
	/// <param name="invertZoomControl">If <see langword="true"/>, swaps which trigger zooms in and which zooms out.</param>
	/// <param name="maxPanAdjustmentPerSec">How much to adjust <see cref="Pan"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultPanSensitivityControllerStick"/> is used.</param>
	/// <param name="maxTiltAdjustmentPerSec">How much to adjust <see cref="Tilt"/> by per second at full stick displacement. If <see langword="null"/>, <see cref="DefaultTiltSensitivityControllerStick"/> is used.</param>
	/// <param name="maxZoomAdjustmentPerSec">How far to adjust <see cref="Zoom"/> by per second at full trigger depression. If <see langword="null"/>, <see cref="DefaultZoomSensitivityControllerTrigger"/> is used.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? maxPanAdjustmentPerSec = null, Angle? maxTiltAdjustmentPerSec = null, float? maxZoomAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPanViaControllerStick(input, deltaTime, maxPanAdjustmentPerSec, invertStickControl: invertPanControl);
		AdjustTiltViaControllerStick(input, deltaTime, maxTiltAdjustmentPerSec, invertStickControl: invertTiltControl);
		AdjustZoomViaControllerTriggers(input, deltaTime, maxZoomAdjustmentPerSec, rightTriggerZoomsIn: !invertZoomControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
