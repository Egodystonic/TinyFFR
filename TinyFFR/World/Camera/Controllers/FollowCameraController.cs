// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class FollowCameraController : ICameraController<FollowCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<FollowCameraController> _controllerPool = new(&New);
	static FollowCameraController New() => new();
	static FollowCameraController ICameraController<FollowCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(FollowCameraController));
	FollowCameraController() { }
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
	readonly CameraEffectStrengthMap _trackingSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.03f,
		Mild: 0.06f,
		Standard: 0.1f,
		Strong: 0.14f,
		VeryStrong: 0.2f
	);
	Direction _zeroYawZeroPitchDir;

	public Strength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionSetpoint.HalfLife);
		set => _positionSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	public Strength TrackingSmoothingStrength {
		get => _trackingSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set {
			_yawSetpoint.HalfLife = _trackingSmoothingStrengthMap.From(value);
			_pitchSetpoint.HalfLife = _trackingSmoothingStrengthMap.From(value);
		}
	}
	
	public Direction WorldUp {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
			_zeroYawZeroPitchDir = value.AnyOrthogonal();
		}
	}
	public bool AllowUpsideDownFlip { get; set; }
	
	public Location Target {
		get; 
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
			RecalculateSetpoints();
		}
	}
	public Vect TargetOffset {
		get; 
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
			RecalculateSetpoints();
		}
	}

	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomTrackingSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		TrackingSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		AllowUpsideDownFlip = false;
		WorldUp = Direction.Up;
		Target = Location.Origin;
		TargetOffset = Direction.Backward * 0.7f + Direction.Up * 0.3f + Direction.Right * 0.2f;
		_positionSetpoint.Reset(TargetOffset);
		_yawSetpoint.Reset(Angle.Zero);
		_pitchSetpoint.Reset(Angle.Zero);
		SetGlobalSmoothing(Strength.VeryMild);
	}
	
	void RecalculateSetpoints() {
		var cameraPos = Target + TargetOffset;
		var cameraToTarget = cameraPos >> Target;
		_positionSetpoint.TargetValue = cameraPos.AsVect();
		_pitchSetpoint.TargetValue = _zeroYawZeroPitchDir.SignedAngleTo(cameraToTarget.Direction.OrthogonalizedAgainst(WorldUp) ?? _zeroYawZeroPitchDir, WorldUp);
		_yawSetpoint.TargetValue = new Plane(WorldUp).SignedAngleTo(cameraToTarget.Direction);
		
		if (!AllowUpsideDownFlip) {
			var curTarget = _pitchSetpoint.TargetValue;
			var diffToLowerBound = curTarget - Angle.QuarterCircle;
			var diffToUpperBound = (Angle.FullCircle - Angle.QuarterCircle) - curTarget;
			if (diffToLowerBound > 0f && diffToUpperBound > 0f) {
				if (diffToUpperBound < diffToLowerBound) _pitchSetpoint.TargetValue = Angle.FullCircle - Angle.QuarterCircle; 
				else _pitchSetpoint.TargetValue = Angle.QuarterCircle; 
			} 
		}
	}

	public void Progress(float deltaTime) {
		_positionSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);

		var currentHorizontalPlaneDir = _zeroYawZeroPitchDir * (_yawSetpoint.CurrentValue % WorldUp);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(WorldUp, currentHorizontalPlaneDir);
		
		Camera.SetPosition(_positionSetpoint.CurrentValue.AsLocation());
		Camera.SetViewAndUpDirection(currentHorizontalPlaneDir * verticalTiltRot, WorldUp * verticalTiltRot);
	}

	public void AdjustTargetOffsetViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Angle += delta * adjustmentPerPixel;
	}
	public void AdjustAngleViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Angle += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustAngleViaControllerStick(GameControllerStickPosition stickPosition, Angle maxAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Angle += maxAdjustmentPerSec * delta;
	}
	public void AdjustAngleViaControllerTriggers(GameControllerTriggerPosition anticlockwiseTriggerPosition, GameControllerTriggerPosition clockwiseTriggerPosition, Angle maxAdjustmentPerSec, float deltaTime) {
		Angle += deltaTime 
			* (anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - clockwiseTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustAngleViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustAngle(adjustmentPerSec, deltaTime);
	}
	public void AdjustAngleViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustAngle(adjustmentPerSec, deltaTime);
	}

	
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? angleAdjustmentPerPixel = null, float? heightAdjustmentPerPixel = null, float? distanceAdjustmentPerWheelIncrement = null) {
		AdjustAngleViaMouseCursor(kbmInput.MouseCursorDelta, angleAdjustmentPerPixel ?? 0.02f, invertMouseControl: invertAngleControl);
		AdjustHeightViaMouseCursor(kbmInput.MouseCursorDelta, heightAdjustmentPerPixel ?? 0.0001f, invertMouseControl: invertHeightControl);
		AdjustDistanceViaMouseWheel(kbmInput.MouseScrollWheelDelta, distanceAdjustmentPerWheelIncrement ?? 0.045f, invertMouseControl: invertDistanceControl);
	}
	
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? maxAngleAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxDistanceAdjustmentPerSec = null) {
		AdjustAngleViaControllerStick(controllerInput.RightStickPosition, maxAngleAdjustmentPerSec ?? 120f, deltaTime, invertStickControl: invertAngleControl);
		AdjustHeightViaControllerTriggers(invertHeightControl ? controllerInput.RightTriggerPosition : controllerInput.LeftTriggerPosition, invertHeightControl ? controllerInput.LeftTriggerPosition : controllerInput.RightTriggerPosition, maxHeightAdjustmentPerSec ?? 0.5f, deltaTime);
		AdjustDistanceViaControllerStick(controllerInput.LeftStickPosition, maxDistanceAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertDistanceControl);
	}
}
