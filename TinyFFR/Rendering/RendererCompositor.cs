// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public readonly struct RendererCompositor : IDisposableResource<RendererCompositor, IRendererCompositorImplProvider> {
	readonly ResourceHandle<RendererCompositor> _handle;
	readonly IRendererCompositorImplProvider _impl;

	internal IRendererCompositorImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<RendererCompositor>();
	internal ResourceHandle<RendererCompositor> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(RendererCompositor)) : _handle;

	IRendererCompositorImplProvider IResource<RendererCompositor, IRendererCompositorImplProvider>.Implementation => Implementation;
	ResourceHandle<RendererCompositor> IResource<RendererCompositor>.Handle => Handle;

	internal RendererCompositor(ResourceHandle<RendererCompositor> handle, IRendererCompositorImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	public IndirectEnumerable<RendererCompositor, Renderer> AddedRenderers {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAddedRenderers(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(Renderer renderer, RenderCompositionType compositionType) => Implementation.Add(_handle, renderer, compositionType);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetEnabledState(Renderer renderer, bool enabled) => Implementation.SetEnabledState(_handle, renderer, enabled);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateCap(Renderer renderer, int? maxFramesPerSecond) => Implementation.SetRendererFrameRateCap(_handle, renderer, maxFramesPerSecond);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int? GetRendererFrameRateCap(Renderer renderer) => Implementation.GetRendererFrameRateCap(_handle, renderer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateRatio(Renderer renderer, int renderOnceEveryNFrames) => Implementation.SetRendererFrameRateRatio(_handle, renderer, renderOnceEveryNFrames);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetRendererFrameRateRatio(Renderer renderer) => Implementation.GetRendererFrameRateRatio(_handle, renderer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RenderAll() => Implementation.RenderAll(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WaitForGpu() => Implementation.WaitForGpu(_handle);

	public void RenderAllAndWaitForGpu() {
		RenderAll();
		WaitForGpu();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static RendererCompositor IResource<RendererCompositor>.CreateFromHandleAndImpl(ResourceHandle<RendererCompositor> handle, IResourceImplProvider impl) {
		return new RendererCompositor(handle, impl as IRendererCompositorImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<RendererCompositor> GetHandleWithoutDisposeCheck() => _handle;

	public override string ToString() => $"Renderer Group {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose(bool disposeContainedRenderers) => Implementation.Dispose(_handle, disposeContainedRenderers);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(RendererCompositor other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	public override bool Equals(object? obj) => obj is RendererCompositor other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(RendererCompositor left, RendererCompositor right) => left.Equals(right);
	public static bool operator !=(RendererCompositor left, RendererCompositor right) => !left.Equals(right);
	#endregion
}
