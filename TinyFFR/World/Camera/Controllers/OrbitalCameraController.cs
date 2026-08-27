// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public sealed class OrbitalCameraController : ICameraController<OrbitalCameraController> {
	#region Creation / Pooling
	static readonly unsafe ArrayPoolBackedObjectPool<OrbitalCameraController> _controllerPool = new(&New);
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

	public const float DefaultHeightMax = 0.5f;
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
	
	public Angle? AngleRange {
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
			if (!value.IsPhysicallyValidAndNotNone) return;
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
			if (!field.IsPhysicallyValidAndNotNone) field = UpDirection.AnyOrthogonal();
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
			if (AngleRange is { } nonNullMaxAngleAbs) {
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
		AngleRange = null;
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
		SetGlobalSmoothing(Strength.VeryMild);
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

	public void AdjustAngle(float deltaTime, Angle adjustmentPerSec) => Angle += adjustmentPerSec * deltaTime;

	public const float DefaultAngleSensitivityMouseCursor = 0.02f;
	public void AdjustAngleViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		Angle += delta * (adjustmentPerPixel ?? DefaultAngleSensitivityMouseCursor);
	}

	public const float DefaultAngleSensitivityMouseWheel = 5f;
	public void AdjustAngleViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, Angle? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Angle += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultAngleSensitivityMouseWheel) * (invertMouseControl ? -1f : 1f);
	}

	public const float DefaultAngleSensitivityControllerStick = 120f;
	public void AdjustAngleViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.X) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Angle += (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerStick) * delta;
	}

	public const float DefaultAngleSensitivityControllerTrigger = 120f;
	public void AdjustAngleViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, Angle? maxAdjustmentPerSec = null, bool leftTriggerRotatesClockwise = true) {
		ArgumentNullException.ThrowIfNull(input);
		var clockwiseTriggerPosition = leftTriggerRotatesClockwise ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var anticlockwiseTriggerPosition = leftTriggerRotatesClockwise ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustAngle(deltaTime, anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerTrigger)
			- clockwiseTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultAngleSensitivityControllerTrigger));
	}

	public const float DefaultAngleSensitivityKeyOrButtonPress = 120f;
	public void AdjustAngleViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustAngle(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultAngleSensitivityKeyOrButtonPress));
	}
	public void AdjustAngleViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, Angle? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustAngle(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultAngleSensitivityKeyOrButtonPress));
	}

	public void AdjustHeight(float deltaTime, float adjustmentPerSec) => Height += adjustmentPerSec * deltaTime;

	public const float DefaultHeightSensitivityMouseCursor = 0.0001f;
	public void AdjustHeightViaMouseCursor(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerPixel = null, bool invertMouseControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var delta = axis switch {
			Axis2D.X => input.MouseCursorDelta.X,
			Axis2D.Y => input.MouseCursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? 1f : -1f);

		Height += delta * (adjustmentPerPixel ?? DefaultHeightSensitivityMouseCursor);
	}

	public const float DefaultHeightSensitivityMouseWheel = 0.025f;
	public void AdjustHeightViaMouseWheel(ILatestKeyboardAndMouseInputRetriever input, float? adjustmentPerWheelIncrement = null, bool invertMouseControl = false) {
		ArgumentNullException.ThrowIfNull(input);
		Height += input.MouseScrollWheelDelta * (adjustmentPerWheelIncrement ?? DefaultHeightSensitivityMouseWheel) * (invertMouseControl ? 1f : -1f);
	}

	public const float DefaultHeightSensitivityControllerStick = 0.5f;
	public void AdjustHeightViaControllerStick(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool useLeftStick = false, bool invertStickControl = false, Axis2D axis = Axis2D.Y) {
		ArgumentNullException.ThrowIfNull(input);
		var stickPosition = useLeftStick ? input.LeftStickPosition : input.RightStickPosition;
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);

		Height += (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerStick) * delta;
	}

	public const float DefaultHeightSensitivityControllerTrigger = 0.5f;
	public void AdjustHeightViaControllerTriggers(ILatestGameControllerInputRetriever input, float deltaTime, float? maxAdjustmentPerSec = null, bool leftTriggerRaisesHeight = true) {
		ArgumentNullException.ThrowIfNull(input);
		var increasingTriggerPosition = leftTriggerRaisesHeight ? input.LeftTriggerPosition : input.RightTriggerPosition;
		var decreasingTriggerPosition = leftTriggerRaisesHeight ? input.RightTriggerPosition : input.LeftTriggerPosition;
		AdjustHeight(deltaTime, increasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerTrigger)
			- decreasingTriggerPosition.GetDisplacementWithDeadzone() * (maxAdjustmentPerSec ?? DefaultHeightSensitivityControllerTrigger));
	}

	public const float DefaultHeightSensitivityKeyOrButtonPress = 0.5f;
	public void AdjustHeightViaKeyPress(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, KeyboardOrMouseKey keyToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.KeyIsCurrentlyDown(keyToTestFor)) return;
		AdjustHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultHeightSensitivityKeyOrButtonPress));
	}
	public void AdjustHeightViaButtonPress(ILatestGameControllerInputRetriever input, float deltaTime, GameControllerButton buttonToTestFor, bool reverse, float? adjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		if (!input.ButtonIsCurrentlyDown(buttonToTestFor)) return;
		AdjustHeight(deltaTime, (reverse ? -1f : 1f) * (adjustmentPerSec ?? DefaultHeightSensitivityKeyOrButtonPress));
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

	public void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? angleAdjustmentPerPixel = null, float? heightAdjustmentPerPixel = null, float? distanceAdjustmentPerWheelIncrement = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustAngleViaMouseCursor(input, angleAdjustmentPerPixel, invertMouseControl: invertAngleControl);
		AdjustHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
		AdjustDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
	}

	public void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime, bool invertAngleControl = false, bool invertHeightControl = false, bool invertDistanceControl = false, Angle? maxAngleAdjustmentPerSec = null, float? maxHeightAdjustmentPerSec = null, float? maxDistanceAdjustmentPerSec = null) {
		ArgumentNullException.ThrowIfNull(input);
		AdjustAngleViaControllerStick(input, deltaTime, maxAngleAdjustmentPerSec, invertStickControl: invertAngleControl);
		AdjustHeightViaControllerStick(input, deltaTime, maxHeightAdjustmentPerSec, invertStickControl: invertHeightControl);
		AdjustDistanceViaControllerTriggers(input, deltaTime, maxDistanceAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
	}
	
	void ICameraController.AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
	void ICameraController.AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime) => AdjustAllViaDefaultControls(input, deltaTime);
}
