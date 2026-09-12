// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Vertex = Egodystonic.TinyFFR.XYPair<float>;
using Edge = Egodystonic.TinyFFR.Pair<Egodystonic.TinyFFR.XYPair<float>, Egodystonic.TinyFFR.XYPair<float>>;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a simple (non-self-intersecting) 2D polygon defined by an ordered list of <see cref="Vertices"/>.
/// </summary>
/// <remarks>
/// The vertices are expected to form a single closed loop in the order given, with an edge implicitly connecting the last vertex back to the first. The polygon does not need to be convex, but no two edges may cross each other.
/// </remarks>
public readonly ref partial struct Polygon2D : IToleranceEquatable<Polygon2D> {
	// readonly float _containmentRadius;
	// readonly float _containmentRadiusSquared;

	/// <summary>
	/// The vertices of this polygon, in the order they appear as you travel around its perimeter.
	/// </summary>
	public ReadOnlySpan<Vertex> Vertices { get; }
	/// <summary>
	/// Whether <see cref="Vertices"/> are wound clockwise (<see langword="true"/>) or anticlockwise (<see langword="false"/>), as seen in the standard 2D orientation (X to the right, Y up).
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
	/// Constructs a new <see cref="Polygon2D"/> from <paramref name="vertices"/>, assuming they are wound anticlockwise (see <see cref="Polygon.DefaultClockwiseExpectation"/>).
	/// </summary>
	/// <param name="vertices">The polygon's vertices, in order around its perimeter.</param>
	public Polygon2D(ReadOnlySpan<Vertex> vertices) : this(vertices, isWoundClockwise: Polygon.DefaultClockwiseExpectation) { }
	/// <summary>
	/// Constructs a new <see cref="Polygon2D"/> from <paramref name="vertices"/> and <paramref name="isWoundClockwise"/>.
	/// </summary>
	/// <param name="vertices">The polygon's vertices, in order around its perimeter.</param>
	/// <param name="isWoundClockwise">Whether <paramref name="vertices"/> are wound clockwise or anticlockwise, as seen in the standard 2D orientation (X to the right, Y up).</param>
	public Polygon2D(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise) : this(vertices, isWoundClockwise, skipPrecalculations: true) { }

	internal Polygon2D(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise, bool skipPrecalculations) {
		Vertices = vertices;
		IsWoundClockwise = isWoundClockwise;

		// if (skipPrecalculations) {
		// 	_containmentRadius = _containmentRadiusSquared = Single.PositiveInfinity;
		// 	return;
		// }
		// foreach (var vertex in vertices) _containmentRadiusSquared = MathF.Max(_containmentRadiusSquared, vertex.LengthSquared);
		// _containmentRadius = MathF.Sqrt(_containmentRadiusSquared);
	}

	#region Factories and Conversions
	// public static Polygon2D FromVerticesWithGeometricPrecalculations(ReadOnlySpan<Vertex> vertices) => FromVerticesWithGeometricPrecalculations(vertices, isWoundClockwise: Polygon.DefaultClockwiseExpectation);
	// public static Polygon2D FromVerticesWithGeometricPrecalculations(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise) => new(vertices, isWoundClockwise, skipPrecalculations: false);
	#endregion

	#region Equality
	/// <inheritdoc/>
	public bool Equals(Polygon2D other) => IsWoundClockwise == other.IsWoundClockwise && Vertices.SequenceEqual(other.Vertices);

	/// <inheritdoc/>
	public bool Equals(Polygon2D other, float tolerance) {
		if (IsWoundClockwise != other.IsWoundClockwise) return false;
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
	public static bool operator ==(Polygon2D left, Polygon2D right) => left.Equals(right);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Polygon2D left, Polygon2D right) => !left.Equals(right);
	/// <summary>
	/// Always returns <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="Polygon2D"/> is a <see langword="ref struct"/>, so it can never actually be boxed to <see cref="object"/> — this override exists only to satisfy the compiler's requirement to override <see cref="ValueType.Equals(object?)"/>, and is unreachable in practice. Use <see cref="Equals(Polygon2D)"/> instead.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => false;
	/// <inheritdoc/>
	public override int GetHashCode() {
		var result = new HashCode();
		foreach (var vertex in Vertices) result.Add(vertex.GetHashCode());
		return result.ToHashCode();
	}
	#endregion

	/// <inheritdoc/>
	public override string ToString() => $"Polygon2D ({VertexCount} vertices)";
}