// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;
using static System.Single;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Selects how pronounced an easing effect should be, for the <see cref="InterpolationAlgorithm{T}"/> factory methods that accept it.
/// </summary>
/// <remarks>
/// The exact effect of each value depends on the algorithm it is passed to; consult the specific factory method's documentation for details.
/// </remarks>
public enum InterpolationStrength {
	/// <summary>
	/// No (or minimal) additional easing effect.
	/// </summary>
	None = 0,
	/// <summary>
	/// A very subtle easing effect.
	/// </summary>
	VeryMild,
	/// <summary>
	/// A subtle easing effect.
	/// </summary>
	Mild,
	/// <summary>
	/// A moderate easing effect. This is the default used when no explicit strength is specified.
	/// </summary>
	Moderate,
	/// <summary>
	/// A pronounced easing effect.
	/// </summary>
	Strong,
	/// <summary>
	/// A very pronounced easing effect.
	/// </summary>
	VeryStrong
}

#pragma warning disable CA1815 // "Should override Equals" -- Can't meaningfully compare function pointers
/// <summary>
/// Represents a specific interpolation function that can be evaluated between a start and end value of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// Obtain an instance from one of the static factory methods (e.g. <see cref="Linear"/>, <see cref="AccelerateFromSlow(InterpolationStrength)"/>, <see cref="CubicBezier"/>, etc),
/// or supply your own function via <see cref="Custom"/>.
/// </remarks>
/// <typeparam name="T">The type of value being interpolated between.</typeparam>
public readonly unsafe struct InterpolationAlgorithm<T> where T : IInterpolatable<T> {
#pragma warning restore CA1815
#pragma warning disable CA1034 // "Do not nest publicly-visible types" -- I prefer it like this
	/// <summary>
	/// A small, fixed-size group of up to four <see cref="float"/> parameters, passed alongside the algorithm function supplied to <see cref="Custom"/>.
	/// </summary>
	/// <remarks>
	/// This exists so that an <see cref="InterpolationAlgorithm{T}"/> can carry its own per-instance configuration (for example, an exponent or a curve control point) without allocating. Unused parameters default to <c>0f</c>.
	/// </remarks>
	/// <param name="A">The first parameter.</param>
	/// <param name="B">The second parameter.</param>
	/// <param name="C">The third parameter.</param>
	/// <param name="D">The fourth parameter.</param>
	public readonly record struct StaticParameterGroup(float A, float B, float C, float D) {
		/// <summary>
		/// Constructs a new <see cref="StaticParameterGroup"/> with only <see cref="A"/> set; <see cref="B"/>, <see cref="C"/> and <see cref="D"/> default to <c>0f</c>.
		/// </summary>
		/// <param name="a">The value for <see cref="A"/>.</param>
		public StaticParameterGroup(float a) : this(a, 0f, 0f, 0f) {}
		/// <summary>
		/// Constructs a new <see cref="StaticParameterGroup"/> with only <see cref="A"/> and <see cref="B"/> set; <see cref="C"/> and <see cref="D"/> default to <c>0f</c>.
		/// </summary>
		/// <param name="a">The value for <see cref="A"/>.</param>
		/// <param name="b">The value for <see cref="B"/>.</param>
		public StaticParameterGroup(float a, float b) : this(a, b, 0f, 0f) {}
		/// <summary>
		/// Constructs a new <see cref="StaticParameterGroup"/> with only <see cref="A"/>, <see cref="B"/> and <see cref="C"/> set; <see cref="D"/> defaults to <c>0f</c>.
		/// </summary>
		/// <param name="a">The value for <see cref="A"/>.</param>
		/// <param name="b">The value for <see cref="B"/>.</param>
		/// <param name="c">The value for <see cref="C"/>.</param>
		public StaticParameterGroup(float a, float b, float c) : this(a, b, c, 0f) {}
	}
#pragma warning restore CA1034
	readonly delegate* managed<T, T, StaticParameterGroup, float, T> _algorithmPtr;
	readonly StaticParameterGroup _parameters;

	/// <summary>
	/// Constructs a new <see cref="InterpolationAlgorithm{T}"/> equivalent to <see cref="Linear"/>.
	/// </summary>
	/// <remarks>
	/// This is different to <see langword="default"/>(<see cref="InterpolationAlgorithm{T}"/>), which is an uninitialized, invalid instance that throws when used;
	/// always prefer this constructor or one of the static factory methods.
	/// </remarks>
	public InterpolationAlgorithm() => this = Linear();
	InterpolationAlgorithm(delegate* managed<T, T, StaticParameterGroup, float, T> algorithmPtr, StaticParameterGroup parameters) {
		ArgumentNullException.ThrowIfNull(algorithmPtr);
		_algorithmPtr = algorithmPtr;
		_parameters = parameters;
	}

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that evaluates an algorithmic function you supply yourself.
	/// </summary>
	/// <remarks>
	/// A function pointer is used (rather than a delegate) so that <see cref="InterpolationAlgorithm{T}"/> remains allocation-free.
	/// </remarks>
	/// <param name="algorithmPtr">The function to invoke to evaluate the curve, given the start value, end value, <paramref name="parameters"/>, and a linear distance.</param>
	/// <param name="parameters">Up to four <see cref="float"/> parameters passed through to <paramref name="algorithmPtr"/> on every invocation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static InterpolationAlgorithm<T> Custom(delegate* managed<T, T, StaticParameterGroup, float, T> algorithmPtr, StaticParameterGroup parameters) => new(algorithmPtr, parameters);

	/// <summary>
	/// Evaluates this algorithm, interpolating between <paramref name="startValue"/> and <paramref name="endValue"/>.
	/// This is a convenience overload of <see cref="GetValue(T,T,float)"/> and is equivalent to calling <c>GetValue(startValue, endValue, current / end)</c>.
	/// </summary>
	/// <remarks>
	/// If <paramref name="end"/> is <c>0f</c> (or the division would not otherwise produce a finite result), a linear distance of <c>1f</c> is used instead, i.e. <paramref name="endValue"/> is returned.
	/// </remarks>
	/// <param name="startValue">The starting value (returned when <c>current == 0</c>).</param>
	/// <param name="endValue">The ending value (returned when <c>current == end</c>).</param>
	/// <param name="current">The amount of progress made so far in your arbitrary operation.</param>
	/// <param name="end">The total amount of progress required to reach <paramref name="endValue"/>.</param>
	public T GetValue(T startValue, T endValue, float current, float end) {
		var linearDistance = current * ReciprocalEstimate(end);
		return GetValue(startValue, endValue, IsFinite(linearDistance) ? linearDistance : 1f);
	}
	/// <summary>
	/// Evaluates this algorithm, interpolating between <paramref name="startValue"/> and <paramref name="endValue"/>.
	/// </summary>
	/// <param name="startValue">The starting value (returned when <c>linearDistance == 0f</c>).</param>
	/// <param name="endValue">The ending value (returned when <c>linearDistance == 1f</c>).</param>
	/// <param name="linearDistance">The normalized position along the curve to evaluate (e.g. the 'x' value on a plot of the curve).
	/// Most algorithms accept values outside <c>[0, 1]</c> and will extrapolate beyond <paramref name="startValue"/>/<paramref name="endValue"/> accordingly;
	/// see the remarks on the specific factory method used to create this instance.</param>
	public T GetValue(T startValue, T endValue, float linearDistance) {
		ThrowIfNullAlgorithm();
		return UnsafeGetValueSkipNullCheck(startValue, endValue, linearDistance);
	}
	
	internal void ThrowIfNullAlgorithm() {
		if (_algorithmPtr == null) throw InvalidObjectException.InvalidDefault<InterpolationAlgorithm<T>>();
	} 
	
	internal T UnsafeGetValueSkipNullAndDividendCheck(T startValue, T endValue, float currentValue, float targetValue) {
		return UnsafeGetValueSkipNullCheck(startValue, endValue, currentValue * ReciprocalEstimate(targetValue));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal T UnsafeGetValueSkipNullCheck(T startValue, T endValue, float linearDistance) {
		return _algorithmPtr(startValue, endValue, _parameters, linearDistance);
	}

	#region Algorithms
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> with no easing effect: progress is directly proportional to <c>linearDistance</c>.
	/// </summary>
	/// <remarks>
	/// This gives the exact same result as invoking <see cref="IInterpolatable{T}.Interpolate"/> directly. 
	/// </remarks>
	public static InterpolationAlgorithm<T> Linear() {
		static T Algorithm(T start, T end, StaticParameterGroup parameters, float linearDistance) {
			return T.Interpolate(start, end, linearDistance);
		}
		return new(&Algorithm, new());
	}

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that starts slowly and accelerates towards <c>endValue</c> as <c>linearDistance</c> approaches <c>1f</c> (commonly known as an "ease-in" curve).
	/// </summary>
	/// <remarks>
	/// The <paramref name="strength"/> parameter maps to the <c>exponent</c> argument of <see cref="AccelerateFromSlow(float)"/> as follows:
	/// <ul>
	/// <li><see cref="InterpolationStrength.None"/> =&gt; <c>1.0</c></li>
	/// <li><see cref="InterpolationStrength.VeryMild"/> =&gt; <c>1.3</c></li>
	/// <li><see cref="InterpolationStrength.Mild"/> =&gt; <c>1.75</c></li>
	/// <li><see cref="InterpolationStrength.Moderate"/> =&gt; <c>2</c></li>
	/// <li><see cref="InterpolationStrength.Strong"/> =&gt; <c>3</c></li>
	/// <li><see cref="InterpolationStrength.VeryStrong"/> =&gt; <c>5</c></li>
	/// </ul>
	/// </remarks>
	/// <param name="strength">How pronounced the acceleration effect should be.</param>
	/// <seealso cref="AccelerateFromSlow(float)"/>
	public static InterpolationAlgorithm<T> AccelerateFromSlow(InterpolationStrength strength = InterpolationStrength.Moderate) => AccelerateFromSlow(strength switch {
		InterpolationStrength.None => 1f,
		InterpolationStrength.VeryMild => 1.3f,
		InterpolationStrength.Mild => 1.75f,
		InterpolationStrength.Strong => 3f,
		InterpolationStrength.VeryStrong => 5f,
		_ => 2f
	});
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that starts slowly and accelerates towards <c>endValue</c> as <c>linearDistance</c> approaches <c>1f</c> (commonly known as an "ease-in" curve).
	/// </summary>
	/// <param name="exponent">The exponent used to shape the curve. Higher values produce a more pronounced (slower-starting, faster-finishing) effect.
	/// An <paramref name="exponent"/> of <c>1f</c> is equivalent to <see cref="Linear"/>.</param>
	/// <seealso cref="AccelerateFromSlow(InterpolationStrength)"/>
	public static InterpolationAlgorithm<T> AccelerateFromSlow(float exponent) {
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

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that starts quickly and decelerates towards <c>endValue</c> as <c>linearDistance</c> approaches <c>1f</c> (commonly known as an "ease-out" curve).
	/// </summary>
	/// <remarks>
	/// The <paramref name="strength"/> parameter maps to the <c>exponent</c> argument of <see cref="DecelerateFromFast(float)"/> as follows:
	/// <ul>
	/// <li><see cref="InterpolationStrength.None"/> =&gt; <c>1.0</c></li>
	/// <li><see cref="InterpolationStrength.VeryMild"/> =&gt; <c>1.3</c></li>
	/// <li><see cref="InterpolationStrength.Mild"/> =&gt; <c>1.75</c></li>
	/// <li><see cref="InterpolationStrength.Moderate"/> =&gt; <c>2</c></li>
	/// <li><see cref="InterpolationStrength.Strong"/> =&gt; <c>3</c></li>
	/// <li><see cref="InterpolationStrength.VeryStrong"/> =&gt; <c>5</c></li>
	/// </ul>
	/// </remarks>
	/// <param name="strength">How pronounced the deceleration effect should be.</param>
	/// <seealso cref="DecelerateFromFast(float)"/>
	public static InterpolationAlgorithm<T> DecelerateFromFast(InterpolationStrength strength = InterpolationStrength.Moderate) => DecelerateFromFast(strength switch {
		InterpolationStrength.None => 1f,
		InterpolationStrength.VeryMild => 1.3f,
		InterpolationStrength.Mild => 1.75f,
		InterpolationStrength.Strong => 3f,
		InterpolationStrength.VeryStrong => 5f,
		_ => 2f
	});
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that starts quickly and decelerates towards <c>endValue</c> as <c>linearDistance</c> approaches <c>1f</c> (commonly known as an "ease-out" curve).
	/// </summary>
	/// <remarks>
	/// This is the mirror image of <see cref="AccelerateFromSlow(float)"/>:.
	/// </remarks>
	/// <param name="exponent">The exponent used to shape the curve. Higher values produce a more pronounced (faster-starting, slower-finishing) effect.
	/// An <paramref name="exponent"/> of <c>1f</c> is equivalent to <see cref="Linear"/>.</param>
	/// <seealso cref="DecelerateFromFast(InterpolationStrength)"/>
	public static InterpolationAlgorithm<T> DecelerateFromFast(float exponent) {
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

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that briefly moves past <c>startValue</c> in the opposite direction before accelerating forward to <c>endValue</c> (an anticipation/wind-up effect, sometimes called a "back ease-in" curve).
	/// </summary>
	/// <remarks>
	/// The <paramref name="strength"/> parameter maps to the <c>coefficient</c> argument of <see cref="AccelerateFromSlowWithInitialReverse(float)"/> as follows:
	/// <ul>
	/// <li><see cref="InterpolationStrength.None"/> =&gt; <c>0.0</c></li>
	/// <li><see cref="InterpolationStrength.VeryMild"/> =&gt; <c>1.0</c></li>
	/// <li><see cref="InterpolationStrength.Mild"/> =&gt; <c>1.3</c></li>
	/// <li><see cref="InterpolationStrength.Moderate"/> =&gt; <c>1.70158</c></li>
	/// <li><see cref="InterpolationStrength.Strong"/> =&gt; <c>2.2</c></li>
	/// <li><see cref="InterpolationStrength.VeryStrong"/> =&gt; <c>4</c></li>
	/// </ul>
	/// </remarks>
	/// <param name="strength">How pronounced the initial reverse and subsequent acceleration should be.</param>
	/// <seealso cref="AccelerateFromSlowWithInitialReverse(float)"/>
	public static InterpolationAlgorithm<T> AccelerateFromSlowWithInitialReverse(InterpolationStrength strength = InterpolationStrength.Moderate) => AccelerateFromSlowWithInitialReverse(strength switch {
		InterpolationStrength.None => 0f,
		InterpolationStrength.VeryMild => 1f,
		InterpolationStrength.Mild => 1.3f,
		InterpolationStrength.Strong => 2.2f,
		InterpolationStrength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% undershoot, from Robert Penner's algorithms
	});
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that briefly moves past <c>startValue</c> in the opposite direction before accelerating forward to <c>endValue</c> (an anticipation/wind-up effect, sometimes called a "back ease-in" curve).
	/// </summary>
	/// <param name="coefficient">Controls how far behind <c>startValue</c> the curve initially moves.
	/// A <paramref name="coefficient"/> of <c>0f</c> removes the initial reverse, leaving a plain acceleration curve; higher values produce a more pronounced reverse.</param>
	/// <seealso cref="AccelerateFromSlowWithInitialReverse(InterpolationStrength)"/>
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

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that overshoots past <c>endValue</c> before settling back to it (sometimes called a "back ease-out" curve).
	/// </summary>
	/// <remarks>
	/// The <paramref name="strength"/> parameter maps to the <c>coefficient</c> argument of <see cref="DecelerateFromFastWithOvershoot(float)"/> as follows:
	/// <ul>
	/// <li><see cref="InterpolationStrength.None"/> =&gt; <c>0.0</c></li>
	/// <li><see cref="InterpolationStrength.VeryMild"/> =&gt; <c>1.0</c></li>
	/// <li><see cref="InterpolationStrength.Mild"/> =&gt; <c>1.3</c></li>
	/// <li><see cref="InterpolationStrength.Moderate"/> =&gt; <c>1.70158</c></li>
	/// <li><see cref="InterpolationStrength.Strong"/> =&gt; <c>2.2</c></li>
	/// <li><see cref="InterpolationStrength.VeryStrong"/> =&gt; <c>4</c></li>
	/// </ul>
	/// </remarks>
	/// <param name="strength">How pronounced the overshoot should be.</param>
	/// <seealso cref="DecelerateFromFastWithOvershoot(float)"/>
	public static InterpolationAlgorithm<T> DecelerateFromFastWithOvershoot(InterpolationStrength strength = InterpolationStrength.Moderate) => DecelerateFromFastWithOvershoot(strength switch {
		InterpolationStrength.None => 0f,
		InterpolationStrength.VeryMild => 1f,
		InterpolationStrength.Mild => 1.3f,
		InterpolationStrength.Strong => 2.2f,
		InterpolationStrength.VeryStrong => 4f,
		_ => 1.70158f // Results in a ~10% overshoot, from Robert Penner's algorithms
	});
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that overshoots past <c>endValue</c> before settling back to it (sometimes called a "back ease-out" curve).
	/// </summary>
	/// <param name="coefficient">Controls how far ahead of <c>endValue</c> the curve overshoots before settling.
	/// A <paramref name="coefficient"/> of <c>0f</c> removes the overshoot, leaving a plain deceleration curve; higher values produce a more pronounced overshoot.</param>
	/// <seealso cref="DecelerateFromFastWithOvershoot(InterpolationStrength)"/>
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
	
	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> that eases in at the start and out at the end, producing a smooth, natural-looking motion (this is sometimes referred to as "smoothstep").
	/// </summary>
	/// <param name="additionalSmoothing">If <see langword="true"/>, uses an even smoother variant (sometimes referred to as "smootherstep") that further softens the very start and end of the motion.</param>
	public static InterpolationAlgorithm<T> Natural(bool additionalSmoothing = false) {
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

	/// <summary>
	/// Creates an <see cref="InterpolationAlgorithm{T}"/> defined by a cubic Bézier curve, using the same two-control-point convention as CSS's <c>cubic-bezier()</c> timing function.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The curve runs from <c>(0, 0)</c> to <c>(1, 1)</c>, with <paramref name="firstCoord"/> and <paramref name="secondCoord"/> as its two intermediate control points. Their X components are clamped to <c>[0, 1]</c> so the curve always represents a valid function of <c>linearDistance</c>, but their Y components are unrestricted and can be used to produce overshoot/undershoot effects.
	/// </para>
	/// <para>
	/// <c>linearDistance</c> values outside <c>[0, 1]</c> are supported and extrapolate linearly, continuing the curve's slope at whichever endpoint is closest.
	/// </para>
	/// <para>
	/// The website <a href="https://cubic-bezier.com">cubic-bezier.com</a> provides an interactive way to design and preview control points for this curve before using them here.
	/// </para>
	/// </remarks>
	/// <param name="firstCoord">The first control point.</param>
	/// <param name="secondCoord">The second control point.</param>
	public static InterpolationAlgorithm<T> CubicBezier(XYPair<float> firstCoord, XYPair<float> secondCoord) {
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