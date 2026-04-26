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
		VeryMild: 0.35f,
		Mild: 0.5f,
		Standard: 0.7f,
		Strong: 1f,
		VeryStrong: 1.4f
	);
	
	Vect _targetPositionOffset;

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
			+ (Direction.FromDualOrthogonalization(TargetForward, TargetUp) * FollowLateralOffset);
	}
	
	void UpdateLookSetpoint() {
		_lookRelativeSetpoint.TargetValue =
			(TargetForward * LookaheadDistance)
			+ (TargetUp * FollowHeight * HeightViewShiftMultiplier)
			+ (Direction.FromDualOrthogonalization(TargetForward, TargetUp) * FollowLateralOffset * LateralOffsetViewShiftMultiplier);
	}

	public void Progress(float deltaTime) {
		_positionRelativeSetpoint.Progress(deltaTime);
		_lookRelativeSetpoint.Progress(deltaTime);

		Camera.SetPosition(Target + _positionRelativeSetpoint.CurrentValue);
		Camera.LookAt(Target + _lookRelativeSetpoint.CurrentValue, TargetUp);
	}

	public void AdjustFollowDistance(float adjustmentPerSec, float deltaTime) {
		FollowDistance += adjustmentPerSec * deltaTime;
	}
	public void AdjustFollowDistanceViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowDistance += delta * adjustmentPerPixel;
	}
	public void AdjustFollowDistanceViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		FollowDistance += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f : 1f);
	}
	public void AdjustFollowDistanceViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowDistance += maxAdjustmentPerSec * delta;
	}
	public void AdjustFollowDistanceViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		AdjustFollowDistance(increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustFollowDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowDistance(adjustmentPerSec, deltaTime);
	}
	public void AdjustFollowDistanceViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowDistance(adjustmentPerSec, deltaTime);
	}

	public void AdjustFollowHeight(float adjustmentPerSec, float deltaTime) {
		FollowHeight += adjustmentPerSec * deltaTime;
	}
	public void AdjustFollowHeightViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		FollowHeight += delta * adjustmentPerPixel;
	}
	public void AdjustFollowHeightViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		FollowHeight += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f : 1f);
	}
	public void AdjustFollowHeightViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowHeight += maxAdjustmentPerSec * delta;
	}
	public void AdjustFollowHeightViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		AdjustFollowHeight(increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustFollowHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowHeight(adjustmentPerSec, deltaTime);
	}
	public void AdjustFollowHeightViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowHeight(adjustmentPerSec, deltaTime);
	}

	public void AdjustFollowLateralOffset(float adjustmentPerSec, float deltaTime) {
		FollowLateralOffset += adjustmentPerSec * deltaTime;
	}
	public void AdjustFollowLateralOffsetViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		FollowLateralOffset += delta * adjustmentPerPixel;
	}
	public void AdjustFollowLateralOffsetViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		FollowLateralOffset += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f : 1f);
	}
	public void AdjustFollowLateralOffsetViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.X, bool invertStickControl = false) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		FollowLateralOffset += maxAdjustmentPerSec * delta;
	}
	public void AdjustFollowLateralOffsetViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		AdjustFollowLateralOffset(increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec, deltaTime);
	}
	public void AdjustFollowLateralOffsetViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustFollowLateralOffset(adjustmentPerSec, deltaTime);
	}
	public void AdjustFollowLateralOffsetViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustFollowLateralOffset(adjustmentPerSec, deltaTime);
	}

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever kbmInput, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? distanceAdjustmentPerWheelIncrement = null, float? heightAdjustmentPerPixel = null, float? lateralAdjustmentPerPixel = null) {
		AdjustFollowHeightViaMouseCursor(kbmInput.MouseCursorDelta, heightAdjustmentPerPixel ?? 0.0004f, invertMouseControl: invertHeightControl);
		AdjustFollowDistanceViaMouseWheel(kbmInput.MouseScrollWheelDelta, distanceAdjustmentPerWheelIncrement ?? 0.05f, invertMouseControl: invertDistanceControl);
		AdjustFollowLateralOffsetViaMouseCursor(kbmInput.MouseCursorDelta, lateralAdjustmentPerPixel ?? 0.0004f, invertMouseControl: invertLateralControl);
	}

	public void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever controllerInput, float deltaTime, bool invertDistanceControl = false, bool invertHeightControl = false, bool invertLateralControl = false, float? maxDistanceAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxLateralAdjustmentPerSec = null) {
		AdjustFollowDistanceViaControllerStick(controllerInput.RightStickPosition, maxDistanceAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertDistanceControl);
		AdjustFollowHeightViaControllerTriggers(invertHeightControl ? controllerInput.RightTriggerPosition : controllerInput.LeftTriggerPosition, invertHeightControl ? controllerInput.LeftTriggerPosition : controllerInput.RightTriggerPosition, maxHeightAdjustmentPerSec ?? 0.5f, deltaTime);
		AdjustFollowLateralOffsetViaControllerStick(controllerInput.LeftStickPosition, maxLateralAdjustmentPerSec ?? 0.5f, deltaTime, invertStickControl: invertLateralControl);
	}
}
