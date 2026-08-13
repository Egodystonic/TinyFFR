// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering.Local;

// This provides the implementation for textures created via RenderOutputBuffer.CreateDynamicTexture()
sealed class LocalRenderOutputBufferTextureImplProvider : ITextureImplProvider {
	const string NamePrefix = "Dynamic texture for ";
	readonly LocalRendererBuilder _owner;

	public LocalRenderOutputBufferTextureImplProvider(LocalRendererBuilder owner) => _owner = owner;
	
	ResourceHandle<RenderOutputBuffer> GetOwningBuffer(ResourceHandle<Texture> handle) {
		return TryGetOwningBuffer(handle) ?? throw new ObjectDisposedException($"Can not use given {nameof(Texture)} as its owning {nameof(RenderOutputBuffer)} has been disposed.");
	}
	ResourceHandle<RenderOutputBuffer>? TryGetOwningBuffer(ResourceHandle<Texture> handle) => _owner.TryGetBufferOwningTexture(handle);

	public string GetNameAsNewStringObject(ResourceHandle<Texture> handle) => NamePrefix + _owner.GetNameAsNewStringObject(GetOwningBuffer(handle));
	public int GetNameLength(ResourceHandle<Texture> handle) => NamePrefix.Length + _owner.GetNameLength(GetOwningBuffer(handle));
	public void CopyName(ResourceHandle<Texture> handle, Span<char> destinationBuffer) {
		NamePrefix.CopyTo(destinationBuffer);
		_owner.CopyName(GetOwningBuffer(handle), destinationBuffer[NamePrefix.Length..]);
	}
	public bool IsDisposed(ResourceHandle<Texture> handle) => TryGetOwningBuffer(handle) == null;
	public void Dispose(ResourceHandle<Texture> handle) { /* no-op */ }
	public XYPair<int> GetDimensions(ResourceHandle<Texture> handle) => _owner.GetBufferTextureDimensions(GetOwningBuffer(handle));
	public TexelType GetTexelType(ResourceHandle<Texture> handle) => TexelType.Rgba32;
	public bool GetAllowsDynamicWrites(ResourceHandle<Texture> handle) => false;
	public bool GetContainsMipMaps(ResourceHandle<Texture> handle) => false;
	public TextureRenderingConfig GetRenderingConfig(ResourceHandle<Texture> handle) => new(true, false, Quality.Standard);
	public void OverwriteTexels<TTexel>(ResourceHandle<Texture> handle, ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		throw new InvalidOperationException($"Dynamic textures associated with {nameof(RenderOutputBuffer)}s can not be dynamically written to.");
	}
}
