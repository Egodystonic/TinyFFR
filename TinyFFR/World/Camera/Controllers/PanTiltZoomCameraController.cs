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
	public const float DefaultHighestZoomFov = 15f;
	public const float DefaultLowestZoomFov = 125f;
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
	public Angle HighestZoomFov {
		get; 
		set {
			if (!value.Radians.IsPositiveAndFinite()) return;
			field = value;
			if (value > LowestZoomFov) LowestZoomFov = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Zoom = Zoom;
#pragma warning restore CA2245
		}
	}
	public Angle LowestZoomFov {
		get; 
		set {
			if (!value.Radians.IsPositiveAndFinite()) return;
			field = value;
			if (value < HighestZoomFov) HighestZoomFov = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Zoom = Zoom;
#pragma warning restore CA2245
		}
	}
	
	public Angle Pan {
		get => _panSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
			// if (value.Absolute > PanRange) {
			// 	value = MathF.Sign(value.Radians) * PanRange.Value;
			// }
			_panSetpoint.TargetValue = value;
		}
	}
	public Angle Tilt {
		get => _tiltSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
			value = value.Clamp(-MaxTiltUp, MaxTiltDown);
			_tiltSetpoint.TargetValue = value;
		}
	}
	public float Zoom {
		get => _zoomSetpoint.TargetValue.RemapRange(new Pair<Angle, Angle>(HighestZoomFov, LowestZoomFov), new Pair<Angle, Angle>(Angle.FromRadians(0f), Angle.FromRadians(1f))).Radians;
		set {
			if (!Single.IsFinite(value)) return;
			_zoomSetpoint.TargetValue = Angle.FromRadians(((Real) value).Clamp(0f, 1f).RemapRange(new Pair<Real, Real>(0f, 1f), new Pair<Real, Real>(HighestZoomFov.Radians, LowestZoomFov.Radians)));
		}
	}
	public Direction ZeroPanTiltDirection {
		get;
		set {
			if (value == Direction.None) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-orthogonalization
			UpDirection = UpDirection;
#pragma warning restore CA2245
		}
	}
	public Direction UpDirection {
		get;
		set {
			field = value.OrthogonalizedAgainst(ZeroPanTiltDirection) ?? Direction.None;
			if (field == Direction.None) field = ZeroPanTiltDirection.AnyOrthogonal();
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
		HighestZoomFov = DefaultHighestZoomFov;
		LowestZoomFov = DefaultLowestZoomFov;
		ZeroPanTiltDirection = Direction.Forward;
		UpDirection = Direction.Up;
		Position = Location.Origin;
		_panSetpoint.Reset(Angle.Zero);
		_tiltSetpoint.Reset(Angle.Zero);
		_zoomSetpoint.Reset((DefaultLowestZoomFov - DefaultHighestZoomFov) * 0.5f + DefaultHighestZoomFov);
		SetGlobalSmoothing(Strength.Standard);
	}

	public void Progress(float deltaTime) {
		_panSetpoint.Progress(deltaTime);
		_tiltSetpoint.Progress(deltaTime);
		_zoomSetpoint.Progress(deltaTime);
		
		var sphericalTranslation = new SphericalTranslation(_panSetpoint.CurrentValue, _tiltSetpoint.CurrentValue);
		Camera.SetPosition(Position);
		Camera.SetViewAndUpDirection(sphericalTranslation.Translate(ZeroPanTiltDirection, UpDirection), UpDirection);
		Camera.SetVerticalFieldOfView(_zoomSetpoint.CurrentValue);	
	}

	public void AdjustPanViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Pan += delta * adjustmentPerPixel;
	}
	public void AdjustPanViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Pan += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustPanViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Pan += maxAdjustmentPerSec * delta;
	}
	public void AdjustPanViaControllerTriggers(GameControllerTriggerPosition anticlockwiseTriggerPosition, GameControllerTriggerPosition clockwiseTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		Pan += deltaTime 
			* (anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - clockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustPanViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Pan += adjustmentPerSec * deltaTime;
	}
	public void AdjustPanViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Pan += adjustmentPerSec * deltaTime;
	}

	public void AdjustTiltViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Tilt += delta * adjustmentPerPixel;
	}
	public void AdjustTiltViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Tilt += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustTiltViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Tilt += maxAdjustmentPerSec * delta;
	}
	public void AdjustTiltViaControllerTriggers(GameControllerTriggerPosition anticlockwiseTriggerPosition, GameControllerTriggerPosition clockwiseTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		Tilt += deltaTime 
			* (anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - clockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustTiltViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Tilt += adjustmentPerSec * deltaTime;
	}
	public void AdjustTiltViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Tilt += adjustmentPerSec * deltaTime;
	}

	public void AdjustZoomViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Zoom += delta * adjustmentPerPixel;
	}
	public void AdjustZoomViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Zoom += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustZoomViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Zoom += maxAdjustmentPerSec * delta;
	}
	public void AdjustZoomViaControllerTriggers(GameControllerTriggerPosition anticlockwiseTriggerPosition, GameControllerTriggerPosition clockwiseTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		Zoom += deltaTime 
			* (anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - clockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustZoomViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Zoom += adjustmentPerSec * deltaTime;
	}
	public void AdjustZoomViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Zoom += adjustmentPerSec * deltaTime;
	}
	
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? panAdjustmentPerPixel = null, Angle? tiltAdjustmentPerPixel = null, float? zoomAdjustmentPerWheelIncrement = null) {
		AdjustPanViaMouseCursor(kbmInput.MouseCursorDelta, panAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertPanControl);
		AdjustTiltViaMouseCursor(kbmInput.MouseCursorDelta, tiltAdjustmentPerPixel ?? 0.0001f, invertMouseControl: invertTiltControl);
		AdjustZoomViaMouseWheel(kbmInput.MouseScrollWheelDelta,  zoomAdjustmentPerWheelIncrement ?? 0.045f, invertMouseControl: invertZoomControl);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? maxPanAdjustmentPerSec = null, Angle? maxTiltAdjustmentPerSec = null, float? maxZoomAdjustmentPerSec = null) {
		AdjustPanViaControllerStick(controllerInput.LeftStickPosition, maxPanAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertPanControl);
		AdjustTiltViaControllerStick(controllerInput.LeftStickPosition, maxTiltAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertTiltControl);
		AdjustZoomViaControllerTriggers(invertZoomControl ? controllerInput.RightTriggerPosition : controllerInput.LeftTriggerPosition, invertZoomControl ? controllerInput.LeftTriggerPosition : controllerInput.RightTriggerPosition, maxZoomAdjustmentPerSec ?? 0.5f, deltaTime);
	}
}
