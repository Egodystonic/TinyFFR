// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class InspectorCameraController : ICameraController<InspectorCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<InspectorCameraController> _controllerPool = new(&New);
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
	
	public void SetParametersFromBoundingBox(PositionedCuboid boundingBox) {
		var enclosingSphere = boundingBox.SmallestEnclosingSphere;
		Target = enclosingSphere.Position;
		MinDistance = Single.Min(enclosingSphere.Radius * 1f, boundingBox.SmallestHalfExtent);
		MaxDistance = enclosingSphere.Radius * 3f;
		Distance = enclosingSphere.Radius * 1.5f;
	}

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
		Pitch += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultPitchSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	public const float DefaultPitchSensitivityControllerStick = 120f;
	public void AdjustPitchViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
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
	public void AdjustPitchViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerPitchesUp = true) {
		ArgumentNullException.ThrowIfNull(input);
		var pitchUpTriggerPosition = leftTriggerPitchesUp ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var pitchDownTriggerPosition = leftTriggerPitchesUp ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustPitch(deltaTime, pitchUpTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger)
			- pitchDownTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultPitchSensitivityControllerTrigger));
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
		Yaw += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultYawSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultYawSensitivityControllerStick = 120f;
	public void AdjustYawViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
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
	public void AdjustYawViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerYawsClockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var yawClockwiseTriggerPosition = leftTriggerYawsClockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var yawAnticlockwiseTriggerPosition = leftTriggerYawsClockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		AdjustYaw(deltaTime, yawClockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger)
			- yawAnticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultYawSensitivityControllerTrigger));
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

	public void AdjustDistance(float deltaTime, float adjustmentPerSec) => Distance += adjustmentPerSec * deltaTime;

	public const float DefaultDistanceSensitivityMouseCursor = 0.001f;
	public void AdjustDistanceViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Distance += delta * (adjustmentPerPixel ?? DefaultDistanceSensitivityMouseCursor);
	}

	public const float DefaultDistanceSensitivityMouseWheel = 0.045f;
	public void AdjustDistanceViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Distance += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultDistanceSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultDistanceSensitivityControllerStick = 0.5f;
	public void AdjustDistanceViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		Distance += (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerStick) * delta;
	}

	public const float DefaultDistanceSensitivityControllerTrigger = 0.5f;
	public void AdjustDistanceViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustDistance(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistanceSensitivityControllerTrigger));
	}

	public const float DefaultDistanceSensitivityKeyOrButtonPress = 1f;
	public void AdjustDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistanceSensitivityKeyOrButtonPress));
	}
	public void AdjustDistanceViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistanceSensitivityKeyOrButtonPress));
	}
	
	public void AdjustDistancePercentage(float adjustment) {
		if (MaxDistance is not { } max || MinDistance is not { } min) {
			Distance += adjustment;
			return;
		}
		Distance += (adjustment * (max - min));
	}
	public void AdjustDistancePercentage(float deltaTime, float adjustmentPerSec) => AdjustDistancePercentage(adjustmentPerSec * deltaTime);

	public const float DefaultDistancePercentageSensitivityMouseCursor = 0.01f;
	public void AdjustDistancePercentageViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		AdjustDistancePercentage(delta * (adjustmentPerPixel ?? DefaultDistancePercentageSensitivityMouseCursor));
	}

	public const float DefaultDistancePercentageSensitivityMouseWheel = 0.05f;
	public void AdjustDistancePercentageViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustDistancePercentage(input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultDistancePercentageSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f));
	}

	public const float DefaultDistancePercentageSensitivityControllerStick = 0.3333f;
	public void AdjustDistancePercentageViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? deltaTime : -deltaTime);

		AdjustDistancePercentage((maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerStick) * delta);
	}

	public const float DefaultDistancePercentageSensitivityControllerTrigger = 0.3333f;
	public void AdjustDistancePercentageViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustDistancePercentage(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultDistancePercentageSensitivityControllerTrigger));
	}

	public const float DefaultDistancePercentageSensitivityKeyOrButtonPress = 0.3333f;
	public void AdjustDistancePercentageViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustDistancePercentage(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistancePercentageSensitivityKeyOrButtonPress));
	}
	public void AdjustDistancePercentageViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustDistancePercentage(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultDistancePercentageSensitivityKeyOrButtonPress));
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? pitchAdjustmentPerPixel = null, Angle? yawAdjustmentPerPixel = null, float? distancePercentageAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
		AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);
		AdjustDistancePercentageViaMouseWheel(input, distancePercentageAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
	}

	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertPitchControl = false, bool invertYawControl = false, bool invertDistanceControl = false, Angle? maxPitchAdjustmentPerSec = null, Angle? maxYawAdjustmentPerSec = null, float? maxDistancePercentageAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
		AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);
		AdjustDistancePercentageViaControllerTriggers(input, deltaTime, maxDistancePercentageAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
