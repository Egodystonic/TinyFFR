// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class FirstPersonCameraController : ICameraController<FirstPersonCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<FirstPersonCameraController> _controllerPool = new(&New);
	static FirstPersonCameraController New() => new();
	static FirstPersonCameraController ICameraController<FirstPersonCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FirstPersonCameraController));
	FirstPersonCameraController() { }
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

	public Strength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	public Strength RotationSmoothingStrength {
		get => _rotationSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set {
			_yawSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
			_pitchSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
		}
	}
	
	public Plane GroundPlane {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
			_forwardDir = value.Normal.AnyOrthogonal();
		}
	}
	
	public Location Position {
		get => _positionSetpoint.TargetValue.AsLocation();
		set {
			if (!value.IsPhysicallyValid) return;
			_positionSetpoint.TargetValue = value.AsVect();
		}
	}
	public Angle Yaw {
		get => _yawSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_yawSetpoint.TargetValue = value;
		}
	}
	public Angle Pitch {
		get => _pitchSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_pitchSetpoint.TargetValue = value;
		}
	}

	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomRotationSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		RotationSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		GroundPlane = new Plane(Direction.Up);
		_positionSetpoint.Reset(Vect.Zero);
		_yawSetpoint.Reset(Angle.Zero);
		_pitchSetpoint.Reset(Angle.Zero);
		SetGlobalSmoothing(Strength.VeryMild);
	}

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
		
		var currentHorizontalPlaneDir = _forwardDir * (_yawSetpoint.CurrentValue % GroundPlane.Normal);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(GroundPlane.Normal, currentHorizontalPlaneDir);
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(currentHorizontalPlaneDir * verticalTiltRot, GroundPlane.Normal * verticalTiltRot);
	}

	public void AdjustPitch(Angle adjustmentPerSec, float deltaTime) {
		Pitch += adjustmentPerSec * deltaTime;
	}
	public void AdjustPitchViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Pitch += delta * adjustmentPerPixel;
	}
	public void AdjustPitchViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Pitch += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustPitchViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Pitch += maxAdjustmentPerSec * delta;
	}
	public void AdjustPitchViaControllerTriggers(GameControllerTriggerPosition pitchUpTriggerPosition, GameControllerTriggerPosition pitchDownTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		AdjustPitch(pitchUpTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - pitchDownTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(adjustmentPerSec, deltaTime);
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPitch(adjustmentPerSec, deltaTime);
	}
	
	public void AdjustYaw(Angle adjustmentPerSec, float deltaTime) {
		Yaw += adjustmentPerSec * deltaTime;
	}
	public void AdjustYawViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Yaw += delta * adjustmentPerPixel;
	}
	public void AdjustYawViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Yaw += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustYawViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Yaw += maxAdjustmentPerSec * delta;
	}
	public void AdjustYawViaControllerTriggers(GameControllerTriggerPosition yawLeftTriggerPosition, GameControllerTriggerPosition yawRightTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		AdjustYaw(yawLeftTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - yawRightTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustYawViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	public void AdjustYawViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	
	public void Move(Angle polarOrientation, float distance) { 
		var zeroDegreeDir = Camera.GetRelativeOrientationDirection(Orientation.Right).ParallelizedWith(GroundPlane) 
			?? Direction.FromDualOrthogonalization(Camera.ViewDirection, GroundPlane.Normal);
		
		Position += (zeroDegreeDir * (polarOrientation % GroundPlane.Normal)) * distance;
	}
	public void Move(Angle polarOrientation, float moveSpeed, float deltaTime) { 
		Move(polarOrientation, moveSpeed * deltaTime);
	}
	public void Move(Orientation2D orientation, float distance) { 
		Move(Angle.From2DPolarAngle(orientation) ?? Angle.Zero, distance);
	}
	public void Move(Orientation2D orientation, float moveSpeed, float deltaTime) { 
		Move(orientation, moveSpeed * deltaTime);
	}
	public void MoveViaMouseCursor(XYPair<int> cursorDelta, float distancePerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Move(axis == Axis2D.X ? Orientation2D.Right : Orientation2D.Up, distancePerPixel * delta);
	}
	public void MoveViaMouseWheel(int mouseWheelDelta, float distancePerDelta, Orientation2D positiveOrientation, bool invertMouseControl = false) {
		Move(positiveOrientation, distancePerDelta * mouseWheelDelta * (invertMouseControl ? -1f: 1f));
	}
	public void MoveViaControllerStick(GameControllerStickPosition stickPosition, float maxSpeed, float deltaTime) {
		var angle = stickPosition.GetPolarAngle();
		if (angle == null) return;
		
		Move(angle.Value, maxSpeed * stickPosition.Displacement, deltaTime);
	}
	public void MoveViaControllerTriggers(GameControllerTriggerPosition positiveTriggerPosition, GameControllerTriggerPosition negativeTriggerPosition, Orientation2D positiveOrientation, float maxSpeed, float deltaTime) {
		Move(positiveOrientation, positiveTriggerPosition.GetDisplacementWithDeadzone() * maxSpeed - negativeTriggerPosition.GetDisplacementWithDeadzone() * maxSpeed, deltaTime);
	}
	public void MoveViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Orientation2D orientation, float speed, float deltaTime) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Move(orientation, speed, deltaTime);
	}
	public void MoveViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Orientation2D orientation, float speed, float deltaTime) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Move(orientation, speed, deltaTime);
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? moveSpeed = null) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		AdjustPitchViaMouseCursor(kbmInput.MouseCursorDelta, pitchAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(kbmInput.MouseCursorDelta, yawAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertYawControl);
		
		var speed = moveSpeed ?? 0.5f;
		MoveViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowLeft, Orientation2D.Left, speed, deltaTime);
		MoveViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowRight, Orientation2D.Right, speed, deltaTime);
		MoveViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowUp, Orientation2D.Up, speed, deltaTime);
		MoveViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowDown, Orientation2D.Down, speed, deltaTime);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxMoveSpeed = null) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		AdjustPitchViaControllerStick(controllerInput.RightStickPosition, maxPitchAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(controllerInput.RightStickPosition, maxYawAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertYawControl);
		
		var maxSpeed = maxMoveSpeed ?? 0.5f;
		MoveViaControllerStick(controllerInput.LeftStickPosition, maxSpeed, deltaTime);
	}
}
