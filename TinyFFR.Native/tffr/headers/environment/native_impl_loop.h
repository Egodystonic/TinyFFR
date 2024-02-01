#pragma once

#include "utils_and_constants.h"
#include "sdl/SDL.h"

typedef interop_bool(*sdl_keycode_filter_delegate)(SDL_Keycode keycode);

class native_impl_loop {
public:
	static void iterate_events(sdl_keycode_filter_delegate* keycodeFilter, );
};