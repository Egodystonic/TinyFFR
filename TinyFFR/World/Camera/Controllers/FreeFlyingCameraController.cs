// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class FreeFlyingCameraController : ICameraController<FreeFlyingCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<FreeFlyingCameraController> _controllerPool = new(&New);
	static FreeFlyingCameraController New() => new();
	static FreeFlyingCameraController ICameraController<FreeFlyingCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FreeFlyingCameraController));
	FreeFlyingCameraController() { }
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		_controllerPool.Return(this);
	}
	#endregion

	readonly Spring3DBasedCameraSetpoint _positionSetpoint = new();
	readonly CameraEffectStrengthMap _positionSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.4f,
		VeryStrong: 0.65f
	);
	readonly SpringRotationBasedCameraSetpoint _rotationSetpoint = new();
	readonly CameraEffectStrengthMap _rotationSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.4f,
		VeryStrong: 0.65f
	);
	
	Angle _selfRightingPerSec;
	readonly CameraEffectStrengthMap _selfRightingStrengthMap = new(
		None: 0f,
		VeryMild: 5f,
		Mild: 10f,
		Standard: 20f,
		Strong: 30f,
		VeryStrong: 45f
	);

	public Strength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	public Strength RotationSmoothingStrength {
		get {
			return _rotationSmoothingStrengthMap.From(_rotationSetpoint.HalfLife);
		}
		set => _rotationSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
	}

	public Strength SelfRightingStrength {
		get => _selfRightingStrengthMap.From(_selfRightingPerSec.Degrees);
		set => _selfRightingPerSec = _selfRightingStrengthMap.From(value);
	}
	
	public Direction WorldForward {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			WorldUp = WorldUp;
#pragma warning restore CA2245
		}
	}
	public Direction WorldUp {
		get;
		set {
			field = value.OrthogonalizedAgainst(WorldForward) ?? Direction.None;
			if (!field.IsPhysicallyValid) field = WorldForward.AnyOrthogonal();
		}
	}
	
	public Location Position {
		get => _positionSetpoint.TargetValue.AsLocation();
		set {
			if (!value.IsPhysicallyValid) return;
			_positionSetpoint.TargetValue = value.AsVect();
		}
	}
	public Rotation Rotation {
		get => _rotationSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_rotationSetpoint.TargetValue = value;
		}
	}

	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomRotationSmoothingStrength(float smoothingHalfLife) {
		_rotationSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetSelfRightingStrength(Angle selfRightingPerSec) {
		_selfRightingPerSec = selfRightingPerSec.ClampZeroToHalfCircle();
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		RotationSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		WorldForward = Direction.Forward;
		WorldUp = Direction.Up;
		_positionSetpoint.Reset(Vect.Zero);
		_rotationSetpoint.Reset(Rotation.None);
		_selfRightingPerSec = Angle.Zero;
		SetGlobalSmoothing(Strength.VeryMild);
	}

	public void Progress(float deltaTime) {
		_positionSetpoint.Progress(deltaTime);
		_rotationSetpoint.Progress(deltaTime);
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(WorldForward * _rotationSetpoint.CurrentValue, WorldUp);
		
		// if (_selfRightingPerSec > Angle.Zero) { // TODO don't do min, actually just skip this when our change is greater than the measured angle
		// 	var orthoUp = WorldUp.OrthogonalizedAgainst(_rotationSetpoint.CurrentValue * WorldForward) ?? WorldUp; 
		// 	var targetUp = _rotationSetpoint.TargetValue * orthoUp;
		// 	var targetToWorld = targetUp >> WorldUp;
		// 	var targetToWorldForThisTick = targetToWorld with { Angle = _selfRightingPerSec * deltaTime };
		// 	Console.WriteLine(targetToWorldForThisTick.Angle + " < " + targetToWorld.Angle);
		// 	if (targetToWorldForThisTick.Angle < targetToWorld.Angle) _rotationSetpoint.TargetValue += targetToWorldForThisTick;
		// }
	}

	public void AdjustPitch(Angle adjustment) {
		if (adjustment == Angle.Zero) return;
		_rotationSetpoint.TargetValue += adjustment % Direction.FromDualOrthogonalization(Camera.ViewDirection, Camera.UpDirection);
	}
	public void AdjustPitch(Angle adjustmentPerSec, float deltaTime) => AdjustPitch(adjustmentPerSec * deltaTime);
	public void AdjustPitchViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		AdjustPitch(delta * adjustmentPerPixel);
	}
	public void AdjustPitchViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		AdjustPitch(mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f));
	}
	public void AdjustPitchViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		AdjustPitch(maxAdjustmentPerSec * delta);
	}
	public void AdjustPitchViaControllerTriggers(GameControllerTriggerPosition pitchUpTriggerPosition, GameControllerTriggerPosition pitchDownTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		AdjustPitch(pitchUpTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - pitchDownTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(adjustmentPerSec, deltaTime);
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPitch(adjustmentPerSec, deltaTime);
	}
	
	public void AdjustYaw(Angle adjustment) {
		if (adjustment == Angle.Zero) return;
		_rotationSetpoint.TargetValue += adjustment % Camera.UpDirection;
	}
	public void AdjustYaw(Angle adjustmentPerSec, float deltaTime) => AdjustYaw(adjustmentPerSec * deltaTime);
	public void AdjustYawViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		AdjustYaw(delta * adjustmentPerPixel);
	}
	public void AdjustYawViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		AdjustYaw(mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f));
	}
	public void AdjustYawViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		AdjustYaw(maxAdjustmentPerSec * delta);
	}
	public void AdjustYawViaControllerTriggers(GameControllerTriggerPosition pitchUpTriggerPosition, GameControllerTriggerPosition pitchDownTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		AdjustYaw(pitchUpTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - pitchDownTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustYawViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	public void AdjustYawViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	
	public void AdjustRoll(Angle adjustment) {
		if (adjustment == Angle.Zero) return;
		_rotationSetpoint.TargetValue += adjustment % Camera.ViewDirection;
	}
	public void AdjustRoll(Angle adjustmentPerSec, float deltaTime) => AdjustRoll(adjustmentPerSec * deltaTime);
	public void AdjustRollViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		AdjustRoll(delta * adjustmentPerPixel);
	}
	public void AdjustRollViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		AdjustRoll(mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f));
	}
	public void AdjustRollViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		AdjustRoll(maxAdjustmentPerSec * delta);
	}
	public void AdjustRollViaControllerTriggers(GameControllerTriggerPosition pitchUpTriggerPosition, GameControllerTriggerPosition pitchDownTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		AdjustRoll(pitchUpTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - pitchDownTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustRollViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustRoll(adjustmentPerSec, deltaTime);
	}
	public void AdjustRollViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustRoll(adjustmentPerSec, deltaTime);
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? panAdjustmentPerPixel = null, Angle? tiltAdjustmentPerPixel = null, float? zoomAdjustmentPerWheelIncrement = null) {
		AdjustPitchViaMouseCursor(kbmInput.MouseCursorDelta, panAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertPanControl);
		AdjustYawViaMouseCursor(kbmInput.MouseCursorDelta, tiltAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertTiltControl);
		AdjustRollViaKeyPress(kbmInput, KeyboardOrMouseKey.MouseLeft, 90f, deltaTime);
		AdjustRollViaKeyPress(kbmInput, KeyboardOrMouseKey.MouseRight, -90f, deltaTime);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? maxPanAdjustmentPerSec = null, Angle? maxTiltAdjustmentPerSec = null, float? maxZoomAdjustmentPerSec = null) {
		// AdjustPanViaControllerStick(controllerInput.LeftStickPosition, maxPanAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertPanControl);
		// AdjustTiltViaControllerStick(controllerInput.LeftStickPosition, maxTiltAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertTiltControl);
		// AdjustZoomViaControllerTriggers(invertZoomControl ? controllerInput.RightTriggerPosition : controllerInput.LeftTriggerPosition, invertZoomControl ? controllerInput.LeftTriggerPosition : controllerInput.RightTriggerPosition, maxZoomAdjustmentPerSec ?? 0.5f, deltaTime);
	}
}
