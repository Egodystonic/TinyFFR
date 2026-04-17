// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static System.Single;

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1815 // "Should override Equals" -- Can't meaningfully compare function pointers
public readonly unsafe struct InterpolationAlgorithm<T> where T : IInterpolatable<T> {
#pragma warning restore CA1815
#pragma warning disable CA1034 // "Do not nest publicly-visible types" -- I prefer it like this
	public readonly record struct StaticParameterGroup(float A, float B, float C, float D) {
		public StaticParameterGroup(float a) : this(a, 0f, 0f, 0f) {}
		public StaticParameterGroup(float a, float b) : this(a, b, 0f, 0f) {}
		public StaticParameterGroup(float a, float b, float c) : this(a, b, c, 0f) {}
	}
#pragma warning restore CA1034
	readonly delegate* managed<T, T, StaticParameterGroup, float, T> _algorithmPtr;
	readonly StaticParameterGroup _parameters;

	public InterpolationAlgorithm() => this = Linear();
	InterpolationAlgorithm(delegate* managed<T, T, StaticParameterGroup, float, T> algorithmPtr, StaticParameterGroup parameters) {
		ArgumentNullException.ThrowIfNull(algorithmPtr);
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

	public static InterpolationAlgorithm<T> AccelerateFromSlow(Strength strength = Strength.Standard) => AccelerateFromSlow(strength switch {
		Strength.None => 1f,
		Strength.VeryMild => 1.3f,
		Strength.Mild => 1.75f,
		Strength.Strong => 3f,
		Strength.VeryStrong => 5f,
		_ => 2f
	});
	public static InterpolationAlgorithm<T> AccelerateFromSlow(float exponent) { // TODO xmldoc this is EaseIn; starts slow and accelerates up to 1.0
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, MathF.Pow(linearDistance, parameters.A));
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

	public static InterpolationAlgorithm<T> DecelerateFromFast(Strength strength = Strength.Standard) => DecelerateFromFast(strength switch {
		Strength.None => 1f,
		Strength.VeryMild => 1.3f,
		Strength.Mild => 1.75f,
		Strength.Strong => 3f,
		Strength.VeryStrong => 5f,
		_ => 2f
	});
	public static InterpolationAlgorithm<T> DecelerateFromFast(float exponent) { // TODO xmldoc this is EaseOut; starts fast and decelerates to 1.0
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, 1f - MathF.Pow(1f - linearDistance, parameters.A));
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

	public static InterpolationAlgorithm<T> AccelerateFromSlowWithInitialReverse(Strength strength = Strength.Standard) => AccelerateFromSlowWithInitialReverse(strength switch {
		Strength.None => 0f,
		Strength.VeryMild => 1f,
		Strength.Mild => 1.3f,
		Strength.Strong => 2.2f,
		Strength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% undershoot, from Robert Penner's algorithms
	});
	public static InterpolationAlgorithm<T> AccelerateFromSlowWithInitialReverse(float coefficient) {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceSquared = linearDistance * linearDistance;
			return T.Interpolate(
				start, 
				end, 
				parameters.A * linearDistanceSquared * linearDistance - parameters.B * linearDistanceSquared
			);
		}
		return new(&Algorithm, new(coefficient + 1f, coefficient));
	}

	public static InterpolationAlgorithm<T> DecelerateFromFastWithOvershoot(Strength strength = Strength.Standard) => DecelerateFromFastWithOvershoot(strength switch {
		Strength.None => 0f,
		Strength.VeryMild => 1f,
		Strength.Mild => 1.3f,
		Strength.Strong => 2.2f,
		Strength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% overshoot, from Robert Penner's algorithms
	});
	public static InterpolationAlgorithm<T> DecelerateFromFastWithOvershoot(float coefficient) {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			var linearDistanceMirrored = 1f - linearDistance;
			var linearDistanceMirroredSquared = linearDistanceMirrored * linearDistanceMirrored;
			return T.Interpolate(
				start, 
				end, 
				1f - parameters.A * linearDistanceMirroredSquared * linearDistanceMirrored + parameters.B * linearDistanceMirroredSquared
			);
		}
		return new(&Algorithm, new(coefficient + 1f, coefficient));
	}
	
	public static InterpolationAlgorithm<T> Natural(bool additionalSmoothing = false) { // TODO xmldoc this is smoothstep/smootherstep
		static T AlgorithmSmoothStep(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance * linearDistance * (3f - 2f * linearDistance));
		}
		static T AlgorithmSmootherStep(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance * linearDistance * linearDistance * (linearDistance * (6.0f * linearDistance - 15.0f) + 10.0f));
		}
		return additionalSmoothing 
			? new(&AlgorithmSmootherStep, new())
			: new(&AlgorithmSmoothStep, new()) ;
	}

	public static InterpolationAlgorithm<T> CubicBezier(XYPair<float> firstCoord, XYPair<float> secondCoord) { // TODO call out https://cubic-bezier.com as a way to play with these parameters
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			const float AcceptableErrorMargin = 1E-5f;
			const float MinNewtonRaphsonGradient = 1E-6f;
			const int MaxNewtonRaphsonIterations = 8;
			const int MaxBinarySearchIterations = 20;

			// These two escape hatches continue the slope at x=0 or x=1
			if (linearDistance < 0f) {
				var x0Slope = parameters.A > 0f ? parameters.B / parameters.A : (parameters.C > 0f ? parameters.D / parameters.C : 1f);
				return T.Interpolate(start, end, x0Slope * linearDistance);
			}
			if (linearDistance > 1f) {
				var secondCoordYMirrored = 1f - parameters.C;
				var firstCoordYMirrored = 1f - parameters.A;
				var x1Slope = secondCoordYMirrored > 0f ? (1f - parameters.D) / secondCoordYMirrored : (firstCoordYMirrored > 0f ? (1f - parameters.B) / firstCoordYMirrored : 1f);
				return T.Interpolate(start, end, FusedMultiplyAdd(x1Slope, linearDistance, 1f - x1Slope));
			}

			var cx = 3f * parameters.A;
			var bx = 3f * (parameters.C - parameters.A) - cx;
			var ax = (1f - cx) - bx;

			var cy = 3f * parameters.B;
			var by = 3f * (parameters.D - parameters.B) - cy;
			var ay = (1f - cy) - by;
			
			var threeAx = ax * 3f;
			var twoBx = bx * 2f;

			var solution = linearDistance;
			for (var i = 0; i < MaxNewtonRaphsonIterations; ++i) {
				var xAxisDistanceFromRoot = FusedMultiplyAdd(FusedMultiplyAdd(FusedMultiplyAdd(ax, solution, bx), solution, cx), solution, -linearDistance);
				if (MathF.Abs(xAxisDistanceFromRoot) < AcceptableErrorMargin) goto solved;
				var derivative = FusedMultiplyAdd(FusedMultiplyAdd(threeAx, solution, twoBx), solution, cx);
				if (MathF.Abs(derivative) < MinNewtonRaphsonGradient) break;
				solution -= xAxisDistanceFromRoot * ReciprocalEstimate(derivative);
			}

			solution = linearDistance;
			var binarySearchBoundsMin = 0f;
			var binarySearchBoundsMax = 1f;
			for (var i = 0; i < MaxBinarySearchIterations; ++i) {
				var x = FusedMultiplyAdd(FusedMultiplyAdd(ax, solution, bx), solution, cx) * solution;
				if (MathF.Abs(x - linearDistance) < AcceptableErrorMargin) goto solved;
				if (linearDistance > x) binarySearchBoundsMin = solution;
				else binarySearchBoundsMax = solution;
				solution = (binarySearchBoundsMin + binarySearchBoundsMax) * 0.5f;
			}

			solved:
			return T.Interpolate(start, end, FusedMultiplyAdd(FusedMultiplyAdd(ay, solution, by), solution, cy) * solution);
		}
		
		return new(
			&Algorithm, 
			new(
				Clamp(firstCoord.X, 0f, 1f), 
				firstCoord.Y, 
				Clamp(secondCoord.X, 0f, 1f), 
				secondCoord.Y
			)
		);
	}
	#endregion
}