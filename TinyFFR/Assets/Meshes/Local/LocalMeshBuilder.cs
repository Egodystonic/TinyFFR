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
	readonly ObjectPool<LocalMeshPolygonGroup, LocalMeshBuilder> _meshPolyGroupPool;
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;
	nuint _nextHandleId = 0;

	public LocalMeshBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_meshPolyGroupPool = new(&CreateNewPolyGroupInstance, this);
	}

	static LocalMeshPolygonGroup CreateNewPolyGroupInstance(LocalMeshBuilder arg) => new(arg, &PolyGroupHeapPoolAccessorFunc, &ReturnPolyGroup);

	static HeapPool PolyGroupHeapPoolAccessorFunc(LocalMeshBuilder builder) {
		if (builder._isDisposed) {
			throw new ObjectDisposedException(
				nameof(IMeshBuilder), 
				$"The mesh builder that allocated the given {nameof(IMeshPolygonGroup)} has been disposed, " +
				$"therefore this polygon group is no longer valid."
			);
		}
		return builder._globals.HeapPool;
	}

	static void ReturnPolyGroup(LocalMeshBuilder builder, LocalMeshPolygonGroup group) {
		if (builder._isDisposed) return;
		builder._meshPolyGroupPool.Return(group);
	}

	public IMeshPolygonGroup AllocateNewPolygonGroup() {
		ThrowIfThisIsDisposed();
		return _meshPolyGroupPool.Rent();
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, scoped in MeshCreationConfig config) {
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

		var tempVertexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		var tempIndexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(triangles);

		foreach (var vertex in vertices) Console.WriteLine(vertex);
		
		if (config.FlipTriangles) {
			var intSpan = tempIndexBuffer.AsSpan<int>();
			for (var i = 0; i < triangles.Length; ++i) {
				var a = intSpan[i * 3];
				var b = intSpan[i * 3 + 1];
				intSpan[i * 3] = b;
				intSpan[i * 3 + 1] = a;
			}
		}
		if (config.InvertTextureU || config.InvertTextureV) {
			var vBufferSpan = tempVertexBuffer.AsSpan<MeshVertex>();
			for (var v = 0; v < vBufferSpan.Length; ++v) {
				vBufferSpan[v] = vBufferSpan[v] with {
					TextureCoords = (
						config.InvertTextureU ? 1f - vBufferSpan[v].TextureCoords.X : vBufferSpan[v].TextureCoords.X,
						config.InvertTextureV ? 1f - vBufferSpan[v].TextureCoords.Y : vBufferSpan[v].TextureCoords.Y
					)
				};
			}
		}

		int indexBufferCount;
		checked {
			indexBufferCount = triangles.Length * 3;
		}

		AllocateVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertex*) tempVertexBuffer.DataPtr, vertices.Length, out var vbHandle).ThrowIfFailure();
		AllocateIndexBuffer(tempIndexBuffer.BufferIdentity, (VertexTriangle*) tempIndexBuffer.DataPtr, indexBufferCount, out var ibHandle).ThrowIfFailure();

		_vertexBufferRefCounts.Add(vbHandle, 1);
		_indexBufferRefCounts.Add(ibHandle, 1);
		_nextHandleId++;
		var handle = new MeshHandle(_nextHandleId);
		_activeMeshes.Add(handle, new(vbHandle, ibHandle, 0, indexBufferCount));
		_globals.StoreResourceNameIfNotEmpty(handle.Ident, config.Name);
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
		VertexTriangle* indicesPtr,
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
			_meshPolyGroupPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(MeshHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Mesh));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}