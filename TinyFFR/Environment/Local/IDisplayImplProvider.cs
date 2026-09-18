// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Display"/> resources.
/// </summary>
public interface IDisplayImplProvider : IResourceImplProvider<Display> {
	/// <summary>
	/// Invoked via <see cref="Display.IsPrimary"/>.
	/// </summary>
	bool GetIsPrimary(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked via <see cref="Display.SupportedDisplayModes"/>.
	/// </summary>
	ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked via <see cref="Display.HighestSupportedResolutionMode"/>.
	/// </summary>
	DisplayMode GetHighestSupportedResolutionMode(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked via <see cref="Display.HighestSupportedRefreshRateMode"/>.
	/// </summary>
	DisplayMode GetHighestSupportedRefreshRateMode(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked via <see cref="Display.CurrentResolution"/>.
	/// </summary>
	XYPair<int> GetCurrentResolution(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked internally to translate window positions between the desktop-wide coordinate space and the coordinate space of a single display; i.e. this returns
	/// where the given display's top-left corner sits relative to the desktop's origin.
	/// </summary>
	XYPair<int> GetGlobalPositionOffset(ResourceHandle<Display> handle);
	/// <summary>
	/// Invoked internally to determine whether the given handle still refers to a display known to this provider.
	/// </summary>
	bool IsValid(ResourceHandle<Display> handle);
}