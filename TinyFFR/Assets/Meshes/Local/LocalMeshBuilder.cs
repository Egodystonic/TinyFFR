// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IMeshImplProvider, IDisposable {
	const string DefaultMeshName = "Unnamed Mesh";
	readonly ArrayPoolBackedMap<MeshHandle, MeshBufferData> _activeMeshes = new();
	readonly ArrayPoolBackedMap<VertexBufferHandle, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<IndexBufferHandle, int> _indexBufferRefCounts = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;
	nuint _nextHandleId = 0;

	public LocalMeshBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public Mesh CreateMesh(CuboidDescriptor cuboidDesc, scoped in MeshCreationConfig config) {
		ThrowIfThisIsDisposed();
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

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config) {
		ThrowIfThisIsDisposed();
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

		var tempVertexBuffer = _globals.CreateAndCopyToGpuHoldingBuffer(vertices);
		var tempIndexBuffer = _globals.CreateAndCopyToGpuHoldingBuffer(triangles);
		
		if (config.FlipTriangles) {
			var intSpan = tempIndexBuffer.AsSpan<int>();
			for (var i = 0; i < triangles.Length; ++i) {
				var a = intSpan[i * 3];
				var b = intSpan[i * 3 + 1];
				intSpan[i * 3] = b;
				intSpan[i * 3 + 1] = a;
			}
		}

		int indexBufferCount;
		checked {
			indexBufferCount = triangles.Length * 3;
		}

		AllocateVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertex*) tempVertexBuffer.DataPtr, vertices.Length, out var vbHandle).ThrowIfFailure();
		AllocateIndexBuffer(tempIndexBuffer.BufferIdentity, (MeshTriangle*) tempIndexBuffer.DataPtr, indexBufferCount, out var ibHandle).ThrowIfFailure();

		_vertexBufferRefCounts.Add(vbHandle, 1);
		_indexBufferRefCounts.Add(ibHandle, 1);
		_nextHandleId++;
		var handle = new MeshHandle(_nextHandleId);
		_activeMeshes.Add(handle, new(vbHandle, ibHandle, 0, indexBufferCount));
		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		return new Mesh(handle, this);
	}

	public MeshBufferData GetBufferData(MeshHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshes[handle];
	}

	public ReadOnlySpan<char> GetName(MeshHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMeshName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Mesh HandleToInstance(MeshHandle h) => new(h, this);

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		MeshVertex* verticesPtr,
		int numVertices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_vertex_buffer")]
	static extern InteropResult DisposeVertexBuffer(
		UIntPtr bufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_index_buffer")]
	static extern InteropResult AllocateIndexBuffer(
		nuint bufferId,
		MeshTriangle* indicesPtr,
		int numIndices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_index_buffer")]
	static extern InteropResult DisposeIndexBuffer(
		UIntPtr bufferHandle
	);
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Mesh Builder [Disposed]" : "TinyFFR Local Mesh Builder";

	#region Disposal
	public bool IsDisposed(MeshHandle handle) => _isDisposed || !_activeMeshes.ContainsKey(handle);

	public void Dispose(MeshHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(MeshHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var bufferData = _activeMeshes[handle];
		var curVbRefCount = _vertexBufferRefCounts[bufferData.VertexBufferHandle];
		var curIbRefCount = _indexBufferRefCounts[bufferData.IndexBufferHandle];
		if (curVbRefCount <= 1) {
			_vertexBufferRefCounts.Remove(bufferData.VertexBufferHandle);
			DisposeVertexBuffer(bufferData.VertexBufferHandle).ThrowIfFailure();
		}
		if (curIbRefCount <= 1) {
			_indexBufferRefCounts.Remove(bufferData.IndexBufferHandle);
			DisposeIndexBuffer(bufferData.IndexBufferHandle).ThrowIfFailure();
		}
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeMeshes.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeMeshes) Dispose(kvp.Key, removeFromMap: false);
			_activeMeshes.Dispose();

			// In theory, both ref-count maps should be empty at this point. But we'll do this anyway.
			foreach (var kvp in _vertexBufferRefCounts) {
				DisposeVertexBuffer(kvp.Key).ThrowIfFailure();
			}
			foreach (var kvp in _indexBufferRefCounts) {
				DisposeIndexBuffer(kvp.Key).ThrowIfFailure();
			}
			_vertexBufferRefCounts.Dispose();
			_indexBufferRefCounts.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(MeshHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Mesh));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}