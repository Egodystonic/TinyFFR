// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="RenderOutputBuffer"/> resources.
/// </summary>
public unsafe interface IRenderOutputBufferImplProvider : IDisposableResourceImplProvider<RenderOutputBuffer> {
	/// <summary>
	/// Invoked via <see cref="RenderOutputBuffer.CreateDynamicTexture"/>.
	/// </summary>
	Texture CreateDynamicTexture(ResourceHandle<RenderOutputBuffer> handle);
	/// <summary>
	/// Invoked via <see cref="RenderOutputBuffer.TextureDimensions"/>.
	/// </summary>
	XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle);
	/// <summary>
	/// Invoked via <see cref="RenderOutputBuffer.ReadNextFrame(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/> (with <paramref name="handleOnlyNextChange"/> <see langword="true"/>) and <see cref="RenderOutputBuffer.StartReadingFrames(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/> (with <paramref name="handleOnlyNextChange"/> <see langword="false"/>).
	/// </summary>
	void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange);
	/// <inheritdoc cref="SetOutputChangeHandler(ResourceHandle{RenderOutputBuffer},Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool,bool)"/>
	void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange);
	/// <summary>
	/// Invoked via <see cref="RenderOutputBuffer.StopReadingFrames"/>.
	/// </summary>
	void ClearOutputChangeHandlers(ResourceHandle<RenderOutputBuffer> handle, bool cancelQueuedFrames);
}