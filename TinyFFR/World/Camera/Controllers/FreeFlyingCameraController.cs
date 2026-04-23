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
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPitch(adjustmentPerSec, deltaTime);
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
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
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	public void AdjustYawViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustYaw(adjustmentPerSec, deltaTime);
	}
	
	public void AdjustPosition(Vect adjustmentPerSec, float deltaTime) {
		Position += adjustmentPerSec * deltaTime;
	}
	public void AdjustPositionViaMouseCursor(XYPair<int> cursorDelta, Vect adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Position += delta * adjustmentPerPixel;
	}
	public void AdjustPositionViaMouseWheel(int mouseWheelDelta, Vect adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Position += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustPositionViaControllerStick(GameControllerStickPosition stickPosition, Vect maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Position += maxAdjustmentPerSec * delta;
	}
	public void AdjustPositionViaControllerTriggers(GameControllerTriggerPosition positiveTriggerPosition, GameControllerTriggerPosition negativeTriggerPosition, Vect maxAdjustmentPerSec, float deltaTime) {
		AdjustPosition(positiveTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - negativeTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustPositionViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Vect adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustPosition(adjustmentPerSec, deltaTime);
	}
	public void AdjustPositionViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Vect adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustPosition(adjustmentPerSec, deltaTime);
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertUpDownPositionalControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? positionAdjustmentPerSec = null) {
		AdjustPitchViaMouseCursor(kbmInput.MouseCursorDelta, pitchAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(kbmInput.MouseCursorDelta, yawAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertYawControl);
		
		var moveSpeed = positionAdjustmentPerSec ?? 0.5f;
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowLeft, Camera.GetDirectionRelativeToCamera(Orientation.Left) * moveSpeed, deltaTime);
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowRight, Camera.GetDirectionRelativeToCamera(Orientation.Right) * moveSpeed, deltaTime);
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowUp, Camera.GetDirectionRelativeToCamera(Orientation.Forward) * moveSpeed, deltaTime);
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.ArrowDown, Camera.GetDirectionRelativeToCamera(Orientation.Backward) * moveSpeed, deltaTime);
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.RightShift, Camera.GetDirectionRelativeToCamera(invertUpDownPositionalControl ? Orientation.Down : Orientation.Up) * moveSpeed, deltaTime);
		AdjustPositionViaKeyPress(kbmInput, KeyboardOrMouseKey.RightControl, Camera.GetDirectionRelativeToCamera(invertUpDownPositionalControl ? Orientation.Up : Orientation.Down) * moveSpeed, deltaTime);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertUpDownPositionalControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? positionAdjustmentPerSec = null) {
		AdjustPitchViaControllerStick(controllerInput.RightStickPosition, maxPitchAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(controllerInput.RightStickPosition, maxYawAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertYawControl);
		
		var moveSpeed = positionAdjustmentPerSec ?? 0.5f;
		AdjustPositionViaControllerStick(controllerInput.LeftStickPosition, Camera.GetDirectionRelativeToCamera(Orientation.Forward) * moveSpeed, deltaTime, axis: Axis2D.Y);
		AdjustPositionViaControllerStick(controllerInput.LeftStickPosition, Camera.GetDirectionRelativeToCamera(Orientation.Right) * moveSpeed, deltaTime, axis: Axis2D.X);
		AdjustPositionViaControllerTriggers(controllerInput.LeftTriggerPosition, controllerInput.RightTriggerPosition, Camera.GetDirectionRelativeToCamera(invertUpDownPositionalControl ? Orientation.Down : Orientation.Up) * moveSpeed, deltaTime);
	}
}
