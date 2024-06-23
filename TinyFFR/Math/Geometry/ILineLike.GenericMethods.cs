// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

// TODO we could probably auto-generate a lot of this with a source generator, so I've just implemented one example and anything else we actually need for now
// TODO should we do the same for IConvexShape?
// Implementation note: Q: "Why aren't these extension methods?" A: Because overload resolution doesn't work so well (and probably couldn't) when we're clashing extensions and trait interfaces and so on.
// Better to keep things simple this time (trust me, I tried, go look at commit hash 762dc61f6d9f90def70088d2a389ee7923e4495c to see how this looked originally and see how nasty ILine.cs and GeometryTraits.cs was).
// Unless you're some super genius with this stuff and want to have a go... Free cookie for you!
public partial interface ILineLike {
	public static Angle AngleTo<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike => @this.Direction.AngleTo(arg.Direction);
	
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

	public static Location? IntersectionWith<TThis, TArg>(TThis @this, TArg arg) where TThis : ILineLike where TArg : ILineLike => IntersectionWith(@this, arg, DefaultLineThickness);
	public static Location? IntersectionWith<TThis, TArg>(TThis @this, TArg arg, float lineThickness) where TThis : ILineLike where TArg : ILineLike {
		var closestPointOnLine = ClosestPointOn(@this, arg);
		return @this.DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}
}