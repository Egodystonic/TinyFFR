// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class InspectorCameraController : ICameraController<InspectorCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<InspectorCameraController> _controllerPool = new(&New);
	static InspectorCameraController New() => new();
	static InspectorCameraController ICameraController<InspectorCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(InspectorCameraController));
	InspectorCameraController() { }
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		_controllerPool.Return(this);
	}
	#endregion

	public const float DefaultDistanceMax = 2f;
	public const float DefaultDistanceMin = 0.6f;
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
	readonly Spring1DBasedCameraSetpoint _distanceSetpoint = new();
	readonly CameraEffectStrengthMap _distanceSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.15f,
		Mild: 0.25f,
		Standard: 0.4f,
		Strong: 0.65f,
		VeryStrong: 0.9f
	);
	Direction _worldForward;

	public Strength DistanceSmoothingStrength {
		get => _distanceSmoothingStrengthMap.From(_distanceSetpoint.HalfLife);
		set => _distanceSetpoint.HalfLife = _distanceSmoothingStrengthMap.From(value);
	}
	public Strength RotationSmoothingStrength {
		get => _rotationSmoothingStrengthMap.From(_yawSetpoint.HalfLife);
		set {
			_yawSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
			_pitchSetpoint.HalfLife = _rotationSmoothingStrengthMap.From(value);
		}
	}
	
	public float? MinDistance {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value > MaxDistance) MaxDistance = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Distance = Distance;
#pragma warning restore CA2245
		}
	}
	public float? MaxDistance {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value < MinDistance) MinDistance = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Distance = Distance;
#pragma warning restore CA2245
		}
	}
	
	public Direction WorldUp {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
			_worldForward = value.AnyOrthogonal();
		}
	}
	public bool AllowUpsideDownFlip { get; set; }
	
	public float Distance {
		get => _distanceSetpoint.TargetValue;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			if (value < MinDistance) value = MinDistance.Value;
			else if (value > MaxDistance) value = MaxDistance.Value;
			_distanceSetpoint.TargetValue = value;
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
	public Location Target { get; set; }

	public void SetCustomRotationSmoothingStrength(float smoothingHalfLife) {
		_yawSetpoint.HalfLife = smoothingHalfLife;
		_pitchSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomDistanceSmoothingStrength(float smoothingHalfLife) {
		_distanceSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		RotationSmoothingStrength = newSmoothingStrength;
		DistanceSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		MinDistance = DefaultDistanceMin;
		MaxDistance = DefaultDistanceMax;
		WorldUp = Direction.Up;
		AllowUpsideDownFlip = false;
		Target = Location.Origin;
		_yawSetpoint.Reset(Angle.Zero);
		_pitchSetpoint.Reset(Angle.Zero);
		_distanceSetpoint.Reset(DefaultDistanceMin);
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
		
		_distanceSetpoint.Progress(deltaTime);
		_pitchSetpoint.Progress(deltaTime);
		_yawSetpoint.Progress(deltaTime);
		
		var currentHorizontalPlaneDir = _worldForward * (_yawSetpoint.CurrentValue % WorldUp);
		var verticalTiltRot = _pitchSetpoint.CurrentValue % Direction.FromDualOrthogonalization(WorldUp, currentHorizontalPlaneDir);
		var viewDir = currentHorizontalPlaneDir * verticalTiltRot;
		
		Camera.SetPosition(Target - viewDir * _distanceSetpoint.CurrentValue);
		Camera.SetViewAndUpDirection(viewDir, WorldUp * verticalTiltRot);
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

	public void AdjustDistance(float adjustmentPerSec, float deltaTime) {
		Distance += adjustmentPerSec * deltaTime;
	}
	public void AdjustDistanceViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Distance += delta * adjustmentPerPixel;
	}
	public void AdjustDistanceViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Distance += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustDistanceViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Distance += maxAdjustmentPerSec * delta;
	}
	public void AdjustDistanceViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		AdjustDistance(increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustDistance(adjustmentPerSec, deltaTime);
	}
	public void AdjustDistanceViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustDistance(adjustmentPerSec, deltaTime);
	}
	
	public const float DefaultMousePitchSensitivity = 0.02f;
	public const float DefaultMouseYawSensitivity = 0.02f;
	public const float DefaultMouseDistanceSensitivity = 0.045f;
	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? distanceAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(kbmInput);
		AdjustPitchViaMouseCursor(kbmInput.MouseCursorDelta, pitchAdjustmentPerPixel ?? DefaultMousePitchSensitivity, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(kbmInput.MouseCursorDelta, yawAdjustmentPerPixel ?? DefaultMouseYawSensitivity, invertMouseControl: invertYawControl);
		AdjustDistanceViaMouseWheel(kbmInput.MouseScrollWheelDelta, distanceAdjustmentPerWheelIncrement ?? DefaultMouseDistanceSensitivity, invertMouseControl: invertDistanceControl);
	}
	
	public const float DefaultControllerPitchSensitivity = 120f;
	public const float DefaultControllerYawSensitivity = 120f;
	public const float DefaultControllerDistanceSensitivity = 0.5f;
	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxDistanceAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(controllerInput);
		AdjustPitchViaControllerStick(controllerInput.RightStickPosition, maxPitchAdjustmentPerSec ?? DefaultControllerPitchSensitivity, deltaTime, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(controllerInput.RightStickPosition, maxYawAdjustmentPerSec ?? DefaultControllerYawSensitivity, deltaTime, invertStickControl: invertYawControl);
		AdjustDistanceViaControllerStick(controllerInput.LeftStickPosition, maxDistanceAdjustmentPerSec ?? DefaultControllerDistanceSensitivity, deltaTime, invertStickControl: invertDistanceControl);
	}
}
