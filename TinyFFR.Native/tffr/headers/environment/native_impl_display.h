#pragma once

#include "utils_and_constants.h"

#include "sdl/SDL.h"

typedef int32_t DisplayHandle;

class native_impl_display {
public:
	static int32_t get_display_count();
	static void get_display_resolution(DisplayHandle handle, int32_t* outWidth, int32_t* outHeight);
	static void get_display_positional_offset(DisplayHandle handle, int32_t* xOffset, int32_t* yOffset);
	static void get_display_name(DisplayHandle handle, char* resultBuffer, int32_t bufferLen);
	static DisplayHandle get_recommended_display();
	static DisplayHandle get_primary_display();
	static int32_t get_display_mode_count(DisplayHandle handle);
	static void get_display_mode(DisplayHandle handle, int32_t modeIndex, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz);
};