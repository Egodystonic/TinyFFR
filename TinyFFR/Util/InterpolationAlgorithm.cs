// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1815 // "Should override Equals" -- Can't meaningfully compare function pointers
public readonly unsafe struct InterpolationAlgorithm<T> where T : IInterpolatable<T> {
#pragma warning restore CA1815
#pragma warning disable CA1034 // "Do not nest publicly-visible types" -- I prefer it like this
	public readonly record struct StaticParameterGroup(float AdditionalParameterA, float AdditionalParameterB, float AdditionalParameterC, float AdditionalParameterD) {
		
		public StaticParameterGroup(float additionalParameterA) : this(additionalParameterA, 0f, 0f, 0f) {}
		public StaticParameterGroup(float additionalParameterA, float additionalParameterB) : this(additionalParameterA, additionalParameterB, 0f, 0f) {}
		public StaticParameterGroup(float additionalParameterA, float additionalParameterB, float additionalParameterC) : this(additionalParameterA, additionalParameterB, additionalParameterC, 0f) {}
	}
#pragma warning restore CA1034
	readonly delegate* managed<T, T, StaticParameterGroup, float, T> _algorithmPtr;
	readonly StaticParameterGroup _parameters;

	public InterpolationAlgorithm() => this = Linear();
	InterpolationAlgorithm(delegate* managed<T, T, StaticParameterGroup, float, T> algorithmPtr, StaticParameterGroup parameters) {
		_algorithmPtr = algorithmPtr;
		_parameters = parameters;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static InterpolationAlgorithm<T> Custom(delegate* managed<T, T, StaticParameterGroup, float, T> algorithmPtr, StaticParameterGroup parameters) => new(algorithmPtr, parameters);

	public T GetValue(T startValue, T endValue, float linearDistance) {
		ThrowIfNullAlgorithm();
		return UnsafeGetValueSkipNullCheck(startValue, endValue, linearDistance);
	}
	
	internal void ThrowIfNullAlgorithm() {
		if (_algorithmPtr == null) throw InvalidObjectException.InvalidDefault<InterpolationAlgorithm<T>>();
	} 
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal T UnsafeGetValueSkipNullCheck(T startValue, T endValue, float linearDistance) {
		return _algorithmPtr(startValue, endValue, _parameters, linearDistance);
	}

	#region Algorithms
	public static InterpolationAlgorithm<T> Linear() {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance);
		}
		return new(&Algorithm, new());
	}

	public static InterpolationAlgorithm<T> Accelerate(Strength strength = Strength.Standard) => Accelerate(strength switch {
		Strength.None => 1f,
		Strength.VeryMild => 1.3f,
		Strength.Mild => 1.75f,
		Strength.Strong => 3f,
		Strength.VeryStrong => 5f,
		_ => 2f
	});
	public static InterpolationAlgorithm<T> Accelerate(float exponent) { // TODO xmldoc this is EaseIn
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, MathF.Pow(linearDistance, parameters.AdditionalParameterA));
		}
		static T AlgorithmSpecializationSquare(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance * linearDistance);
		}
		static T AlgorithmSpecializationCube(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance * linearDistance * linearDistance);
		}
		
		return exponent switch {
			2f => new(&AlgorithmSpecializationSquare, new()),
			3f => new(&AlgorithmSpecializationCube, new()),
			_ => new(&Algorithm, new(exponent)) 
		};
	}

	public static InterpolationAlgorithm<T> Decelerate(Strength strength = Strength.Standard) => Decelerate(strength switch {
		Strength.None => 1f,
		Strength.VeryMild => 1.3f,
		Strength.Mild => 1.75f,
		Strength.Strong => 3f,
		Strength.VeryStrong => 5f,
		_ => 2f
	});
	public static InterpolationAlgorithm<T> Decelerate(float exponent) { // TODO xmldoc this is EaseOut
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, 1f - MathF.Pow(1f - linearDistance, parameters.AdditionalParameterA));
		}
		static T AlgorithmSpecializationSquare(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceMirrored = 1f - linearDistance;
			return T.Interpolate(start, end, 1f - linearDistanceMirrored * linearDistanceMirrored);
		}
		static T AlgorithmSpecializationCube(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceMirrored = 1f - linearDistance;
			return T.Interpolate(start, end, 1f - linearDistanceMirrored * linearDistanceMirrored * linearDistanceMirrored);
		}
		
		return exponent switch {
			2f => new(&AlgorithmSpecializationSquare, new()),
			3f => new(&AlgorithmSpecializationCube, new()),
			_ => new(&Algorithm, new(exponent)) 
		};
	}

	public static InterpolationAlgorithm<T> AccelerateWithInitialReverse(Strength strength = Strength.Standard) => AccelerateWithInitialReverse(strength switch {
		Strength.None => 0f,
		Strength.VeryMild => 1f,
		Strength.Mild => 1.3f,
		Strength.Strong => 2.2f,
		Strength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% undershoot, from Robert Penner's algorithms
	});
	public static InterpolationAlgorithm<T> AccelerateWithInitialReverse(float coefficient) {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceSquared = linearDistance * linearDistance;
			return T.Interpolate(start, end, parameters.AdditionalParameterA * linearDistanceSquared * linearDistance - parameters.AdditionalParameterB * linearDistanceSquared);
		}
		return new(&Algorithm, new(coefficient + 1f, coefficient));
	}

	public static InterpolationAlgorithm<T> DecelerateWithOvershoot(Strength strength = Strength.Standard) => DecelerateWithOvershoot(strength switch {
		Strength.None => 0f,
		Strength.VeryMild => 1f,
		Strength.Mild => 1.3f,
		Strength.Strong => 2.2f,
		Strength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% overshoot, from Robert Penner's algorithms
	});
	public static InterpolationAlgorithm<T> DecelerateWithOvershoot(float coefficient) {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceMirrored = 1f - linearDistance;
			var linearDistanceMirroredSquared = linearDistanceMirrored * linearDistanceMirrored;
			var distance = 1f + parameters.AdditionalParameterA * linearDistanceMirroredSquared * linearDistanceMirrored + parameters.AdditionalParameterB * linearDistanceMirroredSquared;
			return T.Interpolate(start, end, distance);
		}
		return new(&Algorithm, new(coefficient + 1f, coefficient));
	}
	
	public static InterpolationAlgorithm<T> AccelerateDecelerate() { // TODO bool param to choose between smooth or smoother, xmldoc this is smoothstep
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance * linearDistance * (3f - 2f * linearDistance));
		}
		return new(&Algorithm, new());
	}

	public static InterpolationAlgorithm<T> CubicBezier(XYPair<float> firstCoord, XYPair<float> secondCoord) {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var cx = 3f * parameters.AdditionalParameterA;
			var bx = 3f * (parameters.AdditionalParameterC - parameters.AdditionalParameterA) - cx;
			var ax = 1f - cx - bx;

			var cy = 3f * parameters.AdditionalParameterB;
			var by = 3f * (parameters.AdditionalParameterD - parameters.AdditionalParameterB) - cy;
			var ay = 1f - cy - by;

			// Newton-Raphson to find bezier parameter u for given x-axis value t
			var u = linearDistance;
			for (var i = 0; i < 8; ++i) {
				var xError = ((ax * u + bx) * u + cx) * u - linearDistance;
				if (MathF.Abs(xError) < 1e-6f) goto solved;
				var derivative = (3f * ax * u + 2f * bx) * u + cx;
				if (MathF.Abs(derivative) < 1e-6f) break;
				u -= xError / derivative;
			}

			// Binary search fallback
			var lo = 0f;
			var hi = 1f;
			u = linearDistance;
			for (var i = 0; i < 20; ++i) {
				var x = ((ax * u + bx) * u + cx) * u;
				if (MathF.Abs(x - linearDistance) < 1e-6f) goto solved;
				if (linearDistance > x) lo = u;
				else hi = u;
				u = (lo + hi) * 0.5f;
			}

			solved:
			var distance = ((ay * u + by) * u + cy) * u;
			return T.Interpolate(start, end, distance);
		}
		return new(&Algorithm, new(firstCoord.X, firstCoord.Y, secondCoord.X, secondCoord.Y));
	}
	#endregion
}