#include "pch.h"
#include "native_impl_init.h"

#include "utils_and_constants.h"
#include "sdl/SDL.h"

void native_impl_init::initialize_all() {
	auto sdlInitResult = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_GAMECONTROLLER);
	ThrowIfNotZero(sdlInitResult, "Could not initialize SDL: ", SDL_GetError());
}
StartExportedFunc(initialize_all) {
	native_impl_init::initialize_all();
	EndExportedFunc
}


