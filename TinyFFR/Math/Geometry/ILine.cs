// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ILine : IPointTestable, ILineTestable {
	public const float DefaultLineThickness = 0.01f;

	Location StartPoint { get; }
	Direction Direction { get; }
	bool IsUnboundedInBothDirections { get; }
	[MemberNotNull(nameof(LengthSquared), nameof(StartToEndVect), nameof(EndPoint))]
	float? Length { get; }
	[MemberNotNull(nameof(Length), nameof(StartToEndVect), nameof(EndPoint))]
	float? LengthSquared { get; }
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(EndPoint))]
	Vect? StartToEndVect { get; }
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(StartToEndVect))]
	Location? EndPoint { get; }

	bool Contains(Location location, float lineThickness);
	Location? GetIntersectionPointOn<TLine>(TLine line, float lineThickness) where TLine : ILine;

	protected static Location CalculateClosestPointToOtherLine<TThis, TOther>(TThis @this, TOther other) where TThis : ILine where TOther : ILine {
		const float ZeroEpsilon = 0.001f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		if (1f - MathF.Abs(dot) < ZeroEpsilon) {
			// When we enter this condition it essentially means the lines are parallel,
			// so we test the two/three/four endpoints against each other line and pick the closest.
			// We're effectively ignoring the unbounded flag because there's already technically infinite
			// answers anyway for an unbounded solution, so any answer is as good as any other.
			ReadOnlySpan<bool> testLocationAgainstOtherFlags = [true, true, false, false];
			ReadOnlySpan<Location?> testLocations = [@this.StartPoint, @this.EndPoint, other.StartPoint, other.EndPoint];
			Span<float?> distances = stackalloc float?[testLocations.Length];

			for (var i = 0; i < testLocations.Length; ++i) {
				var testLocation = testLocations[i];
				if (testLocation == null) continue;
				distances[i] = testLocationAgainstOtherFlags[i] ? other.DistanceFrom(testLocation.Value) : @this.DistanceFrom(testLocation.Value);
			}

			var indexOfLowestDistance = 0; // This assumes testLocations[0] is never null
			for (var i = 1; i < distances.Length; ++i) {
				if (distances[i] < distances[indexOfLowestDistance]) indexOfLowestDistance = i;
			}

			return testLocationAgainstOtherFlags[indexOfLowestDistance]
				? testLocations[indexOfLowestDistance]!.Value
				: @this.ClosestPointTo(testLocations[indexOfLowestDistance]!.Value);
		}
		else {
			var startDiff = thisStart - otherStart;
			var localOrientationStartDiffDot = Vector3.Dot(thisDir, startDiff);
			var otherOrientationStartDiffDot = Vector3.Dot(otherDir, startDiff);
			var oneMinusDotSquared = 1f - (dot * dot);

			var distFromThisStart = (dot * otherOrientationStartDiffDot - localOrientationStartDiffDot) / oneMinusDotSquared;
			if (distFromThisStart < 0f && !@this.IsUnboundedInBothDirections) return Location.FromVector3(thisStart);
			// @this.Length will be null if this is a Ray or unbounded Line, meaning this if check will always be false and we won't bind the result (which is good)
			else if (distFromThisStart > @this.Length) return @this.EndPoint.Value; 
			else return Location.FromVector3(thisStart + thisDir * distFromThisStart);
		}
	}
}
public interface ILine<TSelf> : ILine, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : ILine<TSelf> {

}