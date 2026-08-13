// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Local;

// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
// on both IModelImplProvider and IBackdropTextureImplProvider. 
sealed class LocalBackdropTextureImplProvider : IBackdropTextureImplProvider {
	readonly LocalAssetLoader _owner;

	public LocalBackdropTextureImplProvider(LocalAssetLoader owner) => _owner = owner;

	public UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetSkyboxTextureHandle(handle);
	public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetIndirectLightingTextureHandle(handle);
	public string GetNameAsNewStringObject(ResourceHandle<BackdropTexture> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<BackdropTexture> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<BackdropTexture> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<BackdropTexture> handle) => _owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<BackdropTexture> handle) => _owner.Dispose(handle);
	public override string ToString() => _owner.ToString();
}
