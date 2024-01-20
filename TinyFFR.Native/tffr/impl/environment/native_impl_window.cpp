#include "pch.h"
#include "environment/native_impl_window.h"

#include "utils_and_constants.h"

WindowHandle native_impl_window::create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = SDL_CreateWindow(
		"TinyFFR Application", 
		xPos >= 0 ? xPos : SDL_WINDOWPOS_CENTERED, 
		yPos >= 0 ? yPos : SDL_WINDOWPOS_CENTERED, 
		width > 0 ? width : 1024, 
		height > 0 ? height : 1080, 
		SDL_WINDOW_OPENGL
	);
	ThrowIfNull(result, SDL_GetError());
	SDL_ShowWindow(result);
	return result;
}
StartExportedFunc(create_window, WindowHandle* outResult, int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = native_impl_window::create_window(width, height, xPos, yPos);
	*outResult = result;
	EndExportedFunc
}

void native_impl_window::dispose_window(WindowHandle handle) {
	SDL_DestroyWindow(handle);
}
StartExportedFunc(dispose_window, WindowHandle handle) {
	native_impl_window::dispose_window(handle);
	EndExportedFunc
}

