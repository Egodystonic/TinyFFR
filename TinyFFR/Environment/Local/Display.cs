﻿// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly struct Display : IResource<Display, IDisplayImplProvider> {
	readonly ResourceHandle<Display> _handle;
	readonly IDisplayImplProvider _impl;

	internal ResourceHandle<Display> Handle => Implementation.IsValid(_handle) ? _handle : throw new ObjectDisposedException(nameof(Display));
	internal IDisplayImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Display>();

	IDisplayImplProvider IResource<Display, IDisplayImplProvider>.Implementation => Implementation;
	ResourceHandle<Display> IResource<Display>.Handle => Handle;

	public bool IsPrimary {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsPrimary(_handle);
	}
	public bool IsRecommended {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsRecommended(_handle);
	}
	public ReadOnlySpan<DisplayMode> SupportedDisplayModes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSupportedDisplayModes(_handle);
	}
	public DisplayMode HighestSupportedResolutionMode {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHighestSupportedResolutionMode(_handle);
	}
	public DisplayMode HighestSupportedRefreshRateMode {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHighestSupportedRefreshRateMode(_handle);
	}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Display IResource<Display>.CreateFromHandleAndImpl(ResourceHandle<Display> handle, IResourceImplProvider impl) {
		return new Display(handle, impl as IDisplayImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	internal XYPair<int> TranslateDisplayLocalWindowPositionToGlobal(XYPair<int> displayLocalPosition) => displayLocalPosition + GlobalPositionOffset;
	internal XYPair<int> TranslateGlobalWindowPositionToDisplayLocal(XYPair<int> globalPosition) => globalPosition - GlobalPositionOffset;

	public override string ToString() {
		return Implementation.IsValid(_handle) 
			? $"{nameof(Display)} \"{GetNameAsNewStringObject()}\" ({CurrentResolution.X:#} x {CurrentResolution.Y:#}){(IsPrimary ? " (Primary)" : "")}{(IsRecommended ? " (Recommended)" : "")}"
			: $"{nameof(Display)} [Invalid]";
	}

	#region Equality
	public bool Equals(Display other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is Display other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Display left, Display right) => left.Equals(right);
	public static bool operator !=(Display left, Display right) => !left.Equals(right);
	#endregion
}