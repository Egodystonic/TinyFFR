// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Desktop;

interface IDisplayHandleImplProvider {
	bool GetIsPrimary(DisplayHandle handle);
	bool GetIsRecommended(DisplayHandle handle);
	XYPair GetResolution(DisplayHandle handle);
	XYPair GetPositionOffset(DisplayHandle handle);
	ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(DisplayHandle handle);
	DisplayMode GetHighestSupportedResolution(DisplayHandle handle);
	DisplayMode GetHighestSupportedRefreshRate(DisplayHandle handle);
	int GetNameMaxLength();
	int GetName(DisplayHandle handle, Span<char> dest);
}