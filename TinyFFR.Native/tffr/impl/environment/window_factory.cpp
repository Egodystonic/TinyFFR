#include "pch.h"
#include "environment/window_factory.h"

#include "utils_and_constants.h"
#include "interop_bool.h"

SDL_Window* window_factory::create_window() {
	return SDL_CreateWindow("TinyFFR Application", SDL_WINDOWPOS_CENTERED, 100, 800, 600, 0U);
}

StartExportedFunc(WindowFactoryCreateWindow) {
	auto w = window_factory::create_window();
	SDL_ShowWindow(w);
	throw std::exception{ "test" };
	EndExportedFunc
}
