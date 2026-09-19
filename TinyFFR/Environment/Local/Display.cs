// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Represents one of the displays (monitors) connected to the local machine. Obtained via the factory's <see cref="IDisplayDiscoverer"/>.
/// </summary>
public readonly struct Display : IResource<Display, IDisplayImplProvider> {
	readonly ResourceHandle<Display> _handle;
	readonly IDisplayImplProvider _impl;

	internal ResourceHandle<Display> Handle => Implementation.IsValid(_handle) ? _handle : throw new ObjectDisposedException(nameof(Display));
	internal IDisplayImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Display>();

	IDisplayImplProvider IResource<Display, IDisplayImplProvider>.Implementation => Implementation;
	ResourceHandle<Display> IResource<Display>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// Whether this is the machine's primary display; i.e. the one the operating system treats as the user's main screen.
	/// </summary>
	public bool IsPrimary {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsPrimary(_handle);
	}
	/// <summary>
	/// Every <see cref="DisplayMode"/> (combination of resolution and refresh rate) that this display can be set to.
	/// </summary>
	public ReadOnlySpan<DisplayMode> SupportedDisplayModes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSupportedDisplayModes(_handle);
	}
	/// <summary>
	/// The supported mode with the most pixels; i.e. this display's maximum resolution.
	/// </summary>
	/// <remarks>
	/// Where multiple supported modes share that same maximum resolution, the one with the highest refresh rate is returned. Note that the highest-resolution
	/// mode is not necessarily the highest-refresh-rate mode: displays commonly support a faster refresh rate at a lower resolution
	/// (see <see cref="HighestSupportedRefreshRateMode"/>).
	/// </remarks>
	public DisplayMode HighestSupportedResolutionMode {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHighestSupportedResolutionMode(_handle);
	}
	/// <summary>
	/// The supported mode with the fastest refresh rate; i.e. the mode in which this display redraws most frequently.
	/// </summary>
	/// <remarks>
	/// Where multiple supported modes share that same maximum refresh rate, the one with the most pixels is returned. Note that the highest-refresh-rate mode is
	/// not necessarily the highest-resolution mode: displays commonly only reach their fastest refresh rate at a reduced resolution
	/// (see <see cref="HighestSupportedResolutionMode"/>).
	/// </remarks>
	public DisplayMode HighestSupportedRefreshRateMode {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHighestSupportedRefreshRateMode(_handle);
	}
	/// <summary>
	/// The resolution this display is currently set to, in pixels (<c>X</c> = width, <c>Y</c> = height).
	/// </summary>
	public XYPair<int> CurrentResolution {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCurrentResolution(_handle);
	}
	internal XYPair<int> GlobalPositionOffset {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetGlobalPositionOffset(_handle);
	}

	internal Display(ResourceHandle<Display> handle, IDisplayImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Display IResource<Display>.CreateFromHandleAndImpl(ResourceHandle<Display> handle, IResourceImplProvider impl) {
		return new Display(handle, impl as IDisplayImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Display> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Display> IResource<Display>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	internal XYPair<int> TranslateDisplayLocalWindowPositionToGlobal(XYPair<int> displayLocalPosition) => displayLocalPosition + GlobalPositionOffset;
	internal XYPair<int> TranslateGlobalWindowPositionToDisplayLocal(XYPair<int> globalPosition) => globalPosition - GlobalPositionOffset;

	/// <inheritdoc/>
	public override string ToString() {
		return Implementation.IsValid(_handle) 
			? $"{nameof(Display)} \"{GetNameAsNewStringObject()}\" ({CurrentResolution.X:#} x {CurrentResolution.Y:#}){(IsPrimary ? " (Primary)" : "")}"
			: $"{nameof(Display)} [Invalid]";
	}

	#region Equality
	/// <inheritdoc/>
	public bool Equals(Display other) => _handle == other._handle && _impl == other._impl;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is Display other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// <see cref="Equals(Display)"/>
	/// </summary>
	public static bool operator ==(Display left, Display right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(Display)"/>
	/// </summary>
	public static bool operator !=(Display left, Display right) => !left.Equals(right);
	#endregion
}