// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IMeshAssetImplProvider, IDisposable {
	const string DefaultMeshName = "Unnamed Mesh";
	readonly ArrayPoolBackedMap<MeshAssetHandle, (UIntPtr VertexBufferRef, UIntPtr IndexBufferRef, IAssetResourcePoolProvider.AssetNameBuffer? NameBuffer)> _activeMeshes = new();
	readonly ArrayPoolBackedMap<UIntPtr, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<UIntPtr, int> _indexBufferRefCounts = new();
	readonly IAssetResourcePoolProvider _resourcePoolProvider;
	bool _isDisposed = false;
	MeshAssetHandle _nextAssetHandleId = 0UL;

	public LocalMeshBuilder(IAssetResourcePoolProvider assetResourcePoolProvider) {
		ArgumentNullException.ThrowIfNull(assetResourcePoolProvider);
		_resourcePoolProvider = assetResourcePoolProvider;
	}
	
	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		MeshVertex* verticesPtr,
		int numVertices,
		out VertexBufferHandle outBufferHandle
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_vertex_buffer")]
	static extern InteropResult DisposeVertexBuffer(
		VertexBufferHandle bufferHandle
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "allocate_index_buffer")]
	static extern InteropResult AllocateIndexBuffer(
		nuint bufferId,
		MeshTriangle* indicesPtr,
		int numIndices,
		out IndexBufferHandle outBufferHandle
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_index_buffer")]
	static extern InteropResult DisposeIndexBuffer(
		IndexBufferHandle bufferHandle
	);
	#endregion

	public Mesh CreateMesh(CuboidDescriptor cuboidDesc) => CreateMesh(cuboidDesc, new());
	public Mesh CreateMesh(CuboidDescriptor cuboidDesc, in MeshCreationConfig config) {
		Span<MeshVertex> vertices = stackalloc MeshVertex[8];
		Span<MeshTriangle> triangles = stackalloc MeshTriangle[12];

		vertices[0] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward), new(0f, 0f));
		vertices[1] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward), new(1f, 0f));
		vertices[2] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward), new(0f, 1f));
		vertices[3] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward), new(1f, 1f));
		vertices[4] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward), new(0f, 0f));
		vertices[5] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward), new(1f, 0f));
		vertices[6] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward), new(0f, 1f));
		vertices[7] = new MeshVertex(cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward), new(1f, 1f));

		// Top
		triangles[00] = new MeshTriangle(0, 1, 2);
		triangles[01] = new MeshTriangle(0, 3, 2);

		// Bottom
		triangles[02] = new MeshTriangle(4, 6, 5);
		triangles[03] = new MeshTriangle(4, 6, 7);

		// Left
		triangles[04] = new MeshTriangle(0, 2, 6);
		triangles[05] = new MeshTriangle(6, 4, 0);

		// Right
		triangles[06] = new MeshTriangle(1, 5, 7);
		triangles[07] = new MeshTriangle(1, 7, 3);

		// Forward
		triangles[08] = new MeshTriangle(0, 4, 1);
		triangles[09] = new MeshTriangle(1, 4, 5);

		// Backward
		triangles[10] = new MeshTriangle(2, 3, 6);
		triangles[11] = new MeshTriangle(3, 7, 6);

		return CreateMesh(vertices, triangles, config);
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles) => CreateMesh(vertices, triangles, new());
	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config) {
		static void CheckTriangleIndex(char indexChar, int triangleIndex, int value, int numVertices) {
			if (value < 0 || value >= numVertices) {
				throw new ArgumentException($"Index '{indexChar}' in triangle #{triangleIndex} (0-indexed) is \"{value}\"; " +
											$"expected a non-negative value smaller than the number of vertices ({numVertices}).");
			}
		}

		if (vertices.Length == 0) throw new ArgumentException("Vertices span must not be empty!", nameof(vertices));
		if (triangles.Length == 0) throw new ArgumentException("Triangles span must not be empty!", nameof(triangles));

		for (var i = 0; i < triangles.Length; ++i) {
			CheckTriangleIndex('A', i, triangles[i].IndexA, vertices.Length);
			CheckTriangleIndex('B', i, triangles[i].IndexB, vertices.Length);
			CheckTriangleIndex('C', i, triangles[i].IndexC, vertices.Length);
		}

		var tempVertexBuffer = _resourcePoolProvider.CopySpanToTemporaryAssetLoadSpace(vertices);
		var tempIndexBuffer = _resourcePoolProvider.CopySpanToTemporaryAssetLoadSpace(triangles);
		
		if (config.FlipTriangles) {
			var intSpan = tempIndexBuffer.AsSpan<int>();
			for (var i = 0; i < triangles.Length; ++i) {
				var a = intSpan[i * 3];
				var b = intSpan[i * 3 + 1];
				intSpan[i * 3] = b;
				intSpan[i * 3 + 1] = a;
			}
		}

		IAssetResourcePoolProvider.AssetNameBuffer? assetNameBuffer = null;
		if (config.NameAsSpan.Length > 0) {
			assetNameBuffer = _resourcePoolProvider.CopyAssetNameToFixedBuffer(config.NameAsSpan);
		}

		AllocateVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertex*) tempVertexBuffer.DataPtr, vertices.Length, out var vbHandle).ThrowIfFailure();
		AllocateIndexBuffer(tempIndexBuffer.BufferIdentity, (MeshTriangle*) tempIndexBuffer.DataPtr, triangles.Length * 3, out var ibHandle).ThrowIfFailure();

		_vertexBufferRefCounts.Add((UIntPtr) vbHandle, 1);
		_indexBufferRefCounts.Add((UIntPtr) ibHandle, 1);
		_nextAssetHandleId++;
		_activeMeshes.Add(_nextAssetHandleId, ((UIntPtr) vbHandle, (UIntPtr) ibHandle, assetNameBuffer));
		return new Mesh(_nextAssetHandleId, this);
	}

	public string GetName(MeshAssetHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var buf = _activeMeshes[handle].NameBuffer;
		if (buf == null) return DefaultMeshName;
		else return new(buf.Value.AsSpan);
	}
	public int GetNameUsingSpan(MeshAssetHandle handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var buf = _activeMeshes[handle].NameBuffer;
		if (buf == null) {
			DefaultMeshName.CopyTo(dest);
			return DefaultMeshName.Length;
		}
		else {
			buf.Value.AsSpan.CopyTo(dest);
			return buf.Value.AsSpan.Length;
		}
	}
	public int GetNameSpanMaxLength(MeshAssetHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var buf = _activeMeshes[handle].NameBuffer;
		if (buf == null) return DefaultMeshName.Length;
		else return buf.Value.CharacterCount;
	}

	#region Disposal
	public bool IsDisposed(MeshAssetHandle handle) => !_activeMeshes.ContainsKey(handle);

	public void Dispose(MeshAssetHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(MeshAssetHandle handle, bool removeFromMap) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var tuple = _activeMeshes[handle];
		var curVbRefCount = _vertexBufferRefCounts[tuple.VertexBufferRef];
		var curIbRefCount = _indexBufferRefCounts[tuple.IndexBufferRef];
		if (curVbRefCount <= 1) {
			_vertexBufferRefCounts.Remove(tuple.VertexBufferRef);
			DisposeVertexBuffer((VertexBufferHandle) tuple.VertexBufferRef).ThrowIfFailure();
		}
		if (curIbRefCount <= 1) {
			_indexBufferRefCounts.Remove(tuple.IndexBufferRef);
			DisposeIndexBuffer((IndexBufferHandle) tuple.IndexBufferRef).ThrowIfFailure();
		}
		if (tuple.NameBuffer != null) _resourcePoolProvider.DeallocateNameBuffer(tuple.NameBuffer.Value);
		if (removeFromMap) _activeMeshes.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeMeshes) Dispose(kvp.Key, removeFromMap: false);
			_activeMeshes.Dispose();

			// In theory, both ref-count maps should be empty at this point. But we'll do this anyway.
			foreach (var kvp in _vertexBufferRefCounts) {
				DisposeVertexBuffer((VertexBufferHandle) kvp.Key).ThrowIfFailure();
			}
			foreach (var kvp in _indexBufferRefCounts) {
				DisposeIndexBuffer((IndexBufferHandle) kvp.Key).ThrowIfFailure();
			}
			_vertexBufferRefCounts.Dispose();
			_indexBufferRefCounts.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(MeshAssetHandle handle) {
		ThrowIfThisIsDisposed();
		ObjectDisposedException.ThrowIf(!_activeMeshes.ContainsKey(handle), typeof(Mesh));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
	#endregion
}