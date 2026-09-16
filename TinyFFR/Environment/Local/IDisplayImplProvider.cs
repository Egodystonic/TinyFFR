// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Display"/> resources.
/// </summary>
public interface IDisplayImplProvider : IResourceImplProvider<Display> {
	bool GetIsPrimary(ResourceHandle<Display> handle);
	ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(ResourceHandle<Display> handle);
	DisplayMode GetHighestSupportedResolutionMode(ResourceHandle<Display> handle);
	DisplayMode GetHighestSupportedRefreshRateMode(ResourceHandle<Display> handle);
	XYPair<int> GetCurrentResolution(ResourceHandle<Display> handle);
	XYPair<int> GetGlobalPositionOffset(ResourceHandle<Display> handle);
	bool IsValid(ResourceHandle<Display> handle);
}