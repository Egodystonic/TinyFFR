// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR;

partial struct Polygon {
	/// <summary>
	/// The average of all of <see cref="Vertices"/>.
	/// </summary>
	public Location Centroid {
		get {
			var result = Vect.Zero;
			foreach (var vertex in Vertices) {
				result += (Vect) vertex;
			}
			return (Location) (result / VertexCount);
		}
	}

	/// <summary>
	/// Flattens this polygon down to a <see cref="Polygon2D"/>, using <see cref="Centroid"/> as the 2D coordinate space's origin.
	/// </summary>
	/// <param name="vertexDest">A buffer to write the converted 2D vertices into. Must be at least <see cref="Polygon.VertexCount"/> long; the returned <see cref="Polygon2D"/>'s vertices are a slice of this buffer, so it must remain valid for as long as the result is used.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest) => ToPolygon2D(vertexDest, Centroid);
	/// <summary>
	/// Flattens this polygon down to a <see cref="Polygon2D"/>, using <paramref name="originPoint"/> as the 2D coordinate space's origin and an arbitrarily-chosen (but consistent) pair of axes lying within this polygon's plane.
	/// </summary>
	/// <param name="vertexDest">A buffer to write the converted 2D vertices into. Must be at least <see cref="Polygon.VertexCount"/> long; the returned <see cref="Polygon2D"/>'s vertices are a slice of this buffer, so it must remain valid for as long as the result is used.</param>
	/// <param name="originPoint">The point (not necessarily coplanar with this polygon) whose closest point on this polygon's plane becomes the 2D coordinate space's origin.</param>
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, Location originPoint) {
		var zBasis = Normal;
		var xBasis = zBasis.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(zBasis, xBasis);
		var converter = new DimensionConverter(xBasis, yBasis, zBasis, originPoint);
		return ToPolygon2D(vertexDest, converter);
	}
	/// <summary>
	/// Flattens this polygon down to a <see cref="Polygon2D"/>, using <paramref name="dimensionConverter"/> to map each vertex from 3D to 2D.
	/// </summary>
	/// <remarks>
	/// <paramref name="dimensionConverter"/> should generally be one created from this polygon's own <see cref="Polygon.Normal"/> (e.g. via <see cref="Plane.CreateDimensionConverter()"/> on a plane built from this polygon), so that the resultant 2D polygon's winding order matches <see cref="Polygon.IsWoundClockwise"/> as expected.
	/// </remarks>
	/// <param name="vertexDest">A buffer to write the converted 2D vertices into. Must be at least <see cref="Polygon.VertexCount"/> long; the returned <see cref="Polygon2D"/>'s vertices are a slice of this buffer, so it must remain valid for as long as the result is used.</param>
	/// <param name="dimensionConverter">The converter used to map each 3D vertex to a 2D coordinate.</param>
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, DimensionConverter dimensionConverter) {
		if (vertexDest.Length < VertexCount) {
			throw new ArgumentException($"Destination span for converted vertices must be at least as large as '{nameof(VertexCount)}' ({VertexCount}).", nameof(vertexDest));
		}

		for (var i = 0; i < VertexCount; ++i) {
			vertexDest[i] = dimensionConverter.ConvertLocation(Vertices[i]);
		}
		return new(vertexDest[..VertexCount], IsWoundClockwise);
	}

	internal void FillInMissingTriangulationParameters([NotNull] ref Direction? textureUDirection, [NotNull] ref Direction? textureVDirection, [NotNull] ref Location? textureOrigin) {
		textureUDirection ??= Normal.AnyOrthogonal();
		textureVDirection ??= Direction.FromDualOrthogonalization(Normal, textureUDirection.Value);
		textureOrigin ??= Centroid;
	}
}