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
	
	public Direction WorldUp {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
			_forwardDir = value.AnyOrthogonal();
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
		WorldUp = Direction.Up;
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
		
		var currentHorizontalPlaneDir = _forwardDir * (_yawSetpoint.CurrentValue % WorldUp);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(WorldUp, currentHorizontalPlaneDir);
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(currentHorizontalPlaneDir * verticalTiltRot, WorldUp * verticalTiltRot);
	}

	public void AdjustPitch(float deltaTime, Angle adjustmentPerSec) => Pitch += adjustmentPerSec * deltaTime;

	public const float DefaultPitchSensitivityMouseCursor = 0.02f;
	public void AdjustPitchViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Pitch += delta * (adjustmentPerPixel ?? DefaultPitchSensitivityMouseCursor);
	}

	public const float DefaultPitchSensitivityMouseWheel = 5f;
	public void AdjustPitchViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Pitch += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPitchSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultPitchSensitivityControllerStick = 120f;
	public void AdjustPitchViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Pitch += (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerStick) * delta;
	}

	public const float DefaultPitchSensitivityControllerTrigger = 120f;
	public void AdjustPitchViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerPitchesUp = true) {
		ArgumentNullException.ThrowIfNull(input);
		var pitchUpTriggerPosition = leftTriggerPitchesUp ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var pitchDownTriggerPosition = leftTriggerPitchesUp ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPitch(deltaTime, pitchUpTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger)
			- pitchDownTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger));
	}

	public const float DefaultPitchSensitivityKeyOrButtonPress = 80f;
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPitchSensitivityKeyOrButtonPress));
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPitch(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPitchSensitivityKeyOrButtonPress));
	}

	public void AdjustYaw(float deltaTime, Angle adjustmentPerSec) => Yaw += adjustmentPerSec * deltaTime;

	public const float DefaultYawSensitivityMouseCursor = 0.02f;
	public void AdjustYawViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Yaw += delta * (adjustmentPerPixel ?? DefaultYawSensitivityMouseCursor);
	}

	public const float DefaultYawSensitivityMouseWheel = 5f;
	public void AdjustYawViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Yaw += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultYawSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	public const float DefaultYawSensitivityControllerStick = 120f;
	public void AdjustYawViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Yaw += (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerStick) * delta;
	}

	public const float DefaultYawSensitivityControllerTrigger = 120f;
	public void AdjustYawViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerYawsLeft = true) {
		ArgumentNullException.ThrowIfNull(input);
		var yawLeftTriggerPosition = leftTriggerYawsLeft ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var yawRightTriggerPosition = leftTriggerYawsLeft ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustYaw(deltaTime, yawLeftTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger)
			- yawRightTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger));
	}

	public const float DefaultYawSensitivityKeyOrButtonPress = 120f;
	public void AdjustYawViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustYaw(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultYawSensitivityKeyOrButtonPress));
	}
	public void AdjustYawViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultYawSensitivityKeyOrButtonPress));
	}

	public void AdjustPosition(Angle polarOrientation, float distance) {
		var zeroDegreeDir = Camera.GetRelativeOrientationDirection(Orientation.Right).OrthogonalizedAgainst(WorldUp)
			?? Direction.FromDualOrthogonalization(Camera.ViewDirection, WorldUp);

		Position += (zeroDegreeDir * (polarOrientation % WorldUp)) * distance;
	}
	public void AdjustPosition(Angle polarOrientation, float moveSpeed, float deltaTime) {
		AdjustPosition(polarOrientation, moveSpeed * deltaTime);
	}
	public void AdjustPosition(Orientation2D orientation, float distance) {
		AdjustPosition(Angle.From2DPolarAngle(orientation) ?? Angle.Zero, distance);
	}
	public void AdjustPosition(Orientation2D orientation, float moveSpeed, float deltaTime) {
		AdjustPosition(orientation, moveSpeed * deltaTime);
	}

	public const float DefaultPositionSensitivityMouseCursor = 0.0002f;
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? distancePerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		AdjustPosition(axis == Axis2D.X ? Orientation2D.Right : Orientation2D.Up, (distancePerPixel ?? DefaultPositionSensitivityMouseCursor) * delta);
	}

	public const float DefaultPositionSensitivityMouseWheel = 0.05f;
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? distancePerWheelIncrement = null, Orientation2D positiveOrientation = Orientation2D.Up, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPosition(positiveOrientation, (distancePerWheelIncrement ?? DefaultPositionSensitivityMouseWheel) * input.MouseScrollWheelDelta * (invertMouseControl ? -1f : 1f));
	}

	public const float DefaultPositionSensitivityControllerStick = 0.5f;
	public void AdjustPositionViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxSpeed = null, bool useLeftStick = true) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var angle = stickPosition.GetPolarAngle();
		if (angle == null) return;
		AdjustPosition(angle.Value, (maxSpeed ?? DefaultPositionSensitivityControllerStick) * stickPosition.Displacement, deltaTime);
	}

	public const float DefaultPositionSensitivityControllerTrigger = 0.5f;
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxSpeed = null, bool leftTriggerMovesPositive = true, Orientation2D positiveOrientation = Orientation2D.Up) {
		ArgumentNullException.ThrowIfNull(input);
		var positiveTriggerPosition = leftTriggerMovesPositive ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var negativeTriggerPosition = leftTriggerMovesPositive ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var sensitivity = maxSpeed ?? DefaultPositionSensitivityControllerTrigger;
		AdjustPosition(positiveOrientation, positiveTriggerPosition.GetDisplacementWithDeadzone() * sensitivity - negativeTriggerPosition.GetDisplacementWithDeadzone() * sensitivity, deltaTime);
	}

	public const float DefaultPositionSensitivityKeyOrButtonPress = 0.5f;
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Orientation2D orientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPosition(orientation, speed ?? DefaultPositionSensitivityKeyOrButtonPress, deltaTime);
	}
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Orientation2D orientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPosition(orientation, speed ?? DefaultPositionSensitivityKeyOrButtonPress, deltaTime);
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? moveSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);

		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation2D.Left, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation2D.Right, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation2D.Up, moveSpeed);
		AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation2D.Down, moveSpeed);
	}

	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxMoveSpeed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);

		AdjustPositionViaControllerStick(input, deltaTime, maxMoveSpeed);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
