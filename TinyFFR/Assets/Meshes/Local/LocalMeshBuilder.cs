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
	readonly ArrayPoolBackedMap<MeshAssetHandle, IAssetResourcePoolProvider.AssetNameBuffer> _activeMeshes = new();
	readonly ArrayPoolBackedMap<UIntPtr, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<UIntPtr, int> _indexBufferRefCounts = new();
	bool _isDisposed = false;
	readonly IAssetResourcePoolProvider _resourcePoolProvider;

	public LocalMeshBuilder(IAssetResourcePoolProvider assetResourcePoolProvider) {
		ArgumentNullException.ThrowIfNull(assetResourcePoolProvider);
		_resourcePoolProvider = assetResourcePoolProvider;
	}
	
	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		UIntPtr verticesPtr,
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
		UIntPtr indicesPtr,
		int numIndices,
		out IndexBufferHandle outBufferHandle
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_index_buffer")]
	static extern InteropResult DisposeIndexBuffer(
		IndexBufferHandle bufferHandle
	);
	#endregion

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles) => CreateMesh(vertices, triangles, new());
	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config) {
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

		try {

		}
		catch {
			// TODO dispose all buffers here
			throw;
		}
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