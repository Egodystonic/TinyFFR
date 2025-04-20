// Created on 2025-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshPolygonGroup : IDisposable {
	int TotalPolygonCount { get; }
	int TotalVertexCount { get; }
	int TotalTriangleCount { get; }

	int HighestIndividualVertexCount { get; }
	int HighestIndividualTriangleCount { get; }
	
	void Add(Polygon p, Direction? textureUDirection = null, Direction? textureVDirection = null, Location? textureOrigin = null);
	void Clear();

	Polygon GetPolygonAtIndex(int index, out Direction textureU, out Direction textureV, out Location textureOrigin);
	
	protected Span<XYPair<float>> Reallocate2DBufferForCurrentCount();
	protected Span<MeshVertex> ReallocateVertexBufferForCurrentCount();
	protected Span<VertexTriangle> ReallocateTriangleBufferForCurrentCount();

	internal void Triangulate(Transform2D textureTransform, out ReadOnlySpan<MeshVertex> outVertexBuffer, out ReadOnlySpan<VertexTriangle> outTriangleBuffer) {
		textureTransform = textureTransform with { Scaling = textureTransform.Scaling.Reciprocal ?? XYPair<float>.Zero };
		
		var twoDimensionalBuffer = Reallocate2DBufferForCurrentCount();
		var vertexBuffer = ReallocateVertexBufferForCurrentCount();
		var triangleBuffer = ReallocateTriangleBufferForCurrentCount();

		outVertexBuffer = vertexBuffer;
		outTriangleBuffer = triangleBuffer;

		var cumulativeVertexCount = 0;
		
		for (var p = 0; p < TotalPolygonCount; ++p) {
			var polygon = GetPolygonAtIndex(p, out var texU, out var texV, out var texOrigin);

			var texCoordConverter = new DimensionConverter(texU, texV, polygon.Normal, texOrigin);

			for (var v = 0; v < polygon.VertexCount; ++v) {
				vertexBuffer[v] = new(
					polygon.Vertices[v],
					texCoordConverter.ConvertLocation(polygon.Vertices[v]) * textureTransform,
					texU,
					texV,
					polygon.Normal
				);
			}

			polygon.ToPolygon2D(twoDimensionalBuffer).Triangulate(triangleBuffer);
			
			for (var t = 0; t < polygon.TriangleCount; ++t) {
				triangleBuffer[t] = triangleBuffer[t].ShiftedBy(cumulativeVertexCount);
			}

			cumulativeVertexCount += polygon.VertexCount;
			vertexBuffer = vertexBuffer[polygon.VertexCount..];
			triangleBuffer = triangleBuffer[polygon.TriangleCount..];
		}
	}
}