// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Vertex = Egodystonic.TinyFFR.XYPair<float>;
using Edge = Egodystonic.TinyFFR.Pair<Egodystonic.TinyFFR.XYPair<float>, Egodystonic.TinyFFR.XYPair<float>>;

namespace Egodystonic.TinyFFR;

public readonly ref partial struct Polygon2D : IToleranceEquatable<Polygon2D> {
	readonly float _containmentRadius;
	readonly float _containmentRadiusSquared;
	readonly bool _isWoundClockwise;
	
	public ReadOnlySpan<Vertex> Vertices { get; }

	public int VertexCount => Vertices.Length;
	public int EdgeCount => VertexCount switch {
		<= 1 => 0,
		2 => 1,
		_ => VertexCount
	};
	public int TriangleCount => Int32.Max(0, VertexCount - 2);

	public Polygon2D(ReadOnlySpan<Vertex> vertices) : this(vertices, isWoundClockwise: true) { }
	public Polygon2D(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise) : this(vertices, isWoundClockwise, skipPrecalculations: true) { }

	// TODO xmldoc that the vertices are expected to form a complete enclosed polygon.
	// TODO They should be specified in order they appear around the polygon, with the last and first comprising the final edge that closes the polygon.
	// TODO Does not need to be convex, but no edges may intersect.
	// TODO Officially this is called a simple polygon
	internal Polygon2D(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise, bool skipPrecalculations) {
		Vertices = vertices;
		_isWoundClockwise = isWoundClockwise;

		if (skipPrecalculations) {
			_containmentRadius = _containmentRadiusSquared = Single.PositiveInfinity;
			return;
		}
		foreach (var vertex in vertices) _containmentRadiusSquared = MathF.Max(_containmentRadiusSquared, vertex.LengthSquared);
		_containmentRadius = MathF.Sqrt(_containmentRadiusSquared);
	}

	#region Factories and Conversions
	public static Polygon2D FromVerticesWithGeometricPrecalculations(ReadOnlySpan<Vertex> vertices) => FromVerticesWithGeometricPrecalculations(vertices, isWoundClockwise: true);
	public static Polygon2D FromVerticesWithGeometricPrecalculations(ReadOnlySpan<Vertex> vertices, bool isWoundClockwise) => new(vertices, isWoundClockwise, skipPrecalculations: false);
	#endregion

	#region Equality
	public bool Equals(Polygon2D other) => Vertices.SequenceEqual(other.Vertices);

	public bool Equals(Polygon2D other, float tolerance) {
		var thisVertices = Vertices;
		var otherVertices = other.Vertices;

		if (thisVertices.Length != otherVertices.Length) return false;

		for (var i = 0; i < thisVertices.Length; ++i) {
			if (!thisVertices[i].Equals(otherVertices[i], tolerance)) return false;
		}

		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Polygon2D left, Polygon2D right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Polygon2D left, Polygon2D right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => false;
	public override int GetHashCode() {
		var result = new HashCode();
		foreach (var vertex in Vertices) result.Add(vertex.GetHashCode());
		return result.ToHashCode();
	}
	#endregion
}