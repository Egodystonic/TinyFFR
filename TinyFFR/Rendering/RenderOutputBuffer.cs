// Created on 2025-07-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public readonly unsafe struct RenderOutputBuffer : IDisposableResource<RenderOutputBuffer, IRenderOutputBufferImplProvider>, IRenderTarget {
	readonly ResourceHandle<RenderOutputBuffer> _handle;
	readonly IRenderOutputBufferImplProvider _impl;

	internal ResourceHandle<RenderOutputBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(RenderOutputBuffer)) : _handle;
	internal IRenderOutputBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<RenderOutputBuffer>();

	IRenderOutputBufferImplProvider IResource<RenderOutputBuffer, IRenderOutputBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<RenderOutputBuffer> IResource<RenderOutputBuffer>.Handle => Handle;

	public XYPair<int> TextureDimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTextureDimensions(_handle);
	}

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<uint> IRenderTarget.ViewportDimensions => TextureDimensions.Cast<uint>();

	internal RenderOutputBuffer(ResourceHandle<RenderOutputBuffer> handle, IRenderOutputBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadNextFrame(Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler) => Implementation.SetOutputChangeHandler(_handle, handler, handleOnlyNextChange: true);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadNextFrame(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler) => Implementation.SetOutputChangeHandler(_handle, handler, handleOnlyNextChange: true);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StartReadingFrames(Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler) => Implementation.SetOutputChangeHandler(_handle, handler, handleOnlyNextChange: false);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StartReadingFrames(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler) => Implementation.SetOutputChangeHandler(_handle, handler, handleOnlyNextChange: false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StopReadingFrames(bool cancelQueuedFrames) => Implementation.ClearOutputChangeHandlers(_handle, cancelQueuedFrames);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static RenderOutputBuffer IResource<RenderOutputBuffer>.CreateFromHandleAndImpl(ResourceHandle<RenderOutputBuffer> handle, IResourceImplProvider impl) {
		return new RenderOutputBuffer(handle, impl as IRenderOutputBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Render Output Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(RenderOutputBuffer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is RenderOutputBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(RenderOutputBuffer left, RenderOutputBuffer right) => left.Equals(right);
	public static bool operator !=(RenderOutputBuffer left, RenderOutputBuffer right) => !left.Equals(right);
	#endregion
}