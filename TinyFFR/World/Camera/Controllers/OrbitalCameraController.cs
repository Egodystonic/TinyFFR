// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class OrbitalCameraController : ICameraController<OrbitalCameraController> {
	#region Creation / Pooling
	static readonly unsafe ObjectPool<OrbitalCameraController> _controllerPool = new(&New);
	static OrbitalCameraController New() => new();
	static OrbitalCameraController ICameraController<OrbitalCameraController>.RentAndTetherToCamera(Camera camera) {
		var result = _controllerPool.Rent();
		result._camera = camera;
		result.ResetParametersToDefault();
		return result;
	}
	Camera? _camera;
	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(OrbitalCameraController));
	OrbitalCameraController() { }
	public void Dispose() {
		if (_camera == null) return;
		_camera = null;
		_controllerPool.Return(this);
	}
	#endregion

	public const float DefaultHeightMax = 0.35f;
	public const float DefaultHeightMin = 0.1f;
	public const float DefaultDistanceMax = 2f;
	public const float DefaultDistanceMin = 0.6f;
	readonly SpringAngleBasedCameraSetpoint _angleSetpoint = new();
	readonly CameraEffectStrengthMap _angleSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.15f,
		Standard: 0.25f,
		Strong: 0.4f,
		VeryStrong: 0.65f
	);
	readonly Spring1DBasedCameraSetpoint _heightSetpoint = new();
	readonly CameraEffectStrengthMap _heightSmoothingStrengthMap = new(
		None: 0f,
		VeryMild: 0.05f,
		Mild: 0.10f,
		Standard: 0.15f,
		Strong: 0.25f,
		VeryStrong: 0.4f
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

	public Strength AngleSmoothingStrength {
		get => _angleSmoothingStrengthMap.From(_angleSetpoint.HalfLife);
		set => _angleSetpoint.HalfLife = _angleSmoothingStrengthMap.From(value);
	}
	public Strength HeightSmoothingStrength {
		get => _heightSmoothingStrengthMap.From(_heightSetpoint.HalfLife);
		set => _heightSetpoint.HalfLife = _heightSmoothingStrengthMap.From(value);
	}
	public Strength DistanceSmoothingStrength {
		get => _distanceSmoothingStrengthMap.From(_distanceSetpoint.HalfLife);
		set => _distanceSetpoint.HalfLife = _distanceSmoothingStrengthMap.From(value);
	}
	
	public Angle? MaxAngleDiffFromZero {
		get; 
		set {
			if (!Single.IsFinite(value?.Radians ?? 0f)) return;
			var absVal = value?.Absolute;
			if (absVal > Angle.FullCircle) absVal = null;
			field = absVal;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Angle = Angle;
#pragma warning restore CA2245
		}
	}
	public float? MinHeight {
		get; 
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value > MaxHeight) MaxHeight = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Height = Height;
#pragma warning restore CA2245
		}
	}
	public float? MaxHeight {
		get;
		set {
			if (value?.IsPositiveAndFinite() == false) return;
			field = value;
			if (value < MinHeight) MinHeight = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-limit-bounding
			Height = Height;
#pragma warning restore CA2245
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
	
	public float Height {
		get => _heightSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value)) return;
			if (value < MinHeight) value = MinHeight.Value;
			else if (value > MaxHeight) value = MaxHeight.Value;
			_heightSetpoint.TargetValue = value;
		}
	}
	public Direction UpDirection {
		get;
		set {
			if (value == Direction.None) return;
			field = value;
#pragma warning disable CA2245 // Self-assignment: Forces re-orthogonalization
			ZeroAngleDirection = ZeroAngleDirection;
#pragma warning restore CA2245
		}
	}
	public Direction ZeroAngleDirection {
		get;
		set {
			field = value.OrthogonalizedAgainst(UpDirection) ?? Direction.None;
			if (field == Direction.None) field = UpDirection.AnyOrthogonal();
		}
	}
	public float Distance {
		get => _distanceSetpoint.TargetValue;
		set {
			if (!value.IsNonNegativeAndFinite()) return;
			if (value < MinDistance) value = MinDistance.Value;
			else if (value > MaxDistance) value = MaxDistance.Value;
			_distanceSetpoint.TargetValue = value;
		}
	}
	public Angle Angle {
		get => _angleSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
			if (MaxAngleDiffFromZero is { } nonNullMaxAngleAbs) {
				if (nonNullMaxAngleAbs <= Angle.Zero) {
					_angleSetpoint.TargetValue = Angle.Zero;
					return;
				}
				var normalized = value.Normalized;
				var half = nonNullMaxAngleAbs * 0.5f;
				var negHalfNorm = (-half).Normalized;
				var amountOver = normalized - half;
				var amountUnder = negHalfNorm - normalized;
				if (amountOver > Angle.Zero && amountUnder > Angle.Zero) {
					value = amountOver > amountUnder ? negHalfNorm : half;
				}
			}
			_angleSetpoint.TargetValue = value;
		}
	}
	public Location Target { get; set; }

	public void SetCustomAngleSmoothingStrength(float smoothingHalfLife) {
		_angleSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomHeightSmoothingStrength(float smoothingHalfLife) {
		_heightSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetCustomDistanceSmoothingStrength(float smoothingHalfLife) {
		_distanceSetpoint.HalfLife = smoothingHalfLife;
	}
	public void SetGlobalSmoothing(Strength newSmoothingStrength) {
		AngleSmoothingStrength = newSmoothingStrength;
		HeightSmoothingStrength = newSmoothingStrength;
		DistanceSmoothingStrength = newSmoothingStrength;
	}

	public void ResetParametersToDefault() {
		MaxAngleDiffFromZero = null;
		MinHeight = DefaultHeightMin;
		MaxHeight = DefaultHeightMax;
		MinDistance = DefaultDistanceMin;
		MaxDistance = DefaultDistanceMax;
		UpDirection = Direction.Up;
		ZeroAngleDirection = Direction.None;
		Target = Location.Origin;
		_angleSetpoint.Reset(Angle.Zero);
		_heightSetpoint.Reset(DefaultHeightMin);
		_distanceSetpoint.Reset(DefaultDistanceMin);
		SetGlobalSmoothing(Strength.Standard);
	}

	public void Progress(float deltaTime) {
		_angleSetpoint.Progress(deltaTime);
		_heightSetpoint.Progress(deltaTime);
		_distanceSetpoint.Progress(deltaTime);

		var planarOffset = (_angleSetpoint.CurrentValue % UpDirection) * ZeroAngleDirection * _distanceSetpoint.CurrentValue;
		var heightOffset = UpDirection * _heightSetpoint.CurrentValue;
		Camera.SetPosition(Target + planarOffset + heightOffset);
		Camera.LookAt(Target, UpDirection);
	}

	public void AdjustAngleViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
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
		Angle += adjustmentPerSec * deltaTime;
	}
	public void AdjustAngleViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, Angle adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Angle += adjustmentPerSec * deltaTime;
	}

	public void AdjustHeightViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Height += delta * adjustmentPerPixel;
	}
	public void AdjustHeightViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, bool invertMouseControl = false) {
		Height += mouseWheelDelta * adjustmentPerWheelIncrement * (invertMouseControl ? -1f: 1f);
	}
	public void AdjustHeightViaControllerStick(GameControllerStickPosition stickPosition, float maxAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Height += maxAdjustmentPerSec * delta;
	}
	public void AdjustHeightViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float maxAdjustmentPerSec, float deltaTime) {
		Height += deltaTime
			* (increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Height += adjustmentPerSec * deltaTime;
	}
	public void AdjustHeightViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Height += adjustmentPerSec * deltaTime;
	}

	public void AdjustDistanceViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
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
		Distance += deltaTime
			* (increasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * maxAdjustmentPerSec);
	}
	public void AdjustDistanceViaKeyPress(ILatestKeyboardAndMouseInputRetriever kbmInput, KeyboardOrMouseKey keyToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!kbmInput.KeyIsCurrentlyDown(keyToTestFor)) return;
		Distance += adjustmentPerSec * deltaTime;
	}
	public void AdjustDistanceViaButtonPress(ILatestGameControllerInputStateRetriever controllerInput, GameControllerButton buttonToTestFor, float adjustmentPerSec, float deltaTime) {
		if (!controllerInput.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		Distance += adjustmentPerSec * deltaTime;
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
