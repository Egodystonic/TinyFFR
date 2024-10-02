#include "pch.h"
#include "native_impl_init.h"

#include "utils_and_constants.h"
#include "sdl/SDL.h"
#include "filament/utils/Log.h"

filament::Engine* native_impl_init::filament_engine_ptr;
deallocate_asset_buffer_delegate native_impl_init::deallocation_delegate;

void native_impl_init::initialize_all() {
	auto sdlInitResult = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_GAMECONTROLLER);
	ThrowIfNotZero(sdlInitResult, "Could not initialize SDL: ", SDL_GetError());

	filament_engine_ptr = filament::Engine::Builder()
						  .backend(filament::Engine::Backend::VULKAN)
						  .build();
	ThrowIfNull(filament_engine_ptr, "Could not initialize filament renderer.");
}
StartExportedFunc(initialize_all) {
	native_impl_init::initialize_all();
	EndExportedFunc
}


void native_impl_init::set_buffer_deallocation_delegate(deallocate_asset_buffer_delegate deallocationDelegate) {
	deallocation_delegate = deallocationDelegate;
}
StartExportedFunc(set_buffer_deallocation_delegate, deallocate_asset_buffer_delegate deallocationDelegate) {
	native_impl_init::set_buffer_deallocation_delegate(deallocationDelegate);
	EndExportedFunc
}