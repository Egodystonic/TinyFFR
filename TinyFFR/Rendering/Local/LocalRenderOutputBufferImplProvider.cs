// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering.Local;

// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
// on both IRenderOutputBufferImplProvider and IRendererBuilder. 
sealed class LocalRenderOutputBufferImplProvider : IRenderOutputBufferImplProvider {
	readonly LocalRendererBuilder _owner;

	public LocalRenderOutputBufferImplProvider(LocalRendererBuilder owner) => _owner = owner;

	public string GetNameAsNewStringObject(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<RenderOutputBuffer> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<RenderOutputBuffer> handle) => _owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<RenderOutputBuffer> handle) => _owner.Dispose(handle);
	public Texture CreateDynamicTexture(ResourceHandle<RenderOutputBuffer> handle) => _owner.CreateDynamicTexture(handle);
	public XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetTextureDimensions(handle);
	public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, lowestAddressesRepresentFrameTop, handleOnlyNextChange);
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, lowestAddressesRepresentFrameTop, handleOnlyNextChange);
	public void ClearOutputChangeHandlers(ResourceHandle<RenderOutputBuffer> handle, bool cancelQueuedFrames) => _owner.ClearOutputChangeHandlers(handle, cancelQueuedFrames);
	public override string ToString() => _owner.ToString();
}
