﻿// Created on 2024-08-19 by Ben Bowen
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
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IMeshImplProvider, IDisposable {
	const string DefaultMeshName = "Unnamed Mesh";
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, MeshBufferData> _activeMeshes = new();
	readonly ArrayPoolBackedMap<ResourceHandle<VertexBuffer>, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<ResourceHandle<IndexBuffer>, int> _indexBufferRefCounts = new();
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

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config) {
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
		
		if (config.FlipTriangles) {
			var intSpan = tempIndexBuffer.AsSpan<int>();
			for (var i = 0; i < triangles.Length; ++i) {
				var a = intSpan[i * 3];
				var b = intSpan[i * 3 + 1];
				intSpan[i * 3] = b;
				intSpan[i * 3 + 1] = a;
			}
		}

		// ReSharper disable once CompareOfFloatsByEqualityOperator Direct comparison with 1f is correct and exact
		if (config.InvertTextureU || config.InvertTextureV || config.OriginTranslation != Vect.Zero || config.LinearRescalingFactor != 1f) {
			var vBufferSpan = tempVertexBuffer.AsSpan<MeshVertex>();
			for (var v = 0; v < vBufferSpan.Length; ++v) {
				vBufferSpan[v] = vBufferSpan[v] with {
					Location = (vBufferSpan[v].Location - config.OriginTranslation).ScaledFromOriginBy(config.LinearRescalingFactor),
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
		var handle = new ResourceHandle<Mesh>(_nextHandleId);
		_activeMeshes.Add(handle, new(vbHandle, ibHandle, 0, indexBufferCount));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultMeshName);
		return new Mesh(handle, this);
	}

	public MeshBufferData GetBufferData(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshes[handle];
	}

	public string GetNameAsNewStringObject(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultMeshName));
	}
	public int GetNameLength(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMeshName).Length;
	}
	public void CopyName(ResourceHandle<Mesh> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultMeshName, destinationBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Mesh HandleToInstance(ResourceHandle<Mesh> h) => new(h, this);

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
	public bool IsDisposed(ResourceHandle<Mesh> handle) => _isDisposed || !_activeMeshes.ContainsKey(handle);

	public void Dispose(ResourceHandle<Mesh> handle) => Dispose(handle, removeFromMap: true);
	void Dispose(ResourceHandle<Mesh> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var bufferData = _activeMeshes[handle];
		var curVbRefCount = _vertexBufferRefCounts[bufferData.VertexBufferHandle];
		var curIbRefCount = _indexBufferRefCounts[bufferData.IndexBufferHandle];
		if (curVbRefCount <= 1) {
			_vertexBufferRefCounts.Remove(bufferData.VertexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.VertexBufferHandle, &DisposeVertexBuffer);
		}
		if (curIbRefCount <= 1) {
			_indexBufferRefCounts.Remove(bufferData.IndexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.IndexBufferHandle, &DisposeIndexBuffer);
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

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Mesh> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Mesh));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}