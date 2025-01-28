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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest) => ToPolygon2D(vertexDest, Location.Origin);
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, Location originPoint) => ToPolygon2D(vertexDest, TODO needs to respect winding order);
	public Polygon2D ToPolygon2D(Span<XYPair<float>> vertexDest, DimensionConverter dimensionConverter) {
		if (vertexDest.Length < VertexCount) {
			throw new ArgumentException($"Destination span for converted vertices must be at least as large as '{nameof(VertexCount)}' ({VertexCount}).", nameof(vertexDest));
		}

		for (var i = 0; i < VertexCount; ++i) vertexDest[i] = dimensionConverter.ConvertLocation(Vertices[i]);
		return new(vertexDest, _isWoundClockwise);
	}
}