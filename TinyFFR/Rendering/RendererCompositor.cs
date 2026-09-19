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
/// <remarks>
/// <para>
/// When rendering via <see cref="RenderAll"/>, the lowest non-negative <see cref="RendererCreationConfig.GpuSynchronizationFrameBufferCount"/> among all added renderers is selected
/// as the value for this compositor's synchronization frame count. If you want to be sure of the sync count used by this compositor, set every added renderer's
/// <c>GpuSynchronizationFrameBufferCount</c> to the desired value. 
/// </para>
/// <para>
/// If no enabled renderer has a non-negative <see cref="RendererCreationConfig.GpuSynchronizationFrameBufferCount"/>, no synchronization takes place at all; which is not recommended
/// (see the caveats documented on <see cref="RendererCreationConfig.GpuSynchronizationFrameBufferCount"/>).
/// </para>
/// <para>
/// Rate-limiting (via <see cref="SetRendererFrameRateCap"/> or <see cref="SetRendererFrameRateRatio"/>) does not affect this selection: If the selected renderer is rate-limited,
/// synchronization still occurs on every invocation of <see cref="RenderAll"/>, even when that renderer's output is being reused rather than re-rendered.
/// </para>
/// </remarks>
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
	/// The <see cref="Window"/> this compositor renders in to, or <c>null</c> if it does not target a window.
	/// </summary>
	public Window? TargetWindow {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetWindow(_handle);
	}
	/// <summary>
	/// The <see cref="RenderOutputBuffer"/> this compositor renders in to, or <c>null</c> if it does not target an output buffer.
	/// </summary>
	public RenderOutputBuffer? TargetBuffer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBuffer(_handle);
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
	/// Sets an independent framerate cap on the given <paramref name="renderer"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Setting a framerate limit is useful as a performance optimisation. For example you may have a canvas scene that just prints debug information and you don't need that
	/// rendered at the full framerate; or you may have a secondary view on a scene that doesn't update often and rendering at 60+ FPS is wasteful.
	/// </para>
	/// <para>
	/// When the target <paramref name="renderer"/> would be skipped, its most recent previous frame output will be reshown in the final composited render instead.
	/// </para>
	/// <para>
	/// Note that you can set both a framerate cap and a <see cref="SetRendererFrameRateRatio">ratio</see> simultaneously, and both will apply proscriptively.
	/// </para>
	/// </remarks>
	/// <param name="renderer">The Renderer whose fraemrate you want to rate-limit.</param>
	/// <param name="maxFramesPerSecond">Regardless of how many times <see cref="RenderAll"/> is invoked, the given <paramref name="renderer"/> will not be invoked more than
	/// this many times per second. Setting this to <c>null</c> removes all limits (i.e. resets the default behaviour).</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="renderer"/> was never added to this compositor.</exception>
	/// <seealso cref="SetRendererFrameRateRatio"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateCap(Renderer renderer, int? maxFramesPerSecond) => Implementation.SetRendererFrameRateCap(_handle, renderer, maxFramesPerSecond);

	/// <summary>
	/// Gets the current framerate cap for the given <paramref name="renderer"/>.
	/// See <see cref="SetRendererFrameRateCap"/> for information on this value's meaning.
	/// </summary>
	/// <param name="renderer">The Renderer whose framerate you want to get the rate-limit for.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="renderer"/> was never added to this compositor.</exception>
	/// <returns>The currently set framerate cap, or <c>null</c> if none is set.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int? GetRendererFrameRateCap(Renderer renderer) => Implementation.GetRendererFrameRateCap(_handle, renderer);

	/// <summary>
	/// Sets an independent framerate ratio on the given <paramref name="renderer"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Setting a framerate ratio is useful as a performance optimisation. For example you may have a canvas scene that just prints debug information and you don't need that
	/// rendered at the full framerate; or you may have a secondary view on a scene that doesn't update often and rendering at 60+ FPS is wasteful.
	/// </para>
	/// <para>
	/// When the target <paramref name="renderer"/> would be skipped, its most recent previous frame output will be reshown in the final composited render instead.
	/// </para>
	/// <para>
	/// Note that you can set both a framerate ratio and a <see cref="SetRendererFrameRateCap">cap</see> simultaneously, and both will apply proscriptively.
	/// </para>
	/// </remarks>
	/// <param name="renderer">The Renderer whose framerate you want to rate-limit.</param>
	/// <param name="ratioDenominator">The reciprocal of the ratio of frame updates. For example, if <paramref name="ratioDenominator"/> is <c>4</c>, one in every four frames
	/// will show an updated frame for the target <paramref name="renderer"/>.</param>
	/// <seealso cref="SetRendererFrameRateCap"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRendererFrameRateRatio(Renderer renderer, int ratioDenominator) => Implementation.SetRendererFrameRateRatio(_handle, renderer, ratioDenominator);

	/// <summary>
	/// Gets the current framerate ratio for the given <paramref name="renderer"/>.
	/// See <see cref="SetRendererFrameRateRatio"/> for information on this value's meaning.
	/// </summary>
	/// <param name="renderer">The Renderer whose framerate you want to get the rate-limit for.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="renderer"/> was never added to this compositor.</exception>
	/// <returns>The currently set framerate ratio, or <c>null</c> if none is set.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetRendererFrameRateRatio(Renderer renderer) => Implementation.GetRendererFrameRateRatio(_handle, renderer);

	/// <summary>
	/// Renders every added renderer and composites their output together on to the target window/buffer.
	/// </summary>
	/// <inheritdoc cref="Renderer.Render" />
	/// <seealso cref="RenderAllAndWaitForGpu"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RenderAll() => Implementation.RenderAll(_handle);

	/// <inheritdoc cref="Renderer.WaitForGpu" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WaitForGpu() => Implementation.WaitForGpu(_handle);

	/// <summary>
	/// Renders every added renderer and composites their output together on to the target window/buffer.
	/// </summary>
	/// <inheritdoc cref="Renderer.RenderAndWaitForGpu" />
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
