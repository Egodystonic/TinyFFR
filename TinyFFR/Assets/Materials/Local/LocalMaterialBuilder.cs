// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IDisposable {
	const string DefaultMaterialName = "Unnamed Material";
	readonly ArrayPoolBackedMap<MaterialHandle, CombinedResourceGroup> _activeMaterialMap = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}
	
	#region Native Methods
	
	#endregion

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
	public bool IsDisposed(MeshAssetHandle handle) => _isDisposed || !_activeMeshes.ContainsKey(handle);

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