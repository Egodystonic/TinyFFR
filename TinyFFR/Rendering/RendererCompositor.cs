// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// A renderer compositor <i>composites</i> (combines) multiple <see cref="Renderer"/>'s outputs in to a single final result on
/// a target <see cref="Window"/> or <see cref="RenderOutputBuffer"/>. 
/// </summary>
public readonly struct RendererCompositor : IDisposableResource<RendererCompositor, IRendererCompositorImplProvider> {
	readonly ResourceHandle<RendererCompositor> _handle;
	readonly IRendererCompositorImplProvider _impl;

	internal IRendererCompositorImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<RendererCompositor>();
	internal ResourceHandle<RendererCompositor> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(RendererCompositor)) : _handle;

	IRendererCompositorImplProvider IResource<RendererCompositor, IRendererCompositorImplProvider>.Implementation => Implementation;
	ResourceHandle<RendererCompositor> IResource<RendererCompositor>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal RendererCompositor(ResourceHandle<RendererCompositor> handle, IRendererCompositorImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	/// <summary>
	/// An <see cref="IndirectEnumerable{TIn,TOut}"/> exposing all <see cref="Renderer"/>s added to this compositor.
	/// </summary>
	public IndirectEnumerable<RendererCompositor, Renderer> AddedRenderers {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAddedRenderers(_handle);
	}

	/// <summary>
	/// Add a <see cref="Renderer"/> to this compositor, meaning its output will be combined with others in to the final result.
	/// </summary>
	/// <remarks>
	/// Note that the ordering of <see cref="Add"/> calls is important: In every frame, each added renderer's output overwrites or composites over/with
	/// all previously-added ones.
	/// </remarks>
	/// <param name="renderer">The renderer to add. Its target window or buffer must match the target window/buffer this compositor was
	/// created with.</param>
	/// <param name="compositionType">How the pixel data written by this <paramref name="renderer"/> should combine with previously-added Renderers.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="renderer"/>'s render target does not match the render target this compositor was created for.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(Renderer renderer, RenderCompositionType compositionType) => Implementation.Add(_handle, renderer, compositionType);

	/// <summary>
	/// Allows disabling (or re-enabling) a previously-added <see cref="Renderer"/>'s contibution to the final composited output.
	/// </summary>
	/// <param name="renderer">The Renderer whose output you want to disable or re-enable.</param>
	/// <param name="enabled">Whether or not the <paramref name="renderer"/> should enable its output.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="renderer"/> was never added to this compositor.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetEnabledState(Renderer renderer, bool enabled) => Implementation.SetEnabledState(_handle, renderer, enabled);

	/// <summary>
	/// Sets an independent framerate cap on a previously-added <paramref name="renderer"/>. 
	/// </summary>
	/// <param name="renderer"></param>
	/// <param name="maxFramesPerSecond"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateCap(Renderer renderer, int? maxFramesPerSecond) => Implementation.SetRendererFrameRateCap(_handle, renderer, maxFramesPerSecond);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int? GetRendererFrameRateCap(Renderer renderer) => Implementation.GetRendererFrameRateCap(_handle, renderer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateRatio(Renderer renderer, int ratioDenominator) => Implementation.SetRendererFrameRateRatio(_handle, renderer, ratioDenominator);

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

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static RendererCompositor IResource<RendererCompositor>.CreateFromHandleAndImpl(ResourceHandle<RendererCompositor> handle, IResourceImplProvider impl) {
		return new RendererCompositor(handle, impl as IRendererCompositorImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<RendererCompositor> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<RendererCompositor> IResource<RendererCompositor>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <inheritdoc />
	public override string ToString() => $"Renderer Group {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	/// <summary>
	/// Invokes <see cref="Dispose()"/> on this compositor, optionally also disposing all added <see cref="Renderer"/>s.
	/// </summary>
	/// <param name="disposeContainedRenderers">Whether to dispose all previously-added renderers.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose(bool disposeContainedRenderers) => Implementation.Dispose(_handle, disposeContainedRenderers);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(RendererCompositor other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is RendererCompositor other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(RendererCompositor)"/>
	/// </summary>
	public static bool operator ==(RendererCompositor left, RendererCompositor right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(RendererCompositor)"/>
	/// </summary>
	public static bool operator !=(RendererCompositor left, RendererCompositor right) => !left.Equals(right);
	#endregion
}
