// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public readonly struct Renderer : IDisposableResource<Renderer, IRendererImplProvider> {
	readonly ResourceHandle<Renderer> _handle;
	readonly IRendererImplProvider _impl;

	internal IRendererImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Renderer>();
	internal ResourceHandle<Renderer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Renderer)) : _handle;

	IRendererImplProvider IResource<Renderer, IRendererImplProvider>.Implementation => Implementation;
	ResourceHandle<Renderer> IResource<Renderer>.Handle => Handle;

	internal Renderer(ResourceHandle<Renderer> handle, IRendererImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Renderer IResource<Renderer>.CreateFromHandleAndImpl(ResourceHandle<Renderer> handle, IResourceImplProvider impl) {
		return new Renderer(handle, impl as IRendererImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Render() => Implementation.Render(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WaitForGpu() => Implementation.WaitForGpu(_handle);

	public void RenderAndWaitForGpu() {
		Render();
		WaitForGpu();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetQuality(RenderQualityConfig newQualityConfig) => Implementation.SetQualityConfig(_handle, newQualityConfig);

	// TODO make it clear that CaptureScreenshot incurs a framedrop penalty and that rendering to an output buffer is preferable for continuous CPU streaming
	// TODO also make it clear that the bitmapFilePath overload can throw IOException
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CaptureScreenshot(ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig = null, XYPair<int>? captureResolution = null) => Implementation.CaptureScreenshot(_handle, bitmapFilePath, saveConfig, captureResolution);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CaptureScreenshot(Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, XYPair<int>? captureResolution = null, bool presentFrameTopToBottom = false) => Implementation.CaptureScreenshot(_handle, handler, captureResolution, presentFrameTopToBottom);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void CaptureScreenshot(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, XYPair<int>? captureResolution = null, bool presentFrameTopToBottom = false) => Implementation.CaptureScreenshot(_handle, handler, captureResolution, presentFrameTopToBottom);

	public override string ToString() => $"Renderer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(Renderer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Renderer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Renderer left, Renderer right) => left.Equals(right);
	public static bool operator !=(Renderer left, Renderer right) => !left.Equals(right);
	#endregion
}