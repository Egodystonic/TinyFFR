#pragma once

#include "sdl/SDL.h"

typedef SDL_Window* WindowHandle;

class native_impl_window {
public:
	static WindowHandle create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos);
	static void dispose_window(WindowHandle handle);
};