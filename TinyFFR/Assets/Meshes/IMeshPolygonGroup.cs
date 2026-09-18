// Created on 2025-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A reusable collection of polygons, which can be converted in to the triangles a mesh is built from via the <see cref="IMeshBuilder"/>.
/// Create a polygon group via the <see cref="IMeshBuilder.AllocateNewPolygonGroup"/> method.
/// </summary>
/// <remarks>
/// Building a mesh from polygons rather than from raw triangles saves working out the triangulation yourself, and is the natural
/// way to describe flat-faced geometry such as walls and floors. Dispose the group when finished with it, or clear and refill it
/// to build another mesh without allocating again.
/// </remarks>
public interface IMeshPolygonGroup : IDisposable {
	/// <summary>
	/// How many polygons have been added to this group.
	/// </summary>
	int TotalPolygonCount { get; }
	/// <summary>
	/// How many vertices those polygons have between them.
	/// </summary>
	int TotalVertexCount { get; }
	/// <summary>
	/// How many triangles those polygons will produce between them once triangulated.
	/// </summary>
	int TotalTriangleCount { get; }

	/// <summary>
	/// The number of vertices in whichever of this group's polygons has the most.
	/// </summary>
	int HighestIndividualVertexCount { get; }
	/// <summary>
	/// The number of triangles that whichever of this group's polygons produces the most will produce.
	/// </summary>
	int HighestIndividualTriangleCount { get; }
	
	/// <summary>
	/// Adds a polygon to this group, optionally saying how a texture should lie across it.
	/// </summary>
	/// <remarks>
	/// The polygon's vertices must be given in anticlockwise order as seen from its front face, in keeping with TinyFFR's
	/// winding convention.
	/// </remarks>
	/// <param name="p">The polygon to add.</param>
	/// <param name="textureUDirection">The direction across the polygon in which the texture's horizontal coordinate increases, or <see langword="null"/> to derive one.</param>
	/// <param name="textureVDirection">The direction across the polygon in which the texture's vertical coordinate increases, or <see langword="null"/> to derive one.</param>
	/// <param name="textureOrigin">The point on the polygon that maps to the texture's origin, or <see langword="null"/> to derive one.</param>
	void Add(Polygon p, Direction? textureUDirection = null, Direction? textureVDirection = null, Location? textureOrigin = null);
	/// <summary>
	/// Removes every polygon from this group, so that it can be filled again without being recreated.
	/// </summary>
	void Clear();

	/// <summary>
	/// Returns the polygon at the given position in this group, along with how a texture lies across it.
	/// </summary>
	/// <param name="index">Which polygon to return, in the range <c>0 &lt;= index &lt; TotalPolygonCount</c>.</param>
	/// <param name="textureU">Set to the direction in which the texture's horizontal coordinate increases.</param>
	/// <param name="textureV">Set to the direction in which the texture's vertical coordinate increases.</param>
	/// <param name="textureOrigin">Set to the point that maps to the texture's origin.</param>
	Polygon GetPolygonAtIndex(int index, out Direction textureU, out Direction textureV, out Location textureOrigin);
	
	/// <summary>
	/// Obtains a scratch buffer large enough to flatten this group's polygons in to two dimensions.
	/// </summary>
	protected Span<XYPair<float>> Reallocate2DBufferForCurrentCount();
	/// <summary>
	/// Obtains a buffer large enough to hold every vertex this group will produce.
	/// </summary>
	protected Span<MeshVertex> ReallocateVertexBufferForCurrentCount();
	/// <summary>
	/// Obtains a buffer large enough to hold every triangle this group will produce.
	/// </summary>
	protected Span<VertexTriangle> ReallocateTriangleBufferForCurrentCount();

	/// <summary>
	/// Converts every polygon in this group in to triangles, ready to be made in to a mesh.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Polygons may have any number of vertices, but a GPU draws only triangles, so each polygon must be cut in to triangles
	/// first. This does that for the whole group at once.
	/// </para>
	/// <para>
	/// The cost of cutting a single polygon grows steeply with its vertex count (roughly with the cube of it), so a few
	/// hundred vertices is comfortable but several thousand in one polygon is not. Split very complex outlines in to several
	/// polygons, or supply vertices and triangles directly, rather than relying on this for them.
	/// </para>
	/// <para>
	/// The returned spans are only valid until this group is next modified or triangulated.
	/// </para>
	/// </remarks>
	/// <param name="textureTransform">How to adjust the resulting texture coordinates. The scaling is applied as its reciprocal, so that a scaling of <c>2f</c> makes the texture appear twice as large.</param>
	/// <param name="outVertexBuffer">Set to the vertices produced.</param>
	/// <param name="outTriangleBuffer">Set to the triangles produced, indexing in to <paramref name="outVertexBuffer"/>.</param>
	void Triangulate(Transform2D textureTransform, out ReadOnlySpan<MeshVertex> outVertexBuffer, out ReadOnlySpan<VertexTriangle> outTriangleBuffer) {
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