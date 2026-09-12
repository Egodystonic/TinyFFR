// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a flat, simple (non-self-intersecting) polygon defined by an ordered list of <see cref="Vertices"/>.
/// </summary>
/// <remarks>
/// The vertices are expected to be coplanar and to form a single closed loop in the order given, with an edge implicitly connecting the last vertex back to the first. The polygon does not need to be convex, but no two edges may cross each other.
/// </remarks>
public readonly ref partial struct Polygon : IToleranceEquatable<Polygon> {
	/// <summary>
	/// The value used for <see cref="IsWoundClockwise"/> by the constructor overloads that don't specify it explicitly (i.e. <see langword="false"/>, meaning <see cref="Vertices"/> are assumed to be wound anticlockwise as seen from the side <see cref="Normal"/> points towards).
	/// </summary>
	public const bool DefaultClockwiseExpectation = false;

	/// <summary>
	/// The vertices of this polygon, in the order they appear as you travel around its perimeter.
	/// </summary>
	public ReadOnlySpan<Location> Vertices { get; }
	/// <summary>
	/// The direction this polygon's front face points towards.
	/// </summary>
	public Direction Normal { get; }
	/// <summary>
	/// Whether <see cref="Vertices"/> are wound clockwise (<see langword="true"/>) or anticlockwise (<see langword="false"/>) as seen from the side <see cref="Normal"/> points towards.
	/// </summary>
	public bool IsWoundClockwise { get; }

	/// <summary>
	/// The number of vertices in this polygon; equivalent to <c><see cref="Vertices"/>.Length</c>.
	/// </summary>
	public int VertexCount => Vertices.Length;
	/// <summary>
	/// The number of edges in this polygon.
	/// </summary>
	public int EdgeCount => VertexCount switch {
		<= 1 => 0,
		2 => 1,
		_ => VertexCount
	};
	/// <summary>
	/// The number of triangles this polygon would be divided into by triangulation.
	/// </summary>
	public int TriangleCount => Int32.Max(0, VertexCount - 2);

	/// <summary>
	/// Constructs a new <see cref="Polygon"/> from <paramref name="vertices"/>, deriving <see cref="Normal"/> automatically and assuming <paramref name="vertices"/> are wound anticlockwise as seen from the front (see <see cref="DefaultClockwiseExpectation"/>).
	/// </summary>
	/// <param name="vertices">The polygon's vertices, in order around its perimeter.</param>
	public Polygon(ReadOnlySpan<Location> vertices) : this(vertices, CalculateNormalForAnticlockwiseCoplanarVertices(vertices)) { }
	/// <summary>
	/// Constructs a new <see cref="Polygon"/> from <paramref name="vertices"/> and <paramref name="normal"/>, assuming <paramref name="vertices"/> are wound anticlockwise as seen from the side <paramref name="normal"/> points towards (see <see cref="DefaultClockwiseExpectation"/>).
	/// </summary>
	/// <param name="vertices">The polygon's vertices, in order around its perimeter.</param>
	/// <param name="normal">The direction this polygon's front face points towards.</param>
	public Polygon(ReadOnlySpan<Location> vertices, Direction normal) : this(vertices, normal, isWoundClockwise: DefaultClockwiseExpectation) { }
	/// <summary>
	/// Constructs a new <see cref="Polygon"/> from <paramref name="vertices"/>, <paramref name="normal"/> and <paramref name="isWoundClockwise"/>.
	/// </summary>
	/// <param name="vertices">The polygon's vertices, in order around its perimeter.</param>
	/// <param name="normal">The direction this polygon's front face points towards.</param>
	/// <param name="isWoundClockwise">Whether <paramref name="vertices"/> are wound clockwise or anticlockwise as seen from the side <paramref name="normal"/> points towards.</param>
	public Polygon(ReadOnlySpan<Location> vertices, Direction normal, bool isWoundClockwise) {
		Vertices = vertices;
		Normal = normal;
		IsWoundClockwise = isWoundClockwise;
	}

	/// <summary>
	/// Calculates the most likely normal for a polygon with the given <paramref name="vertices"/>, assuming they are coplanar and wound anticlockwise as seen from the front.
	/// </summary>
	/// <remarks>
	/// This works by sampling the normal formed by consecutive triples of vertices (via <see cref="Plane.FromTriangleOnSurface(Location,Location,Location)"/>) and returning whichever of the (at most two) distinct normals found was sampled the most — this makes the result robust to a handful of duplicate/degenerate vertices, but assumes the vertices are otherwise genuinely coplanar; if they are not, the result is not meaningful.
	/// </remarks>
	/// <param name="vertices">The candidate polygon's vertices, in order around its perimeter. Must contain at least three vertices.</param>
	public static Direction CalculateNormalForAnticlockwiseCoplanarVertices(ReadOnlySpan<Location> vertices) {
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
	/// <inheritdoc/>
	public bool Equals(Polygon other) => Normal.Equals(other.Normal) && IsWoundClockwise.Equals(other.IsWoundClockwise) && Vertices.SequenceEqual(other.Vertices);

	/// <inheritdoc/>
	public bool Equals(Polygon other, float tolerance) {
		if (IsWoundClockwise != other.IsWoundClockwise) return false;
		if (!Normal.Equals(other.Normal, tolerance)) return false;

		var thisVertices = Vertices;
		var otherVertices = other.Vertices;

		if (thisVertices.Length != otherVertices.Length) return false;

		for (var i = 0; i < thisVertices.Length; ++i) {
			if (!thisVertices[i].Equals(otherVertices[i], tolerance)) return false;
		}

		return true;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Polygon left, Polygon right) => left.Equals(right);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Polygon left, Polygon right) => !left.Equals(right);
	/// <summary>
	/// Always returns <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="Polygon"/> is a <see langword="ref struct"/>, so it can never actually be boxed to <see cref="object"/> — this override exists only to satisfy the compiler's requirement to override <see cref="ValueType.Equals(object?)"/>, and is unreachable in practice. Use <see cref="Equals(Polygon)"/> instead.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => false;
	/// <inheritdoc/>
	public override int GetHashCode() {
		var result = new HashCode();
		foreach (var vertex in Vertices) result.Add(vertex.GetHashCode());
		result.Add(Normal.GetHashCode());
		result.Add(IsWoundClockwise.GetHashCode());
		return result.ToHashCode();
	}
	#endregion

	/// <inheritdoc/>
	public override string ToString() => $"Polygon ({VertexCount} vertices)";
}