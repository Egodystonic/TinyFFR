#pragma once

#include "sdl/SDL.h"

typedef SDL_Window* WindowPtr;

class native_impl_window {
public:
	static WindowPtr create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos);
	static void set_window_title(WindowPtr ptr, const char* newTitle);
	static void get_window_title(WindowPtr ptr, char* resultBuffer, int32_t bufferLen);
	static void dispose_window(WindowPtr ptr);
};