#include "pch.h"
#include "environment/native_impl_window.h"

#include "utils_and_constants.h"

WindowHandle native_impl_window::create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = SDL_CreateWindow(
		"TinyFFR Application", 
		xPos, 
		yPos, 
		width,
		height, 
		SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE
	);
	ThrowIfNull(result, "Could not create window: ", SDL_GetError());
	SDL_ShowWindow(result);
	return result;
}
StartExportedFunc(create_window, WindowHandle* outResult, int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = native_impl_window::create_window(width, height, xPos, yPos);
	*outResult = result;
	EndExportedFunc
}



void native_impl_window::set_window_title(WindowHandle handle, const char* newTitle) {
	SDL_SetWindowTitle(handle, newTitle);
}
StartExportedFunc(set_window_title, WindowHandle ptr, const char* newTitle) {
	native_impl_window::set_window_title(ptr, newTitle);
	EndExportedFunc
}
void native_impl_window::get_window_title(WindowHandle handle, char* resultBuffer, int32_t bufferLen) {
	auto title = SDL_GetWindowTitle(handle);
	strcpy_s(resultBuffer, bufferLen, title);
}
StartExportedFunc(get_window_title, WindowHandle ptr, char* resultBuffer, int32_t bufferLen) {
	native_impl_window::get_window_title(ptr, resultBuffer, bufferLen);
	EndExportedFunc
}



void native_impl_window::set_window_size(WindowHandle handle, int32_t newWidth, int32_t newHeight) {
	SDL_SetWindowSize(handle, newWidth, newHeight);
}
StartExportedFunc(set_window_size, WindowHandle ptr, int32_t newWidth, int32_t newHeight) {
	native_impl_window::set_window_size(ptr, newWidth, newHeight);
	EndExportedFunc
}
void native_impl_window::get_window_size(WindowHandle handle, int32_t* outWidth, int32_t* outHeight) {
	SDL_GetWindowSize(handle, outWidth, outHeight);
}
StartExportedFunc(get_window_size, WindowHandle ptr, int32_t* outWidth, int32_t* outHeight) {
	native_impl_window::get_window_size(ptr, outWidth, outHeight);
	EndExportedFunc
}



void native_impl_window::set_window_position(WindowHandle handle, int32_t newX, int32_t newY) {
	SDL_SetWindowPosition(handle, newX, newY);
}
StartExportedFunc(set_window_position, WindowHandle ptr, int32_t newX, int32_t newY) {
	native_impl_window::set_window_position(ptr, newX, newY);
	EndExportedFunc
}
void native_impl_window::get_window_position(WindowHandle handle, int32_t* outX, int32_t* outY) {
	SDL_GetWindowPosition(handle, outX, outY);
}
StartExportedFunc(get_window_position, WindowHandle ptr, int32_t* outX, int32_t* outY) {
	native_impl_window::get_window_position(ptr, outX, outY);
	EndExportedFunc
}



void native_impl_window::set_window_fullscreen_state(WindowHandle handle, interop_bool fullscreen, interop_bool borderless) {
	auto fsSetResult = SDL_SetWindowFullscreen(handle, fullscreen ? SDL_WINDOW_FULLSCREEN : 0);
	ThrowIfNotZero(fsSetResult, "Could not set fullscreen state of window: ", SDL_GetError());
	SDL_SetWindowBordered(handle, borderless && fullscreen ? SDL_FALSE : SDL_TRUE);
	SDL_SetWindowResizable(handle, (borderless || fullscreen) ? SDL_FALSE : SDL_TRUE);
}
StartExportedFunc(set_window_fullscreen_state, WindowHandle ptr, interop_bool fullscreen, interop_bool borderless) {
	native_impl_window::set_window_fullscreen_state(ptr, fullscreen, borderless);
	EndExportedFunc
}
void native_impl_window::get_window_fullscreen_state(WindowHandle handle, interop_bool* outFullscreen, interop_bool* outBorderless) {
	auto flags = SDL_GetWindowFlags(handle);
	*outFullscreen = (flags & SDL_WINDOW_FULLSCREEN) > 0 ? interop_bool_true : interop_bool_false;
	*outBorderless = (flags & SDL_WINDOW_BORDERLESS) > 0 ? interop_bool_true : interop_bool_false;
}
StartExportedFunc(get_window_fullscreen_state, WindowHandle ptr, interop_bool* outFullscreen, interop_bool* outBorderless) {
	native_impl_window::get_window_fullscreen_state(ptr, outFullscreen, outBorderless);
	EndExportedFunc
}



void native_impl_window::set_window_cursor_lock_state(WindowHandle handle, interop_bool lockState) {
	auto setModeResult = SDL_SetRelativeMouseMode(lockState == interop_bool_true ? SDL_TRUE : SDL_FALSE);
	ThrowIfNotZero(setModeResult, "Could not set relative mouse mode: ", SDL_GetError());
}
StartExportedFunc(set_window_cursor_lock_state, WindowHandle handle, interop_bool lockState) {
	native_impl_window::set_window_cursor_lock_state(handle, lockState);
	EndExportedFunc
}
void native_impl_window::get_window_cursor_lock_state(WindowHandle handle, interop_bool* outLockState) {
	*outLockState = SDL_GetRelativeMouseMode() == SDL_TRUE ? interop_bool_true : interop_bool_false;
}
StartExportedFunc(get_window_cursor_lock_state, WindowHandle handle, interop_bool* outLockState) {
	native_impl_window::get_window_cursor_lock_state(handle, outLockState);
	EndExportedFunc
}



void native_impl_window::dispose_window(WindowHandle handle) {
	SDL_DestroyWindow(handle);
}
StartExportedFunc(dispose_window, WindowHandle handle) {
	native_impl_window::dispose_window(handle);
	EndExportedFunc
}