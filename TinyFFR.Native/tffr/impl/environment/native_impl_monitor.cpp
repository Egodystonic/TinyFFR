#include "pch.h"
#include "environment/native_impl_monitor.h"

#include "utils_and_constants.h"

int32_t native_impl_monitor::get_monitor_count() {
	auto result = SDL_GetNumVideoDisplays();
	ThrowIfNotPositive(result, "Can not find any connected monitors. ", SDL_GetError());
	return result;
}
StartExportedFunc(get_monitor_count, int32_t* outCount) {
	*outCount = native_impl_monitor::get_monitor_count();
	EndExportedFunc
}

void native_impl_monitor::get_monitor_resolution(MonitorHandle handle, int32_t* outWidth, int32_t* outHeight) {
	SDL_Rect outRect;
	auto getBoundsResult = SDL_GetDisplayBounds(handle, &outRect);
	ThrowIfNotZero(getBoundsResult, "Could not get monitor resolution. ", SDL_GetError());
	*outWidth = outRect.w;
	*outHeight = outRect.h;
}
StartExportedFunc(get_monitor_resolution, MonitorHandle index, int32_t* outWidth, int32_t* outHeight) {
	native_impl_monitor::get_monitor_resolution(index, outWidth, outHeight);
	EndExportedFunc
}

void native_impl_monitor::get_monitor_positional_offset(MonitorHandle handle, int32_t* xOffset, int32_t* yOffset) {
	SDL_Rect outRect;
	auto getBoundsResult = SDL_GetDisplayBounds(handle, &outRect);
	ThrowIfNotZero(getBoundsResult, "Could not get monitor positional offset. ", SDL_GetError());
	*xOffset = outRect.x;
	*yOffset = outRect.y;
}
StartExportedFunc(get_monitor_positional_offset, MonitorHandle index, int32_t* xOffset, int32_t* yOffset) {
	native_impl_monitor::get_monitor_positional_offset(index, xOffset, yOffset);
	EndExportedFunc
}

void native_impl_monitor::get_monitor_name(MonitorHandle handle, char* resultBuffer, int32_t bufferLen) {
	auto name = SDL_GetDisplayName(handle);
	strcpy_s(resultBuffer, bufferLen, name);
}
StartExportedFunc(get_monitor_name, MonitorHandle index, char* resultBuffer, int32_t bufferLen) {
	native_impl_monitor::get_monitor_name(index, resultBuffer, bufferLen);
	EndExportedFunc
}


