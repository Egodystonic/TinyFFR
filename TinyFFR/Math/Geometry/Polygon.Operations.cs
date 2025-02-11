// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR;

partial struct Polygon {
	public Location Centroid {
		get {
			var result = Vect.Zero;
			foreach (var vertex in Vertices) {
				result += (Vect) vertex;
			}
			return (Location) (result / VertexCount);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest) => ToPolygon2D(vertexDest, Centroid);
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, Location originPoint) {
		var zBasis = -Normal;
		var xBasis = zBasis.AnyOrthogonal();
		var yBasis = ((IsWoundClockwise ? 90f : -90f) % zBasis) * xBasis;
		var converter = new DimensionConverter(xBasis, yBasis, zBasis, originPoint);
		return ToPolygon2D(vertexDest, converter);
	}
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, DimensionConverter dimensionConverter) {
		if (vertexDest.Length < VertexCount) {
			throw new ArgumentException($"Destination span for converted vertices must be at least as large as '{nameof(VertexCount)}' ({VertexCount}).", nameof(vertexDest));
		}

		Console.WriteLine($"Begin triangulation: {Normal.NearestOrientation.AsEnum}");
		for (var i = 0; i < VertexCount; ++i) {
			vertexDest[i] = dimensionConverter.ConvertLocation(Vertices[i]);
			var signVertex3d = new Location(MathF.Sign(Vertices[i].X), MathF.Sign(Vertices[i].Y), MathF.Sign(Vertices[i].Z));
			var signVertex2d = new XYPair<float>(MathF.Sign(vertexDest[i].X), MathF.Sign(vertexDest[i].Y));
			Console.WriteLine($"\t{Vertices[i]} to {vertexDest[i]} || {signVertex3d} to {signVertex2d}");
		}
		return new(vertexDest[..VertexCount], IsWoundClockwise);
	}
}