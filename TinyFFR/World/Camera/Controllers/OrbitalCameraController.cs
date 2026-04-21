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
		VeryMild: 0.02f,
		Mild: 0.05f,
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
	public float Height {
		get => _heightSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value)) return;
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
			_distanceSetpoint.TargetValue = value; 
		}
	}
	public Angle Angle {
		get => _angleSetpoint.TargetValue;
		set {
			if (!Single.IsFinite(value.Radians)) return;
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
		_angleSetpoint.Reset(Angle.Zero);
		_heightSetpoint.Reset(DefaultHeightMin);
		_distanceSetpoint.Reset(DefaultDistanceMin);
		UpDirection = Direction.Up;
		Target = Location.Origin;
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
	
	public void AdjustAngle(Angle adjustment, Angle? maxAngleDiffFromZero = null) {
		Angle += adjustment;
		if (maxAngleDiffFromZero is not { } nonNullMaxAngleAbs) return;
		var half = nonNullMaxAngleAbs * 0.5f;
		var negHalfNorm = (-half).Normalized;
		
		var amountOver = Angle - half;
		var amountUnder = negHalfNorm - Angle;
		if (amountOver > Angle.Zero && amountUnder > Angle.Zero) {
			if (amountOver > amountUnder) Angle = negHalfNorm;
			else Angle = half;
		}
	}
	public void AdjustAngleViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false, Angle? maxAngleDiffFromZero = null) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);

		AdjustAngle(delta * adjustmentPerPixel, maxAngleDiffFromZero);
	}
	public void AdjustAngleViaMouseWheel(int mouseWheelDelta, Angle adjustmentPerWheelIncrement, Angle? maxAngleDiffFromZero = null) {
		AdjustAngle(mouseWheelDelta * adjustmentPerWheelIncrement, maxAngleDiffFromZero);
	}
	public void AdjustAngleViaControllerStick(GameControllerStickPosition stickPosition, Angle fullStickDisplacementAdjustmentPerSec, float deltaTime, bool invertStickControl = false, Axis2D axis = Axis2D.X, Angle? maxAngleDiffFromZero = null) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);
		
		AdjustAngle(fullStickDisplacementAdjustmentPerSec * delta, maxAngleDiffFromZero);
	}
	public void AdjustAngleViaControllerTriggers(GameControllerTriggerPosition anticlockwiseTriggerPosition, GameControllerTriggerPosition clockwiseTriggerPosition, Angle fullTriggerDisplacementAdjustmentPerSec, float deltaTime, Angle? maxAngleDiffFromZero = null) {
		AdjustAngle(
			deltaTime * (anticlockwiseTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec - clockwiseTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec), 
			maxAngleDiffFromZero
		);
	}
	
	public void AdjustHeight(float adjustment, float? minHeight = DefaultHeightMin, float? maxHeight = DefaultHeightMax) {
		Height += adjustment;
		if (Height < minHeight) Height = minHeight.Value;
		else if (Height > maxHeight) Height = maxHeight.Value;
	}
	public void AdjustHeightViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false, float? minHeight = DefaultHeightMin, float? maxHeight = DefaultHeightMax) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);
		
		AdjustHeight(delta * adjustmentPerPixel, minHeight, maxHeight);
	}
	public void AdjustHeightViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, float? minHeight = DefaultHeightMin, float? maxHeight = DefaultHeightMax) {
		AdjustHeight(mouseWheelDelta * adjustmentPerWheelIncrement, minHeight, maxHeight);
	}
	public void AdjustHeightViaControllerStick(GameControllerStickPosition stickPosition, float fullStickDisplacementAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false, float? minHeight = DefaultHeightMin, float? maxHeight = DefaultHeightMax) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);
		
		AdjustHeight(fullStickDisplacementAdjustmentPerSec * delta, minHeight, maxHeight);
	}
	public void AdjustHeightViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float fullTriggerDisplacementAdjustmentPerSec, float deltaTime, float? minHeight = DefaultHeightMin, float? maxHeight = DefaultHeightMax) {
		AdjustHeight(
			deltaTime * (increasingTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec), 
			minHeight, 
			maxHeight
		);
	}
	
	public void AdjustDistance(float adjustment, float? minDistance = DefaultDistanceMin, float? maxDistance = DefaultDistanceMax) {
		Distance += adjustment;
		if (Distance < minDistance) Distance = minDistance.Value;
		else if (Distance > maxDistance) Distance = maxDistance.Value;
	}
	public void AdjustDistanceViaMouseCursor(XYPair<int> cursorDelta, float adjustmentPerPixel, Axis2D axis = Axis2D.Y, bool invertMouseControl = false,float? minDistance = DefaultDistanceMin, float? maxDistance = DefaultDistanceMax) {
		var delta = axis switch {
			Axis2D.X => cursorDelta.X,
			Axis2D.Y => -cursorDelta.Y,
			_ => 0
		} * (invertMouseControl ? -1f : 1f);
		
		AdjustDistance(delta * adjustmentPerPixel, minDistance, maxDistance);
	}
	public void AdjustDistanceViaMouseWheel(int mouseWheelDelta, float adjustmentPerWheelIncrement, float? minDistance = DefaultDistanceMin, float? maxDistance = DefaultDistanceMax) {
		AdjustDistance(mouseWheelDelta * adjustmentPerWheelIncrement, minDistance, maxDistance);
	}
	public void AdjustDistanceViaControllerStick(GameControllerStickPosition stickPosition, float fullStickDisplacementAdjustmentPerSec, float deltaTime, Axis2D axis = Axis2D.Y, bool invertStickControl = false, float? minDistance = DefaultDistanceMin, float? maxDistance = DefaultDistanceMax) {
		var delta = axis switch {
			Axis2D.X => stickPosition.GetDisplacementHorizontalWithDeadzone(),
			Axis2D.Y => stickPosition.GetDisplacementVerticalWithDeadzone(),
			_ => 0f
		} * (invertStickControl ? -deltaTime : deltaTime);
		
		AdjustDistance(fullStickDisplacementAdjustmentPerSec * delta, minDistance, maxDistance);
	}
	public void AdjustDistanceViaControllerTriggers(GameControllerTriggerPosition increasingTriggerPosition, GameControllerTriggerPosition decreasingTriggerPosition, float fullTriggerDisplacementAdjustmentPerSec, float deltaTime, float? minDistance = DefaultDistanceMin, float? maxDistance = DefaultDistanceMax) {
		AdjustDistance(
			deltaTime * (increasingTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec - decreasingTriggerPosition.GetDisplacementWithDeadzone() * fullTriggerDisplacementAdjustmentPerSec), 
			minDistance, 
			maxDistance
		);
	}
}