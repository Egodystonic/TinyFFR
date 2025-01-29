// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

// TODO xmldoc that the vertices are expected to form a complete enclosed polygon.
// TODO They should be specified in order they appear around the polygon, with the last and first comprising the final edge that closes the polygon.
// TODO Does not need to be convex, but no edges may intersect.
// TODO Officially this is called a simple polygon
public readonly ref partial struct Polygon : IToleranceEquatable<Polygon> {
	readonly bool _isWoundClockwise;

	public ReadOnlySpan<Location> Vertices { get; }
	public Direction Normal { get; }

	public int VertexCount => Vertices.Length;
	public int EdgeCount => VertexCount switch {
		<= 1 => 0,
		2 => 1,
		_ => VertexCount
	};
	public int TriangleCount => Int32.Max(0, VertexCount - 2);
	
	public Polygon(ReadOnlySpan<Location> vertices) : this(vertices, CalculateMostLikelyNormal(vertices)) { }
	public Polygon(ReadOnlySpan<Location> vertices, Direction normal) : this(vertices, normal, isWoundClockwise: true) { }
	public Polygon(ReadOnlySpan<Location> vertices, Direction normal, bool isWoundClockwise) {
		Vertices = vertices;
		Normal = normal;
		_isWoundClockwise = isWoundClockwise;
	}

	public static Direction CalculateMostLikelyNormal(ReadOnlySpan<Location> vertices) { // TODO in the unit test we need to make sure this always gives us an answer with clockwise winding wrt the normal (e.g. the normal faces out of the clockwise-wound polygon)
		if (vertices.Length < 3) throw new ArgumentException("Can not calculate most-likely normal for polygon with fewer than 3 vertices.", nameof(vertices));

		var firstCandidate = Direction.None;
		var secondCandidate = Direction.None;
		var firstCandidateCount = 0;
		var secondCandidateCount = 0;

		for (var i = 2; i < vertices.Length; ++i) {
			var potentialPlane = Plane.FromTriangleOnSurface(vertices[i - 2], vertices[i - 1], vertices[i]);
			if (potentialPlane is not { } plane) continue;

			if (firstCandidate == Direction.None) {
				firstCandidate = plane.Normal;
				firstCandidateCount++;
			}
			else if (firstCandidate.AngleTo(plane.Normal) < Angle.QuarterCircle) firstCandidateCount++;
			else {
				if (secondCandidate == Direction.None) secondCandidate = plane.Normal;
				secondCandidateCount++;
			}
		}

		return firstCandidateCount > secondCandidateCount ? firstCandidate : secondCandidate;
	}

	#region Equality
	public bool Equals(Polygon other) => Normal.Equals(other.Normal) && Vertices.SequenceEqual(other.Vertices);

	public bool Equals(Polygon other, float tolerance) {
		if (!Normal.Equals(other.Normal, tolerance)) return false;
		
		var thisVertices = Vertices;
		var otherVertices = other.Vertices;

		if (thisVertices.Length != otherVertices.Length) return false;

		for (var i = 0; i < thisVertices.Length; ++i) {
			if (!thisVertices[i].Equals(otherVertices[i], tolerance)) return false;
		}

		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Polygon left, Polygon right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Polygon left, Polygon right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => false;
	public override int GetHashCode() {
		var result = new HashCode();
		foreach (var vertex in Vertices) result.Add(vertex.GetHashCode());
		result.Add(Normal.GetHashCode());
		return result.ToHashCode();
	}
	#endregion
}