#pragma once

#include "sdl/SDL.h"

class window_factory {
public:
	static SDL_Window* create_window();
};