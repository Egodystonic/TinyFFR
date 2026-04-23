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
	readonly SpringAngleBasedCameraSetpoint _pitchSetpoint = new();
	readonly SpringAngleBasedCameraSetpoint _yawSetpoint = new();
	readonly SpringAngleBasedCameraSetpoint _rollSetpoint = new();
	readonly CameraEffectStrengthMap _directionSmoothingStrengthMap = new(
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
	
	Direction _pitchAxis;

	public Strength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	public Strength PitchSmoothingStrength {
		get => _directionSmoothingStrengthMap.From(_pitchSetpoint.HalfLife);
		set => _pitchSetpoint.HalfLife = _directionSmoothingStrengthMap.From(value);
	}
	public Strength YawSmoothingStrength {
		get => _directionSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set => _yawSetpoint.HalfLife = _directionSmoothingStrengthMap.From(value);
	}
	public Strength RollSmoothingStrength {
		get => _directionSmoothingStrengthMap.From(_rollSetpoint.HalfLife);
		set => _rollSetpoint.HalfLife = _directionSmoothingStrengthMap.From(value);
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
			_pitchAxis = TinyFFR.Direction.FromDualOrthogonalization(WorldForward, field);
		}
	}
	
	public Location Position {
		get => _positionSetpoint.TargetValue.AsLocation(); 
		set => _positionSetpoint.TargetValue = value.AsVect();
	}
	public Angle Pitch {
		get => _pitchSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_pitchSetpoint.TargetValue = value;
		}
	}
	public Angle Yaw {
		get => _yawSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_yawSetpoint.TargetValue = value;
		}
	}
	public Angle Roll {
		get => _rollSetpoint.TargetValue;
		set {
			if (!value.IsPhysicallyValid) return;
			_rollSetpoint.TargetValue = value;
		}
	}
	public Direction Direction {
		get {
			return PyrToRot(
				_pitchSetpoint.TargetValue,
				_yawSetpoint.TargetValue,
				_rollSetpoint.TargetValue
			) * WorldForward;
		}
		set {
			if (!value.IsPhysicallyValid) return;
			(_pitchSetpoint.TargetValue, _yawSetpoint.TargetValue, _rollSetpoint.TargetValue) = RotToPyr(WorldForward >> value);
		}
	}

	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomPitchSmoothingStrength(float smoothingHalfLife) {
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomYawSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomRollSmoothingStrength(float smoothingHalfLife) {
		_rollSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetSelfRightingStrength(Angle selfRightingPerSec) {
		_selfRightingPerSec = selfRightingPerSec.ClampZeroToHalfCircle();
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		PitchSmoothingStrength = newSmoothingStrength;
		YawSmoothingStrength = newSmoothingStrength;
		RollSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		WorldForward = Direction.Forward;
		WorldUp = Direction.Up;
		_positionSetpoint.Reset(Vect.Zero);
		_pitchSetpoint.Reset(Angle.Zero);
		_yawSetpoint.Reset(Angle.Zero);
		_rollSetpoint.Reset(Angle.Zero);
		_selfRightingPerSec = Angle.Zero;
		SetGlobalSmoothing(Strength.VeryMild);
	}
	
	Rotation PyrToRot(Angle pitch, Angle yaw, Angle roll) {
		var yawRot = yaw % WorldUp;
		var pitchYawRot = (_pitchAxis % pitch) + yawRot;
		return pitchYawRot + ((WorldForward * pitchYawRot) % roll);
	}
	(Angle Pitch, Angle Yaw, Angle Roll) RotToPyr(Rotation r) {
		var rotatedForward = WorldForward * r;

		Angle yaw, pitch;
		var pitchComponentDir = rotatedForward.OrthogonalizedAgainst(WorldUp);
		if (pitchComponentDir is { } pcd) {
			yaw = WorldForward.SignedAngleTo(pcd, WorldUp);
			var pitchAxisAfterYaw = _pitchAxis * (yaw % WorldUp);
			pitch = pcd.SignedAngleTo(rotatedForward, pitchAxisAfterYaw);
		}
		else { // Gimbal locked
			yaw = Angle.Zero;
			pitch = WorldForward.SignedAngleTo(rotatedForward, _pitchAxis);
		}

		var pitchYawRot = (_pitchAxis % pitch) + (yaw % WorldUp);
		var rollResidual = r - pitchYawRot;
		var roll = rollResidual.AngleAroundAxis(WorldForward);

		return (pitch, yaw, roll);
	}

	public void Progress(float deltaTime) {
		if (_selfRightingPerSec > Angle.Zero) {
			var targetRotation = PyrToRot(_pitchSetpoint.TargetValue, _yawSetpoint.TargetValue, _rollSetpoint.TargetValue);
			var targetToRightedRot = (targetRotation * WorldUp) >> WorldUp;
			var selfRightedRotation = targetRotation + targetToRightedRot with { Angle = Angle.FromRadians(Single.Min(_selfRightingPerSec.Radians, targetToRightedRot.Angle.Radians)) };
			(_pitchSetpoint.TargetValue, _yawSetpoint.TargetValue, _rollSetpoint.TargetValue) = RotToPyr(selfRightedRotation);
		}
		
		_positionSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		_rollSetpoint.Progress(deltaTime);
		
		var currentRotation = PyrToRot(_pitchSetpoint.CurrentValue, _yawSetpoint.CurrentValue, _rollSetpoint.CurrentValue);		
		var viewDir = WorldForward * currentRotation;
		var upDir = WorldUp * currentRotation;
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(viewDir, upDir);
	}

	public void AdjustPitch(Angle adjustmentPerSec, float deltaTime) {
		Pitch += adjustmentPerSec * deltaTime;
	}
	public void AdjustPitchViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

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
		Pitch += deltaTime 
			* (pitchUpTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - pitchDownTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustPitchViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Pitch += adjustmentPerSec * deltaTime;
	}
	public void AdjustPitchViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Pitch += adjustmentPerSec * deltaTime;
	}

	// public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? panAdjustmentPerPixel = null, Angle? tiltAdjustmentPerPixel = null, float? zoomAdjustmentPerWheelIncrement = null) {
	// 	AdjustPanViaMouseCursor(kbmInput.MouseCursorDelta, panAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertPanControl);
	// 	AdjustTiltViaMouseCursor(kbmInput.MouseCursorDelta, tiltAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertTiltControl);
	// 	AdjustZoomViaMouseWheel(kbmInput.MouseScrollWheelDelta, zoomAdjustmentPerWheelIncrement ?? 0.025f, invertMouseControl: invertZoomControl);
	// }
	//
	// public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPanControl = false, bool invertTiltControl = false, bool invertZoomControl = false, Angle? maxPanAdjustmentPerSec = null, Angle? maxTiltAdjustmentPerSec = null, float? maxZoomAdjustmentPerSec = null) {
	// 	AdjustPanViaControllerStick(controllerInput.LeftStickPosition, maxPanAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertPanControl);
	// 	AdjustTiltViaControllerStick(controllerInput.LeftStickPosition, maxTiltAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertTiltControl);
	// 	AdjustZoomViaControllerTriggers(invertZoomControl ? controllerInput.RightTriggerPosition : controllerInput.LeftTriggerPosition, invertZoomControl ? controllerInput.LeftTriggerPosition : controllerInput.RightTriggerPosition, maxZoomAdjustmentPerSec ?? 0.5f, deltaTime);
	// }
}
