#include "pch.h"
#include "environment/native_impl_window.h"

#include "utils_and_constants.h"

WindowPtr native_impl_window::create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
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
StartExportedFunc(create_window, WindowPtr* outResult, int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = native_impl_window::create_window(width, height, xPos, yPos);
	*outResult = result;
	EndExportedFunc
}



void native_impl_window::set_window_title(WindowPtr ptr, const char* newTitle) {
	SDL_SetWindowTitle(ptr, newTitle);
}
StartExportedFunc(set_window_title, WindowPtr ptr, const char* newTitle) {
	native_impl_window::set_window_title(ptr, newTitle);
	EndExportedFunc
}
void native_impl_window::get_window_title(WindowPtr ptr, char* resultBuffer, int32_t bufferLen) {
	auto title = SDL_GetWindowTitle(ptr);
	strcpy_s(resultBuffer, bufferLen, title);
}
StartExportedFunc(get_window_title, WindowPtr ptr, char* resultBuffer, int32_t bufferLen) {
	native_impl_window::get_window_title(ptr, resultBuffer, bufferLen);
	EndExportedFunc
}



void native_impl_window::set_window_size(WindowPtr ptr, int32_t newWidth, int32_t newHeight) {
	SDL_SetWindowSize(ptr, newWidth, newHeight);
}
StartExportedFunc(set_window_size, WindowPtr ptr, int32_t newWidth, int32_t newHeight) {
	native_impl_window::set_window_size(ptr, newWidth, newHeight);
	EndExportedFunc
}
void native_impl_window::get_window_size(WindowPtr ptr, int32_t* outWidth, int32_t* outHeight) {
	SDL_GetWindowSize(ptr, outWidth, outHeight);
}
StartExportedFunc(get_window_size, WindowPtr ptr, int32_t* outWidth, int32_t* outHeight) {
	native_impl_window::get_window_size(ptr, outWidth, outHeight);
	EndExportedFunc
}



void native_impl_window::set_window_position(WindowPtr ptr, int32_t newX, int32_t newY) {
	SDL_SetWindowPosition(ptr, newX, newY);
}
StartExportedFunc(set_window_position, WindowPtr ptr, int32_t newX, int32_t newY) {
	native_impl_window::set_window_position(ptr, newX, newY);
	EndExportedFunc
}
void native_impl_window::get_window_position(WindowPtr ptr, int32_t* outX, int32_t* outY) {
	SDL_GetWindowPosition(ptr, outX, outY);
}
StartExportedFunc(get_window_position, WindowPtr ptr, int32_t* outX, int32_t* outY) {
	native_impl_window::get_window_position(ptr, outX, outY);
	EndExportedFunc
}




void native_impl_window::dispose_window(WindowPtr ptr) {
	SDL_DestroyWindow(ptr);
}
StartExportedFunc(dispose_window, WindowPtr handle) {
	native_impl_window::dispose_window(handle);
	EndExportedFunc
}

