#include "pch.h"
#include "environment/native_impl_display.h"

#include "utils_and_constants.h"
#include "environment/native_impl_window.h"

int32_t native_impl_display::get_display_count() {
	auto result = SDL_GetNumVideoDisplays();
	ThrowIfNotPositive(result, "Can not find any connected displays: ", SDL_GetError());
	return result;
}
StartExportedFunc(get_display_count, int32_t* outCount) {
	*outCount = native_impl_display::get_display_count();
	EndExportedFunc
}

void native_impl_display::get_display_resolution(DisplayHandle handle, int32_t* outWidth, int32_t* outHeight) {
	SDL_Rect outRect;
	auto getBoundsResult = SDL_GetDisplayBounds(handle, &outRect);
	ThrowIfNotZero(getBoundsResult, "Could not get display resolution: ", SDL_GetError());
	*outWidth = outRect.w;
	*outHeight = outRect.h;
}
StartExportedFunc(get_display_resolution, DisplayHandle index, int32_t* outWidth, int32_t* outHeight) {
	native_impl_display::get_display_resolution(index, outWidth, outHeight);
	EndExportedFunc
}

void native_impl_display::get_display_positional_offset(DisplayHandle handle, int32_t* xOffset, int32_t* yOffset) {
	SDL_Rect outRect;
	auto getBoundsResult = SDL_GetDisplayBounds(handle, &outRect);
	ThrowIfNotZero(getBoundsResult, "Could not get display positional offset: ", SDL_GetError());
	*xOffset = outRect.x;
	*yOffset = outRect.y;
}
StartExportedFunc(get_display_positional_offset, DisplayHandle index, int32_t* xOffset, int32_t* yOffset) {
	native_impl_display::get_display_positional_offset(index, xOffset, yOffset);
	EndExportedFunc
}

void native_impl_display::get_display_name(DisplayHandle handle, char* resultBuffer, int32_t bufferLen) {
	auto name = SDL_GetDisplayName(handle);
	interop_utils::safe_copy_string(resultBuffer, bufferLen, name);
}
StartExportedFunc(get_display_name, DisplayHandle index, char* resultBuffer, int32_t bufferLen) {
	native_impl_display::get_display_name(index, resultBuffer, bufferLen);
	EndExportedFunc
}






DisplayHandle native_impl_display::get_primary_display() {
	auto numDisplays = get_display_count();
	ThrowIfNotPositive(numDisplays, "Can not get primary display: No connected displays discovered.");

	for (auto i = 0; i < numDisplays; ++i) {
		int32_t xOffset, yOffset;
		get_display_positional_offset(i, &xOffset, &yOffset);
		if (xOffset == 0 && yOffset == 0) return i;
	}
	
	// If we can't find the primary this way, just return the first display in the list
	return 0;
}
StartExportedFunc(get_primary_display, DisplayHandle* outHandle) {
	auto result = native_impl_display::get_primary_display();
	*outHandle = result;
	EndExportedFunc
}

int32_t native_impl_display::get_display_mode_count(DisplayHandle handle) {
	return SDL_GetNumDisplayModes(handle);
}
StartExportedFunc(get_display_mode_count, DisplayHandle handle, int32_t* outNumDisplayModes) {
	*outNumDisplayModes = native_impl_display::get_display_mode_count(handle);
	EndExportedFunc
}

void native_impl_display::get_display_mode(DisplayHandle handle, int32_t modeIndex, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz) {
	SDL_DisplayMode mode;
	SDL_GetDisplayMode(handle, modeIndex, &mode);
	*outWidth = mode.w;
	*outHeight = mode.h;
	*outRefreshRateHz = mode.refresh_rate;
}
StartExportedFunc(get_display_mode, DisplayHandle handle, int32_t modeIndex, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz) {
	native_impl_display::get_display_mode(handle, modeIndex, outWidth, outHeight, outRefreshRateHz);
	EndExportedFunc
}
