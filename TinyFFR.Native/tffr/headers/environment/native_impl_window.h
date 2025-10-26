#pragma once

#include "native_impl_display.h"
#include "utils_and_constants.h"
#include "sdl/SDL.h"

typedef SDL_Window* WindowHandle;

class native_impl_window {
public:
	static WindowHandle create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos);

	static void set_window_title(WindowHandle handle, const char* newTitle);
	static void get_window_title(WindowHandle handle, char* resultBuffer, int32_t bufferLen);

	static void set_window_icon(WindowHandle handle, const char* iconFilePath);

	static void set_window_size(WindowHandle handle, int32_t newWidth, int32_t newHeight);
	static void get_window_size(WindowHandle handle, int32_t* outWidth, int32_t* outHeight);
	static void set_window_fullscreen_display_mode(WindowHandle window, DisplayHandle display, int32_t modeIndex);
	static void get_window_fullscreen_display_mode(WindowHandle handle, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz);

	static void set_window_position(WindowHandle handle, int32_t newX, int32_t newY);
	static void get_window_position(WindowHandle handle, int32_t* outX, int32_t* outY);

	static void set_window_fullscreen_state(WindowHandle handle, interop_bool fullscreen, interop_bool borderless);
	static void get_window_fullscreen_state(WindowHandle handle, interop_bool* outFullscreen, interop_bool* outBorderless);

	static void set_window_cursor_lock_state(WindowHandle handle, interop_bool lockState);
	static void get_window_cursor_lock_state(WindowHandle handle, interop_bool* outLockState);

	static void dispose_window(WindowHandle handle);
};