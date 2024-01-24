#pragma once

#include "utils_and_constants.h"

#include "sdl/SDL.h"

typedef int32_t MonitorHandle;

class native_impl_monitor {
public:
	static int32_t get_monitor_count();
	static void get_monitor_resolution(MonitorHandle handle, int32_t* outWidth, int32_t* outHeight);
	static void get_monitor_positional_offset(MonitorHandle handle, int32_t* xOffset, int32_t* yOffset);
	static void get_monitor_name(MonitorHandle handle, char* resultBuffer, int32_t bufferLen);
};