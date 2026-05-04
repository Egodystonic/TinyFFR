// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class PanTiltZoomCameraController : ICameraController<PanTiltZoomCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<PanTiltZoomCameraController> _controllerPool = new(&New);
	static PanTiltZoomCameraController New() => new();
	static PanTiltZoomCameraController ICameraController<PanTiltZoomCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(PanTiltZoomCameraController));
	PanTiltZoomCameraController() { }
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		_controllerPool.Return(this);
	}
	#endregion

	public const float DefaultPanRangeDegrees = 160f;
	public const float DefaultMaxTiltUpDegrees = 35f;
	public const float DefaultMaxTiltDownDegrees = 55f;
	public const float DefaultMaxZoomInFov = 15f;
	public const float DefaultMaxZoomOutFov = 90f;
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

	public Strength PanSmoothingStrength {
		get => _panSmoothingStrengthMap.From(_panSetpoint.HalfLife);
		set => _panSetpoint.HalfLife = _panSmoothingStrengthMap.From(value);
	}
	public Strength TiltSmoothingStrength {
		get => _tiltSmoothingStrengthMap.From(_tiltSetpoint.HalfLife);
		set => _tiltSetpoint.HalfLife = _tiltSmoothingStrengthMap.From(value);
	}
	public Strength ZoomSmoothingStrength {
		get => _zoomSmoothingStrengthMap.From(_zoomSetpoint.HalfLife);
		set => _zoomSetpoint.HalfLife = _zoomSmoothingStrengthMap.From(value);
	}
	
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
	public float Zoom {
		get => _zoomSetpoint.TargetValue.RemapRange(new Pair<Angle, Angle>(MaxZoomOutFov, MaxZoomInFov), new Pair<Angle, Angle>(Angle.FromRadians(0f), Angle.FromRadians(1f))).Radians;
		set {
			if (!Single.IsFinite(value)) return;
			_zoomSetpoint.TargetValue = Angle.FromRadians(((Real) value).Clamp(0f, 1f).RemapRange(new Pair<Real, Real>(0f, 1f), new Pair<Real, Real>(MaxZoomOutFov.Radians, MaxZoomInFov.Radians)));
		}
	}
	public Direction ZeroPanTiltDirection {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
		}
	}
	public Direction UpDirection {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
		}
	}
	public Location Position { get; set; }

	public void SetCustomPanSmoothingStrength(float smoothingHalfLife) {
		_panSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomTiltSmoothingStrength(float smoothingHalfLife) {
		_tiltSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomZoomSmoothingStrength(float smoothingHalfLife) {
		_zoomSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PanSmoothingStrength = newSmoothingStrength;
		ZoomSmoothingStrength = newSmoothingStrength;
		TiltSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		PanRange = DefaultPanRangeDegrees;
		MaxTiltUp = DefaultMaxTiltUpDegrees;
		MaxTiltDown = DefaultMaxTiltDownDegrees;
		MaxZoomInFov = DefaultMaxZoomInFov;
		MaxZoomOutFov = DefaultMaxZoomOutFov;
		ZeroPanTiltDirection = Direction.Forward;
		UpDirection = Direction.Up;
		Position = Location.Origin;
		_panSetpoint.Reset(Angle.Zero);
		_tiltSetpoint.Reset(Angle.Zero);
		_zoomSetpoint.Reset((DefaultMaxZoomOutFov - DefaultMaxZoomInFov) * 0.5f + DefaultMaxZoomInFov);
		SetGlobalSmoothing(Strength.VeryMild);
	}

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

	public void AdjustPan(float deltaTime, Angle adjustmentPerSec) => Pan += adjustmentPerSec * deltaTime;

	public const float DefaultPanSensitivityMouseCursor = 0.02f;
	public void AdjustPanViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Pan += delta * (adjustmentPerPixel ?? DefaultPanSensitivityMouseCursor);
	}
	public const float DefaultPanSensitivityMouseWheel = 5f;
	public void AdjustPanViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Pan += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPanSensitivityMouseWheel) * (invertMouseControl ? -1f: 1f);
	}

	public const float DefaultPanSensitivityControllerStick = 120f;
	public void AdjustPanViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Pan += (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerStick) * delta;
	}
	
	public const float DefaultPanSensitivityControllerTrigger = 120f;
	public void AdjustPanViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerPansAnticlockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var anticlockwiseTriggerPosition = leftTriggerPansAnticlockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var clockwiseTriggerPosition = leftTriggerPansAnticlockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPan(deltaTime, anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerTrigger) 
			- clockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPanSensitivityControllerTrigger));
	}
	public const float DefaultPanSensitivityKeyOrButtonPress = 120f;
	public void AdjustPanViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPan(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPanSensitivityKeyOrButtonPress));
	}
	public void AdjustPanViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPan(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPanSensitivityKeyOrButtonPress));
	}

	public void AdjustTilt(float deltaTime, Angle adjustmentPerSec) => Tilt += adjustmentPerSec * deltaTime;
	
	public const float DefaultTiltSensitivityMouseCursor = 0.02f;
	public void AdjustTiltViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Tilt += delta * (adjustmentPerPixel ?? DefaultTiltSensitivityMouseCursor);
	}
	
	public const float DefaultTiltSensitivityMouseWheel = 5f;
	public void AdjustTiltViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Tilt += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultTiltSensitivityMouseWheel) * (invertMouseControl ? 1f: -1f);
	}
	
	public const float DefaultTiltSensitivityControllerStick = 80f;
	public void AdjustTiltViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Tilt += (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerStick) * delta;
	}
	
	public const float DefaultTiltSensitivityControllerTrigger = 120f;
	public void AdjustTiltViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerTiltsUpward = true) {
		ArgumentNullException.ThrowIfNull(input);
		var upwardTiltTriggerPosition = leftTriggerTiltsUpward ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var downwardTiltTriggerPosition = leftTriggerTiltsUpward ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustTilt(deltaTime, upwardTiltTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerTrigger)
			- downwardTiltTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultTiltSensitivityControllerTrigger));
	}
	
	public const float DefaultTiltSensitivityKeyOrButtonPress = 120f;
	public void AdjustTiltViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustTilt(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultTiltSensitivityKeyOrButtonPress));
	}
	public void AdjustTiltViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustTilt(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultTiltSensitivityKeyOrButtonPress));
	}

	public void AdjustZoom(float deltaTime, float adjustmentPerSec) => Zoom += adjustmentPerSec * deltaTime;
	
	public const float DefaultZoomSensitivityMouseCursor = 0.0001f;
	public void AdjustZoomViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Zoom += delta * (adjustmentPerPixel ?? DefaultZoomSensitivityMouseCursor);
	}
	
	public const float DefaultZoomSensitivityMouseWheel = 0.025f;
	public void AdjustZoomViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Zoom += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultZoomSensitivityMouseWheel) * (invertMouseControl ? 1f: -1f);
	}
		
	public const float DefaultZoomSensitivityControllerStick = 0.33f;
	public void AdjustZoomViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Zoom += (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerStick) * delta;
	}
	
	public const float DefaultZoomSensitivityControllerTrigger = 0.5f;
	public void AdjustZoomViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool rightTriggerZoomsIn = true) {
		ArgumentNullException.ThrowIfNull(input);
		var zoomInTriggerPosition = rightTriggerZoomsIn ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var zoomOutTriggerPosition = rightTriggerZoomsIn ? input.LeftTriggerPosition : input.RightTriggerPosition;
		AdjustZoom(deltaTime, zoomInTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerTrigger)
			- zoomOutTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultZoomSensitivityControllerTrigger));
	}
	
	public const float DefaultZoomSensitivityKeyOrButtonPress = 0.33f;
	public void AdjustZoomViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustZoom(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultZoomSensitivityKeyOrButtonPress));
	}
	public void AdjustZoomViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustZoom(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultZoomSensitivityKeyOrButtonPress));
	}
	
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? panAdjustmentPerPixel = null, Angle? tiltAdjustmentPerPixel = null, float? zoomAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPanViaMouseCursor(input, panAdjustmentPerPixel, invertMouseControl: invertPanControl);
		AdjustTiltViaMouseCursor(input, tiltAdjustmentPerPixel, invertMouseControl: invertTiltControl);
		AdjustZoomViaMouseWheel(input, zoomAdjustmentPerWheelIncrement, invertMouseControl: invertZoomControl);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? maxPanAdjustmentPerSec = null, Angle? maxTiltAdjustmentPerSec = null, float? maxZoomAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPanViaControllerStick(input, deltaTime, maxPanAdjustmentPerSec, invertStickControl: invertPanControl);
		AdjustTiltViaControllerStick(input, deltaTime, maxTiltAdjustmentPerSec, invertStickControl: invertTiltControl);
		AdjustZoomViaControllerTriggers(input, deltaTime, maxZoomAdjustmentPerSec, rightTriggerZoomsIn: !invertZoomControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
