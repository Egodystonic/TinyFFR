// Created on 2026-04-26 by Ben Bowen
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

	readonly Spring3DBasedCameraSetpoint _positionRelativeSetpoint = new();
	readonly CameraEffectStrengthMap _positionSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.1f,
		Standard: 0.2f,
		Strong: 0.3f,
		VeryStrong: 0.4f
	);
	
	readonly Spring3DBasedCameraSetpoint _lookRelativeSetpoint = new();
	readonly CameraEffectStrengthMap _trackingSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.5f,
		Mild: 0.7f,
		Standard: 1f,
		Strong: 1.4f,
		VeryStrong: 2f
	);

	public Strength PositionSmoothingStrength {
		get => _positionSmoothingStrengthMap.From(_positionRelativeSetpoint.HalfLife);
		set => _positionRelativeSetpoint.HalfLife = _positionSmoothingStrengthMap.From(value);
	}
	public Strength TrackingSmoothingStrength {
		get => _trackingSmoothingStrengthMap.From(_lookRelativeSetpoint.HalfLife);
		set => _lookRelativeSetpoint.HalfLife = _trackingSmoothingStrengthMap.From(value);
	}

	public Location Target {
		get;
		set {
			if (!value.IsPhysicallyValid) return;
			field = value;
		}
	}
	public Direction TargetForward {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			TargetUp = TargetUp;
#pragma warning restore CA2245
		}
	}
	public Direction TargetUp {
		get;
		set {
			if (!value.IsPhysicallyValidAndNotNone) return;
			field = value.OrthogonalizedAgainst(TargetForward) ?? TargetForward.AnyOrthogonal();
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}

	public float FollowDistance {
		get;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdatePositionOffset();
		}
	}
	public float FollowHeight {
		get;
		set {
			if (!Single.IsFinite(value)) return;
			field = value;
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}
	public float FollowLateralOffset {
		get;
		set {
			if (!Single.IsFinite(value)) return;
			field = value;
			UpdatePositionOffset();
			UpdateLookSetpoint();
		}
	}
	public float LateralOffsetViewShiftMultiplier {
		get; 
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}
	public float HeightViewShiftMultiplier {
		get; 
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}
	public float LookaheadDistance {
		get;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			field = value;
			UpdateLookSetpoint();
		}
	}

	public void SetCustomPositionSmoothingStrength(float smoothingHalfLife) {
		_positionRelativeSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomTrackingSmoothingStrength(float smoothingHalfLife) {
		_lookRelativeSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		PositionSmoothingStrength = newSmoothingStrength;
		TrackingSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		LateralOffsetViewShiftMultiplier = 0.28f;
		HeightViewShiftMultiplier = 0.44f;
		LookaheadDistance = 2.4f;
		Target = Location.Origin;
		TargetForward = Direction.Forward;
		TargetUp = Direction.Up;
		FollowDistance = 0.6f;
		FollowHeight = 0.3f;
		FollowLateralOffset = 0.4f;
		_positionRelativeSetpoint.Reset(_positionRelativeSetpoint.TargetValue);
		_lookRelativeSetpoint.Reset(_lookRelativeSetpoint.TargetValue);
		SetGlobalSmoothing(Strength.VeryMild);
	}
	
	void UpdatePositionOffset() {
		_positionRelativeSetpoint.TargetValue =
			(TargetForward * -FollowDistance)
			+ (TargetUp * FollowHeight)
			+ (Direction.FromDualOrthogonalization(TargetUp, TargetForward) * FollowLateralOffset);
	}
	
	void UpdateLookSetpoint() {
		_lookRelativeSetpoint.TargetValue =
			(TargetForward * LookaheadDistance)
			+ (TargetUp * FollowHeight * HeightViewShiftMultiplier)
			+ (Direction.FromDualOrthogonalization(TargetUp, TargetForward) * FollowLateralOffset * LateralOffsetViewShiftMultiplier);
	}

	public void Progress(float deltaTime) {
		_positionRelativeSetpoint.Progress(deltaTime);
		_lookRelativeSetpoint.Progress(deltaTime);

		Camera.SetPosition(Target + _positionRelativeSetpoint.CurrentValue);
		Camera.LookAt(Target + _lookRelativeSetpoint.CurrentValue, TargetUp);
	}

	public void AdjustFollowDistance(float deltaTime, float adjustmentPerSec) => FollowDistance += adjustmentPerSec * deltaTime;

	public const float DefaultFollowDistanceSensitivityMouseCursor = 0.001f;
	public void AdjustFollowDistanceViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowDistance += delta * (adjustmentPerPixel ?? DefaultFollowDistanceSensitivityMouseCursor);
	}

	public const float DefaultFollowDistanceSensitivityMouseWheel = 0.05f;
	public void AdjustFollowDistanceViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowDistance += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowDistanceSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultFollowDistanceSensitivityControllerStick = 0.5f;
	public void AdjustFollowDistanceViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowDistance += (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerStick) * delta;
	}

	public const float DefaultFollowDistanceSensitivityControllerTrigger = 0.5f;
	public void AdjustFollowDistanceViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerIncreasesDistance = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerIncreasesDistance ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerIncreasesDistance ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustFollowDistance(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowDistanceSensitivityControllerTrigger));
	}

	public const float DefaultFollowDistanceSensitivityKeyOrButtonPress = 1f;
	public void AdjustFollowDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowDistanceSensitivityKeyOrButtonPress));
	}
	public void AdjustFollowDistanceViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowDistance(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowDistanceSensitivityKeyOrButtonPress));
	}

	public void AdjustFollowHeight(float deltaTime, float adjustmentPerSec) => FollowHeight += adjustmentPerSec * deltaTime;

	public const float DefaultFollowHeightSensitivityMouseCursor = 0.0002f;
	public void AdjustFollowHeightViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		FollowHeight += delta * (adjustmentPerPixel ?? DefaultFollowHeightSensitivityMouseCursor);
	}

	public const float DefaultFollowHeightSensitivityMouseWheel = 0.05f;
	public void AdjustFollowHeightViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowHeight += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowHeightSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	public const float DefaultFollowHeightSensitivityControllerStick = 0.5f;
	public void AdjustFollowHeightViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = true, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowHeight += (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerStick) * delta;
	}

	public const float DefaultFollowHeightSensitivityControllerTrigger = 0.5f;
	public void AdjustFollowHeightViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerRaisesHeight = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerRaisesHeight ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerRaisesHeight ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustFollowHeight(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowHeightSensitivityControllerTrigger));
	}

	public const float DefaultFollowHeightSensitivityKeyOrButtonPress = 1f;
	public void AdjustFollowHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowHeightSensitivityKeyOrButtonPress));
	}
	public void AdjustFollowHeightViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowHeightSensitivityKeyOrButtonPress));
	}

	public void AdjustFollowLateralOffset(float deltaTime, float adjustmentPerSec) => FollowLateralOffset += adjustmentPerSec * deltaTime;

	public const float DefaultFollowLateralOffsetSensitivityMouseCursor = DefaultFollowHeightSensitivityMouseCursor;
	public void AdjustFollowLateralOffsetViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => -input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowLateralOffset += delta * (adjustmentPerPixel ?? DefaultFollowLateralOffsetSensitivityMouseCursor);
	}

	public const float DefaultFollowLateralOffsetSensitivityMouseWheel = DefaultFollowHeightSensitivityMouseWheel;
	public void AdjustFollowLateralOffsetViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		FollowLateralOffset += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultFollowLateralOffsetSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultFollowLateralOffsetSensitivityControllerStick = DefaultFollowHeightSensitivityControllerStick;
	public void AdjustFollowLateralOffsetViaControllerStick(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = true, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowLateralOffset += (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerStick) * delta;
	}

	public const float DefaultFollowLateralOffsetSensitivityControllerTrigger = DefaultFollowHeightSensitivityControllerTrigger;
	public void AdjustFollowLateralOffsetViaControllerTriggers(ILatestGameControllerInputStateRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerOffsetsLeft = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerOffsetsLeft ? input.RightTriggerPosition : input.LeftTriggerPosition;
		var decreasingTriggerPosition = leftTriggerOffsetsLeft ? input.LeftTriggerPosition : input.RightTriggerPosition;
		AdjustFollowLateralOffset(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityControllerTrigger));
	}

	public const float DefaultFollowLateralOffsetSensitivityKeyOrButtonPress = DefaultFollowHeightSensitivityKeyOrButtonPress;
	public void AdjustFollowLateralOffsetViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowLateralOffset(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityKeyOrButtonPress));
	}
	public void AdjustFollowLateralOffsetViaButtonPress(ILatestGameControllerInputStateRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowLateralOffset(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultFollowLateralOffsetSensitivityKeyOrButtonPress));
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? distanceAdjustmentPerWheelIncrement = null, float? heightAdjustmentPerPixel = null, float? lateralAdjustmentPerPixel = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustFollowHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
		AdjustFollowDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
		AdjustFollowLateralOffsetViaMouseCursor(input, lateralAdjustmentPerPixel, invertMouseControl: invertLateralControl);
	}

	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? maxDistanceAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxLateralAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustFollowDistanceViaControllerStick(input, deltaTime, maxDistanceAdjustmentPerSec, invertStickControl: invertDistanceControl);
		AdjustFollowHeightViaControllerTriggers(input, deltaTime, maxHeightAdjustmentPerSec, leftTriggerRaisesHeight: !invertHeightControl);
		AdjustFollowLateralOffsetViaControllerStick(input, deltaTime, maxLateralAdjustmentPerSec, useLeftStick: true, invertStickControl: invertLateralControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
