// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

// Implementation note: Q: "Why aren't these extension methods?" A: Because overload resolution doesn't work so well (and probably couldn't) when we're clashing extensions and trait interfaces and so on.
// Better to keep things simple this time (trust me, I tried, go look at commit hash 762dc61f6d9f90def70088d2a389ee7923e4495c to see how this looked originally and see how nasty ILine.cs and GeometryTraits.cs was).
// Unless you're some super genius with this stuff and want to have a go... Free cookie for you!
public partial interface ILineLike {
	/// <summary>
	/// Calculates the (unsigned) angle between <paramref name="this"/> and <paramref name="arg"/>'s directions.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	public static Angle AngleTo<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike => @this.Direction.AngleTo(arg.Direction);
	/// <summary>
	/// Calculates the angle formed between <paramref name="this"/> and <paramref name="arg"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from <paramref name="this"/> to <paramref name="arg"/> will be reported with a positive value.</param>
	public static Angle SignedAngleTo<TThis, TArg>(TThis @this, TArg arg, Direction clockwiseAxis) where TThis : ILineLike where TArg : ILineLike => @this.Direction.SignedAngleTo(arg.Direction, clockwiseAxis);

	/// <summary>
	/// Returns the point on <paramref name="arg"/> that is closest to <paramref name="this"/>.
	/// </summary>
	/// <typeparam name="TThis">The type of the line-like to measure from.</typeparam>
	/// <typeparam name="TArg">The type of the line-like to find the closest point on.</typeparam>
	/// <param name="this">The line-like to measure from.</param>
	/// <param name="arg">The line-like to find the closest point on.</param>
	public static Location ClosestPointOn<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike {
		return arg switch {
			Line line => @this.ClosestPointOn(line),
			Ray ray => @this.ClosestPointOn(ray),
			BoundedRay boundedRay => @this.ClosestPointOn(boundedRay),
			_ when arg.IsUnboundedInBothDirections => @this.ClosestPointOn(arg.CoerceToLine()),
			_ when arg.IsFiniteLength => @this.ClosestPointOn(arg.CoerceToBoundedRay(arg.Length.Value)),
			_ => @this.ClosestPointOn(arg.CoerceToRay()),
		};
	}

	/// <summary>
	/// Calculates the distance between <paramref name="this"/> and <paramref name="arg"/>.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	public static float DistanceFrom<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike {
		return arg switch {
			Line line => @this.DistanceFrom(line),
			Ray ray => @this.DistanceFrom(ray),
			BoundedRay boundedRay => @this.DistanceFrom(boundedRay),
			_ when arg.IsUnboundedInBothDirections => @this.DistanceFrom(arg.CoerceToLine()),
			_ when arg.IsFiniteLength => @this.DistanceFrom(arg.CoerceToBoundedRay(arg.Length.Value)),
			_ => @this.DistanceFrom(arg.CoerceToRay()),
		};
	}

	/// <summary>
	/// Calculates the intersection between <paramref name="this"/> and <paramref name="arg"/>, if any, using <see cref="DefaultLineThickness"/>.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	public static Location? IntersectionWith<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike => IntersectionWith(@this, arg, DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between <paramref name="this"/> and <paramref name="arg"/>, if any.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting. See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	public static Location? IntersectionWith<TThis, TArg>(TThis @this, TArg arg, float lineThickness) where TThis : ILineLike where TArg : ILineLike {
		var closestPointOnLine = ClosestPointOn(@this, arg);
		return @this.DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith{TThis,TArg}(TThis,TArg)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="this"/> and <paramref name="arg"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	public static Location FastIntersectionWith<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike => FastIntersectionWith(@this, arg, DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith{TThis,TArg}(TThis,TArg,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="this"/> and <paramref name="arg"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror <see cref="IntersectionWith{TThis,TArg}(TThis,TArg,float)"/>.</param>
	public static Location FastIntersectionWith<TThis, TArg>(TThis @this, TArg arg, float lineThickness) where TThis : ILineLike where TArg : ILineLike => ClosestPointOn(@this, arg);


	/// <summary>
	/// Determines whether <paramref name="this"/> is colinear with <paramref name="arg"/>, within <see cref="DefaultLineThickness"/> and <see cref="DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	public static bool IsApproximatelyColinearWith<TThis, TArg>(TThis @this, TArg arg) where TThis : struct, ILineLike<TThis> where TArg : struct, ILineLike<TArg> => IsApproximatelyColinearWith(@this, arg, DefaultLineThickness, DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <summary>
	/// Determines whether <paramref name="this"/> is colinear with <paramref name="arg"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	public static bool IsApproximatelyColinearWith<TThis, TArg>(TThis @this, TArg arg, float lineThickness, Angle tolerance) where TThis : struct, ILineLike<TThis> where TArg : struct, ILineLike<TArg> {
		return @this.IsApproximatelyParallelTo(arg.Direction, tolerance) && DistanceFrom(@this, arg) <= (lineThickness * 2f);
	}
	/// <summary>
	/// Determines whether <paramref name="this"/> is exactly colinear with <paramref name="arg"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <typeparam name="TThis">The type of the first line-like.</typeparam>
	/// <typeparam name="TArg">The type of the second line-like.</typeparam>
	/// <param name="this">The first line-like.</param>
	/// <param name="arg">The second line-like.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear. See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	public static bool IsExactlyColinearWith<TThis, TArg>(TThis @this, TArg arg, float lineThickness) where TThis : struct, ILineLike<TThis> where TArg : struct, ILineLike<TArg> {
		return @this.IsParallelTo(arg.Direction) && DistanceFrom(@this, arg) <= (lineThickness * 2f);
	}
}