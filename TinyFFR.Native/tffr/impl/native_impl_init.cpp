#include "pch.h"
#include "native_impl_init.h"

#include "utils_and_constants.h"
#include "sdl/SDL.h"
#include "filament/utils/Log.h"
#include "filamat/MaterialBuilder.h"

filament::Engine* filament_engine;
deallocate_asset_buffer_delegate native_impl_init::deallocation_delegate;
log_notify_delegate native_impl_init::log_delegate;

void native_impl_init::initialize_all() {
	auto sdlInitResult = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_GAMECONTROLLER);
	ThrowIfNotZero(sdlInitResult, "Could not initialize SDL: ", SDL_GetError());

	filament_engine_ptr = filament::Engine::Builder()
						  .backend(filament::Engine::Backend::OPENGL)
						  .build();
	ThrowIfNull(filament_engine_ptr, "Could not initialize filament renderer.");

	filamat::MaterialBuilder::init();
}
StartExportedFunc(initialize_all) {
	native_impl_init::initialize_all();
	EndExportedFunc
}


void native_impl_init::set_buffer_deallocation_delegate(deallocate_asset_buffer_delegate deallocationDelegate) {
	ThrowIfNull(deallocationDelegate, "Deallocation delegate was null.");
	deallocation_delegate = deallocationDelegate;
}
StartExportedFunc(set_buffer_deallocation_delegate, deallocate_asset_buffer_delegate deallocationDelegate) {
	native_impl_init::set_buffer_deallocation_delegate(deallocationDelegate);
	EndExportedFunc
}

void native_impl_init::set_log_notify_delegate(log_notify_delegate logNotifyDelegate) {
	ThrowIfNull(logNotifyDelegate, "Log notify delegate was null.");
	log_delegate = logNotifyDelegate;
}
StartExportedFunc(set_log_notify_delegate, log_notify_delegate logNotifyDelegate) {
	native_impl_init::set_log_notify_delegate(logNotifyDelegate);
	EndExportedFunc
}

void native_impl_init::notify_of_log_msg() {
	if (log_delegate == nullptr) return;
	log_delegate();
}
