// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IDisplayImplProvider {
	bool GetIsPrimary(DisplayHandle handle);
	bool GetIsRecommended(DisplayHandle handle);
	ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(DisplayHandle handle);
	DisplayMode GetHighestSupportedResolutionMode(DisplayHandle handle);
	DisplayMode GetHighestSupportedRefreshRateMode(DisplayHandle handle);
	XYPair<int> GetCurrentResolution(DisplayHandle handle);
	XYPair<int> GetGlobalPositionOffset(DisplayHandle handle);
	string GetName(DisplayHandle handle);
	int GetNameUsingSpan(DisplayHandle handle, Span<char> dest);
	int GetNameSpanMaxLength(DisplayHandle handle);
}