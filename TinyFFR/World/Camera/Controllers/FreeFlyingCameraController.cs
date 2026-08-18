// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

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
	public Direction WorldUp {
		get;
		set {
			field = value.OrthogonalizedAgainst(WorldForward) ?? Direction.None;
			if (!field.IsPhysicallyValidAndNotNone) field = WorldForward.AnyOrthogonal();
		}
	}
	public bool AllowUpsideDownFlip { get; set; }
	
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
		WorldForward = Direction.Forward;
		WorldUp = Direction.Up;
		AllowUpsideDownFlip = false;
		_positionSetpoint.Reset(Vect.Zero);
		_yawSetpoint.Reset(Angle.Zero);
		_pitchSetpoint.Reset(Angle.Zero);
		SetGlobalSmoothing(Strength.VeryMild);
	}

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

	public const float DefaultPitchSensitivityControllerTrigger = 120f;
	public void AdjustPitchViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool rightTriggerPitchesUp = true) {
		ArgumentNullException.ThrowIfNull(input);
		var pitchDownTriggerPosition = rightTriggerPitchesUp ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var pitchUpTriggerPosition = rightTriggerPitchesUp ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPitch(deltaTime, pitchDownTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger)
			- pitchUpTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger));
	}

	public const float DefaultPitchSensitivityKeyOrButtonPress = 120f;
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultPitchSensitivityKeyOrButtonPress));
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
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

	public const float DefaultYawSensitivityControllerTrigger = 120f;
	public void AdjustYawViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerYawsLeft = true) {
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
	public void AdjustYawViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultYawSensitivityKeyOrButtonPress));
	}

	public void AdjustPosition(Orientation cameraRelativeOrientation, float distance) => Position += Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * distance;
	public void AdjustPosition(float deltaTime, Orientation cameraRelativeOrientation, float speed) => AdjustPosition(deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * speed);
	public void AdjustPosition(float deltaTime, Vect adjustmentPerSec) => Position += adjustmentPerSec * deltaTime;

	public const float DefaultPositionSensitivityMouseCursor = 0.0002f;
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Orientation cameraRelativeOrientation, float? speed = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaMouseCursor(input, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityMouseCursor), invertMouseControl, axis);
	}
	public void AdjustPositionViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Vect adjustmentPerPixel, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => -input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Position += delta * adjustmentPerPixel;
	}

	public const float DefaultPositionSensitivityMouseWheel = 0.05f;
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Orientation cameraRelativeOrientation, float? speed = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaMouseWheel(input, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityMouseWheel), invertMouseControl);
	}
	public void AdjustPositionViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Vect adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Position += input.MouseScrollWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultPositionSensitivityControllerStick = 0.5f;
	public void AdjustPositionViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Orientation cameraRelativeOrientation, float? maxSpeed = null, bool useLeftStick = true, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaControllerStick(input, deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (maxSpeed ?? DefaultPositionSensitivityControllerStick), useLeftStick, invertStickControl, axis);
	}
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

	public const float DefaultPositionSensitivityControllerTrigger = 0.5f;
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Orientation cameraRelativeOrientation, float? maxSpeed = null, bool leftTriggerMovesPositive = true) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaControllerTriggers(input, deltaTime, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (maxSpeed ?? DefaultPositionSensitivityControllerTrigger), leftTriggerMovesPositive);
	}
	public void AdjustPositionViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Vect maxAdjustmentPerSec, bool leftTriggerMovesPositive = true) {
		ArgumentNullException.ThrowIfNull(input);
		var positiveTriggerPosition = leftTriggerMovesPositive ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var negativeTriggerPosition = leftTriggerMovesPositive ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPosition(deltaTime, positiveTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - negativeTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}

	public const float DefaultPositionSensitivityKeyOrButtonPress = 0.5f;
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Orientation cameraRelativeOrientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaKeyPress(input, deltaTime, keyToTestFor, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityKeyOrButtonPress));
	}
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, Vect adjustmentPerSec) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPosition(deltaTime, adjustmentPerSec);
	}
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Orientation cameraRelativeOrientation, float? speed = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPositionViaButtonPress(input, deltaTime, buttonToTestFor, Camera.GetRelativeOrientationDirection(cameraRelativeOrientation) * (speed ?? DefaultPositionSensitivityKeyOrButtonPress));
	}
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, Vect adjustmentPerSec) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPosition(deltaTime, adjustmentPerSec);
	}

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
