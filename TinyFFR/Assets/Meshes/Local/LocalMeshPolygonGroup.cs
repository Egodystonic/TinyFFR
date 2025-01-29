// Created on 2025-01-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Diagnostics.CodeAnalysis;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;
using MetadataEntry = (int Index, int Count, Egodystonic.TinyFFR.Direction TexU, Egodystonic.TinyFFR.Direction TexV, Egodystonic.TinyFFR.Location TexOrigin);

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed unsafe class LocalMeshPolygonGroup : IMeshPolygonGroup {
	readonly LocalMeshBuilder _parentBuilder;
	readonly delegate* managed<LocalMeshBuilder, HeapPool> _poolAccessFunc;
	readonly delegate* managed<LocalMeshBuilder, LocalMeshPolygonGroup, void> _disposalAction;

	PooledHeapMemory<Location>? _vertexList = null;
	PooledHeapMemory<MetadataEntry>? _metadataList = null;
	PooledHeapMemory<XYPair<float>>? _triangulationBuffer = null;
	PooledHeapMemory<MeshVertex>? _vertexBuffer = null;
	PooledHeapMemory<VertexTriangle>? _triangleBuffer = null;

	public int TotalPolygonCount { get; set; } = 0;
	public int TotalVertexCount { get; set; } = 0;
	public int TotalTriangleCount { get; set; } = 0;
	public int HighestIndividualVertexCount { get; set; } = 0;
	public int HighestIndividualTriangleCount { get; set; } = 0;

	public LocalMeshPolygonGroup(LocalMeshBuilder parentBuilder, delegate*<LocalMeshBuilder, HeapPool> poolAccessFunc, delegate*<LocalMeshBuilder, LocalMeshPolygonGroup, void> disposalAction) {
		_parentBuilder = parentBuilder;
		_poolAccessFunc = poolAccessFunc;
		_disposalAction = disposalAction;
	}

	public void Dispose() {
		Clear();
		_triangulationBuffer?.Dispose();
		_vertexBuffer?.Dispose();
		_triangleBuffer?.Dispose();
		_disposalAction(_parentBuilder, this);
	}

	public void Add(Polygon p, Direction textureUDirection, Direction textureVDirection, Location textureOrigin) {
		var vertexListSpaceRemaining = (_vertexList?.Buffer.Length ?? 0) - TotalVertexCount;
		if (_vertexList == null || vertexListSpaceRemaining < p.VertexCount) IncreaseVertexListSize(TotalVertexCount + p.VertexCount);

		var metadataListSpaceRemaining = (_metadataList?.Buffer.Length ?? 0) - TotalPolygonCount;
		if (_metadataList == null || metadataListSpaceRemaining <= 0) IncreaseMetadataListSize(TotalPolygonCount + 1);

		var vertexDest = _vertexList.Value.Buffer[TotalVertexCount..];
		var metadataDest = _metadataList.Value.Buffer[TotalPolygonCount..];

		p.Vertices.CopyTo(vertexDest);
		metadataDest[0] = (TotalPolygonCount, p.VertexCount, textureUDirection, textureVDirection, textureOrigin);

		TotalPolygonCount += 1;
		TotalVertexCount += p.VertexCount;
		TotalTriangleCount += p.TriangleCount;
		HighestIndividualVertexCount = Int32.Max(HighestIndividualVertexCount, p.VertexCount);
		HighestIndividualTriangleCount = Int32.Max(HighestIndividualTriangleCount, p.TriangleCount);
	}

	[MemberNotNull(nameof(_vertexList))]
	void IncreaseVertexListSize(int minSize) {
		var newList = _poolAccessFunc(_parentBuilder).Borrow<Location>(minSize * 2);
		_vertexList?.Buffer.CopyTo(newList.Buffer);
		_vertexList?.Dispose();
		_vertexList = newList;
	}

	[MemberNotNull(nameof(_metadataList))]
	void IncreaseMetadataListSize(int minSize) {
		var newList = _poolAccessFunc(_parentBuilder).Borrow<MetadataEntry>(minSize * 2);
		_metadataList?.Buffer.CopyTo(newList.Buffer);
		_metadataList?.Dispose();
		_metadataList = newList;
	}

	public Polygon GetPolygonAtIndex(int index, out Direction textureU, out Direction textureV, out Location textureOrigin) {
		if (index < 0 || index >= TotalPolygonCount) {
			throw new ArgumentOutOfRangeException(nameof(index), index, $"Total number of polygons = {TotalPolygonCount}. No polygon exists at given index.");
		}

		var metadata = _metadataList!.Value.Buffer[index];
		textureU = metadata.TexU;
		textureV = metadata.TexV;
		textureOrigin = metadata.TexOrigin;
		return new(_vertexList!.Value.Buffer.Slice(metadata.Index, metadata.Count));
	}

	public void Clear() {
		TotalPolygonCount = 0;
		TotalVertexCount = 0;
		TotalTriangleCount = 0;
		HighestIndividualVertexCount = 0;
		HighestIndividualTriangleCount = 0;
		_vertexList?.Dispose();
		_metadataList?.Dispose();
	}

	public Span<XYPair<float>> ReallocateTriangulationBufferForCurrentCount() {
		_triangulationBuffer?.Dispose();
		_triangulationBuffer = _poolAccessFunc(_parentBuilder).Borrow<XYPair<float>>(HighestIndividualVertexCount);
		return _triangulationBuffer.Value.Buffer;
	}
	public Span<MeshVertex> ReallocateVertexBufferForCurrentCount() {
		_vertexBuffer?.Dispose();
		_vertexBuffer = _poolAccessFunc(_parentBuilder).Borrow<MeshVertex>(TotalVertexCount);
		return _vertexBuffer.Value.Buffer;
	}
	public Span<VertexTriangle> ReallocateTriangleBufferForCurrentCount() {
		_triangleBuffer?.Dispose();
		_triangleBuffer = _poolAccessFunc(_parentBuilder).Borrow<VertexTriangle>(TotalTriangleCount);
		return _triangleBuffer.Value.Buffer;
	}
}