#include "pch.h"
#include "native_impl_init.h"

#include "utils_and_constants.h"
#include "sdl/SDL.h"
#include "filament/utils/Log.h"

filament::Engine* native_impl_init::filament_engine_ptr;
deallocate_asset_buffer_delegate native_impl_init::deallocation_delegate;
log_notify_delegate native_impl_init::log_delegate;

void native_impl_init::exec_once_only_initialization() {
	SDL_SetHint(SDL_HINT_WINDOWS_DPI_AWARENESS, "permonitorv2");
	auto sdlInitResult = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_GAMECONTROLLER);
	ThrowIfNotZero(sdlInitResult, "Could not initialize SDL: ", SDL_GetError());
}
StartExportedFunc(exec_once_only_initialization) {
	native_impl_init::exec_once_only_initialization();
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

void native_impl_init::on_factory_build(interop_bool enableVsync) {
	auto config = filament::Engine::Config{
		.disableVsync = enableVsync ? false : true
	};

	filament_engine_ptr = filament::Engine::Builder()
		.backend(filament::Engine::Backend::OPENGL)
		.featureLevel(filament::Engine::FeatureLevel::FEATURE_LEVEL_3)
		.config(&config)
		.build();

	ThrowIfNull(filament_engine_ptr, "Could not initialize filament.");
}
StartExportedFunc(on_factory_build, interop_bool enableVsync) {
	native_impl_init::on_factory_build(enableVsync);
	EndExportedFunc
}
void native_impl_init::on_factory_teardown() {
	if (filament_engine_ptr == nullptr) return;
	filament_engine_ptr->destroy(&filament_engine_ptr);
}
StartExportedFunc(on_factory_teardown) {
	native_impl_init::on_factory_teardown();
	EndExportedFunc
}