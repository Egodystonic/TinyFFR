// Created on 2025-07-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Represents a target buffer that a <see cref="Renderer"/> or <see cref="RendererCompositor"/> can render in to.
/// The rendered data can then be used as a <see cref="Texture"/> inside a scene or read from/written to disc. Created via the factory's <see cref="IRendererBuilder"/>.
/// </summary>
public readonly unsafe struct RenderOutputBuffer : IDisposableResource<RenderOutputBuffer, IRenderOutputBufferImplProvider>, IRenderTarget {
	readonly ResourceHandle<RenderOutputBuffer> _handle;
	readonly IRenderOutputBufferImplProvider _impl;

	internal ResourceHandle<RenderOutputBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(RenderOutputBuffer)) : _handle;
	internal IRenderOutputBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<RenderOutputBuffer>();

	IRenderOutputBufferImplProvider IResource<RenderOutputBuffer, IRenderOutputBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<RenderOutputBuffer> IResource<RenderOutputBuffer>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The dimensions of this output buffer.
	/// </summary>
	public XYPair<int> TextureDimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTextureDimensions(_handle);
	}

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<int> IRenderTarget.ViewportDimensions => TextureDimensions;

	internal RenderOutputBuffer(ResourceHandle<RenderOutputBuffer> handle, IRenderOutputBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <summary>
	/// Registers <paramref name="handler"/> to be invoked exactly once, with the pixel data of the next frame rendered to this buffer, after which the handler is automatically de-registered.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The given span's referred memory is only valid to use/access until <paramref name="handler"/> returns,
	/// and is laid out in rows (i.e. for a 1920x1080 frame, the first 1920 texels are row 1, the texels from 1920 to 3840 are row 2, etc).
	/// </para>
	/// <para>
	/// <b>Note: This replaces any previously-registered handler (from this method or <see cref="StartReadingFrames(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/>).</b>
	/// In other words, only one handler can be set for any buffer at any time.
	/// </para>
	/// </remarks>
	/// <param name="handler">The function to invoke with the next frame's dimensions and texel data. Must not be <see langword="null"/>.</param>
	/// <param name="presentFrameTopToBottom">If <see langword="true"/>, the first row in the given texel data will be the top of the frame; if <see langword="false"/> it will be the bottom.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadNextFrame(Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool presentFrameTopToBottom = false) => Implementation.SetOutputChangeHandler(_handle, handler, presentFrameTopToBottom, handleOnlyNextChange: true);
	/// <inheritdoc cref="ReadNextFrame(System.Action{XYPair{System.Int32},System.ReadOnlySpan{TexelRgba32}},System.Boolean)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadNextFrame(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool presentFrameTopToBottom = false) => Implementation.SetOutputChangeHandler(_handle, handler, presentFrameTopToBottom, handleOnlyNextChange: true);

	/// <summary>
	/// Registers <paramref name="handler"/> to be invoked repeatedly, once for every subsequent frame rendered to this buffer, until
	/// <see cref="StopReadingFrames"/> is called (or another call to this method or <see cref="ReadNextFrame(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/> replaces it).
	/// </summary>
	/// <para>
	/// The given span's referred memory is only valid to use/access until <paramref name="handler"/> returns,
	/// and is laid out in rows (i.e. for a 1920x1080 frame, the first 1920 texels are row 1, the texels from 1920 to 3840 are row 2, etc).
	/// </para>
	/// <remarks>
	/// <b>Note: This replaces any previously-registered handler (from this method or <see cref="ReadNextFrame(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/>).</b>
	/// In other words, only one handler can be set for any buffer at any time.
	/// </remarks>
	/// <param name="handler">The function to invoke with each frame's dimensions and texel data. Must not be <see langword="null"/>.</param>
	/// <param name="presentFramesTopToBottom">If <see langword="true"/>, the first row in the given texel data will be the top of the frame; if <see langword="false"/> it will be the bottom.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StartReadingFrames(Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool presentFramesTopToBottom = false) => Implementation.SetOutputChangeHandler(_handle, handler, presentFramesTopToBottom, handleOnlyNextChange: false);
	/// <inheritdoc cref="StartReadingFrames(System.Action{XYPair{System.Int32},System.ReadOnlySpan{TexelRgba32}},System.Boolean)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StartReadingFrames(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool presentFramesTopToBottom = false) => Implementation.SetOutputChangeHandler(_handle, handler, presentFramesTopToBottom, handleOnlyNextChange: false);

	/// <summary>
	/// De-register any handler previously registered via <see cref="ReadNextFrame(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/>
	/// or <see cref="StartReadingFrames(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/>, stopping it from being invoked on any frames rendered after this point.
	/// </summary>
	/// <param name="cancelQueuedFrames">If <see langword="true"/>, any frame read that was already in-flight (submitted to the GPU but not yet delivered to the handler) is cancelled immediately.
	/// If <see langword="false"/>, all such frames are allowed to complete and invoke the handler one final time each before reading stops (usually between 1 and 5 frames).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StopReadingFrames(bool cancelQueuedFrames) => Implementation.ClearOutputChangeHandlers(_handle, cancelQueuedFrames);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	/// <summary>
	/// Creates a <see cref="Texture"/> that mirrors this buffer's live output, for use as a material's texture input.
	/// </summary>
	/// <remarks>
	/// Unlike a texture loaded from an image file, the returned texture updates automatically every time this buffer is rendered to again. There is no need to re-create or refresh it per frame.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Texture CreateDynamicTexture() => Implementation.CreateDynamicTexture(_handle);

	static RenderOutputBuffer IResource<RenderOutputBuffer>.CreateFromHandleAndImpl(ResourceHandle<RenderOutputBuffer> handle, IResourceImplProvider impl) {
		return new RenderOutputBuffer(handle, impl as IRenderOutputBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<RenderOutputBuffer> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<RenderOutputBuffer> IResource<RenderOutputBuffer>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc/>
	public override string ToString() => $"Render Output Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc/>
	public bool Equals(RenderOutputBuffer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is RenderOutputBuffer other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <see cref="Equals(RenderOutputBuffer)"/>
	public static bool operator ==(RenderOutputBuffer left, RenderOutputBuffer right) => left.Equals(right);
	/// <see cref="Equals(RenderOutputBuffer)"/>
	public static bool operator !=(RenderOutputBuffer left, RenderOutputBuffer right) => !left.Equals(right);
	#endregion
}