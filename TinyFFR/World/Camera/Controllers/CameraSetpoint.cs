// Created on 2026-04-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

sealed class InterpolationBasedCameraSetpoint<T> where T : IInterpolatable<T> {
	public T TargetValue { get; private set; } = default!;
	public T StartingValue { get; private set; } = default!;
	public T CurrentValue { get; private set; } = default!;
	public InterpolationAlgorithm<T> InterpolationAlgorithm {
		get;
		set {
			value.ThrowIfNullAlgorithm();
			field = value;
		}
	}
	public float CompletionTime { get; private set; }
	public float ElapsedTime { get; private set; }
	public bool IsCompleted { get; private set; } = true;

	public void Reset(T targetValue, T startingValue, float completionTime) {
		TargetValue = targetValue;
		StartingValue = startingValue;
		CompletionTime = completionTime;
		ElapsedTime = 0f;
		IsCompleted = !completionTime.IsPositiveAndFinite();
		CurrentValue = IsCompleted ? TargetValue : StartingValue;
	}

	public void AdjustTarget(T newTargetValue) {
		TargetValue = newTargetValue;
		if (IsCompleted) CurrentValue = newTargetValue;
	}

	public void Progress(float deltaTime) {
		if (IsCompleted || !deltaTime.IsPositiveAndFinite()) return;
		ElapsedTime += deltaTime;
		if (ElapsedTime >= CompletionTime) {
			IsCompleted = true;
			ElapsedTime = CompletionTime;
			CurrentValue = TargetValue;
			return;
		}

		CurrentValue = InterpolationAlgorithm.UnsafeGetValueSkipNullAndDividendCheck(StartingValue, TargetValue, ElapsedTime, CompletionTime);
	}
}

sealed class Spring1DBasedCameraSetpoint {
	const float HalfLifeOmegaProduct = 1.6783469f;
	bool _springDisabled = false;
	float _omega = 8f;

	public float HalfLife {
		get => HalfLifeOmegaProduct / _omega;
		set {
			if (!value.IsPositiveAndFinite()) {
				_springDisabled = true;
				return;
			}
			_springDisabled = false;
			_omega = HalfLifeOmegaProduct / value;
		}
	}
	public float Velocity { get; private set; } = 0f;
	public float CurrentValue { get; set; } = 0f;
	public float TargetValue { get; set; } = 0f;

	public void Reset(float currentValue) {
		Velocity = 0f;
		CurrentValue = currentValue;
		TargetValue = currentValue;
		_springDisabled = false;
	}

	public void Progress(float deltaTime) {
		if (!deltaTime.IsPositiveAndFinite()) return;
		if (_springDisabled) {
			CurrentValue = TargetValue;
			return;
		}
		var x = _omega * deltaTime;
		var xSquared = x * x;
		var expApprox = Single.ReciprocalEstimate(1f + x + 0.48f * xSquared + 0.235f * xSquared * x);
		var change = CurrentValue - TargetValue;
		var temp = (Velocity + _omega * change) * deltaTime;
		Velocity = (Velocity - _omega * temp) * expApprox;
		CurrentValue = TargetValue + (change + temp) * expApprox;
	}
}

sealed class Spring3DBasedCameraSetpoint {
	readonly Spring1DBasedCameraSetpoint _xComponentSpring = new();
	readonly Spring1DBasedCameraSetpoint _yComponentSpring = new();
	readonly Spring1DBasedCameraSetpoint _zComponentSpring = new();

	public float HalfLife {
		get => _xComponentSpring.HalfLife;
		set {
			_xComponentSpring.HalfLife = value;
			_yComponentSpring.HalfLife = value;
			_zComponentSpring.HalfLife = value;
		}
	}
	public Vect Velocity {
		get {
			return new Vect(
				_xComponentSpring.Velocity,
				_yComponentSpring.Velocity,
				_zComponentSpring.Velocity
			);
		}
	}
	public Vect CurrentValue {
		get {
			return new Vect(
				_xComponentSpring.CurrentValue,
				_yComponentSpring.CurrentValue,
				_zComponentSpring.CurrentValue
			);
		}
		set {
			_xComponentSpring.CurrentValue = value.X;
			_yComponentSpring.CurrentValue = value.Y;
			_zComponentSpring.CurrentValue = value.Z;
		}
	}
	public Vect TargetValue {
		get {
			return new Vect(
				_xComponentSpring.TargetValue,
				_yComponentSpring.TargetValue,
				_zComponentSpring.TargetValue
			);
		}
		set {
			_xComponentSpring.TargetValue = value.X;
			_yComponentSpring.TargetValue = value.Y;
			_zComponentSpring.TargetValue = value.Z;
		}
	}

	public void Reset(Vect currentValue) {
		_xComponentSpring.Reset(currentValue.X);
		_yComponentSpring.Reset(currentValue.Y);
		_zComponentSpring.Reset(currentValue.Z);
	}

	public void Progress(float deltaTime) {
		_xComponentSpring.Progress(deltaTime);
		_yComponentSpring.Progress(deltaTime);
		_zComponentSpring.Progress(deltaTime);
	}
}

sealed class SpringAngleBasedCameraSetpoint {
	readonly Spring1DBasedCameraSetpoint _rawSetpoint = new();

	public float HalfLife {
		get => _rawSetpoint.HalfLife;
		set => _rawSetpoint.HalfLife = value;
	}
	public Angle Velocity => Angle.FromRadians(_rawSetpoint.Velocity);
	public Angle CurrentValue => Angle.FromRadians(_rawSetpoint.CurrentValue).Normalized;
	public Angle TargetValue {
		get => Angle.FromRadians(_rawSetpoint.TargetValue).Normalized;
		set {
			var currentValueNormalized = CurrentValue;
			var delta = (value - currentValueNormalized).Normalized;
			if (delta > Angle.HalfCircle) delta -= Angle.FullCircle;
			_rawSetpoint.CurrentValue = currentValueNormalized.Radians;
			_rawSetpoint.TargetValue = currentValueNormalized.Radians + delta.Radians;
		}
	}

	public void Reset(Angle currentValue) => _rawSetpoint.Reset(currentValue.Normalized.Radians);
	public void Progress(float deltaTime) => _rawSetpoint.Progress(deltaTime);
}