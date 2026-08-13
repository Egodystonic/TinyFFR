// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed class LocalMeshNodeImplProvider : IMeshNodeImplProvider {
	readonly LocalMeshAnimationTable _owner;
	public LocalMeshNodeImplProvider(LocalMeshAnimationTable owner) => _owner = owner;
	
	public string GetNameAsNewStringObject(ResourceHandle<MeshNode> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<MeshNode> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<MeshNode> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<MeshNode> handle) => _owner.IsDisposed(handle);
	public int GetIndex(ResourceHandle<MeshNode> handle) => _owner.GetIndex(handle);
}
