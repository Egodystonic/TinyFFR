#include "pch.h"
#include "environment/native_impl_window.h"

#define STBI_FAILURE_USERMSG
#include "stb/stb_image.h"

#include "utils_and_constants.h"

WindowHandle native_impl_window::create_window(int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	auto result = SDL_CreateWindow(
		"TinyFFR Application", 
		xPos, 
		yPos, 
		width,
		height, 
		SDL_WINDOW_VULKAN | SDL_WINDOW_RESIZABLE
	);
	ThrowIfNull(result, "Could not create window: ", SDL_GetError());
	SDL_ShowWindow(result);
	return result;
}
StartExportedFunc(create_window, WindowHandle* outResult, int32_t width, int32_t height, int32_t xPos, int32_t yPos) {
	ThrowIfNull(outResult, "Window out result pointer was null.");
	auto result = native_impl_window::create_window(width, height, xPos, yPos);
	*outResult = result;
	EndExportedFunc
}

void native_impl_window::set_window_icon(WindowHandle handle, const char* iconFilePath) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(iconFilePath, "File path pointer was null.");

	int width, height, channelCount;
	stbi_set_flip_vertically_on_load(false);
	auto imageData = stbi_load(iconFilePath, &width, &height, &channelCount, 4);
	ThrowIfNull(imageData, "Could not load icon '", iconFilePath, "': ", stbi_failure_reason());

	if (width > 128 || height > 128)
	{
		// Why? Because X11 can't handle larger files.
		// We could check this only on Linux/X11 systems but then we have an inconsistency across platforms which I think is worse.
		stbi_image_free(imageData);
		Throw("Window icon file dimensions can not be larger than 128x128.");
	}

	auto sdlSurface = SDL_CreateRGBSurfaceFrom(imageData, width, height, 32, width * 4, 0xFFU, 0xFF00U, 0xFF0000U, 0xFF000000U);
	if (sdlSurface == nullptr) {
		stbi_image_free(imageData);
		Throw("Could not load icon '", iconFilePath, "': ", SDL_GetError());
	}

	SDL_SetWindowIcon(handle, sdlSurface);
	SDL_FreeSurface(sdlSurface);
	stbi_image_free(imageData);
}
StartExportedFunc(set_window_icon, WindowHandle handle, const char* iconFilePath) {
	native_impl_window::set_window_icon(handle, iconFilePath);
	EndExportedFunc
}

StartExportedFunc(set_window_icon_from_memory, WindowHandle handle, stbi_uc* data, int sizeBytes) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(data, "Data pointer was null.");

	int width, height, channelCount;
	stbi_set_flip_vertically_on_load(false);
	auto imageData = stbi_load_from_memory(data, sizeBytes, &width, &height, &channelCount, 4);
	ThrowIfNull(imageData, "Could not load icon: ", stbi_failure_reason());

	auto sdlSurface = SDL_CreateRGBSurfaceFrom(imageData, width, height, 32, width * 4, 0xFFU, 0xFF00U, 0xFF0000U, 0xFF000000U);
	if (sdlSurface == nullptr) {
		stbi_image_free(imageData);
		Throw("Could not load icon: ", SDL_GetError());
	}

	SDL_SetWindowIcon(handle, sdlSurface);
	SDL_FreeSurface(sdlSurface);
	stbi_image_free(imageData);
	EndExportedFunc
}


void native_impl_window::set_window_title(WindowHandle handle, const char* newTitle) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(newTitle, "Title was null.");
	SDL_SetWindowTitle(handle, newTitle);
}
StartExportedFunc(set_window_title, WindowHandle ptr, const char* newTitle) {
	native_impl_window::set_window_title(ptr, newTitle);
	EndExportedFunc
}
void native_impl_window::get_window_title(WindowHandle handle, char* resultBuffer, int32_t bufferLen) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(resultBuffer, "Result buffer was null.");
	ThrowIfNegative(bufferLen, "Buffer length was negative.");
	auto title = SDL_GetWindowTitle(handle);
	interop_utils::safe_copy_string(resultBuffer, bufferLen, title);
}
StartExportedFunc(get_window_title, WindowHandle ptr, char* resultBuffer, int32_t bufferLen) {
	native_impl_window::get_window_title(ptr, resultBuffer, bufferLen);
	EndExportedFunc
}



void native_impl_window::set_window_size(WindowHandle handle, int32_t newWidth, int32_t newHeight) {
	ThrowIfNull(handle, "Window was null.");
	SDL_SetWindowSize(handle, newWidth, newHeight);
}
StartExportedFunc(set_window_size, WindowHandle ptr, int32_t newWidth, int32_t newHeight) {
	native_impl_window::set_window_size(ptr, newWidth, newHeight);
	EndExportedFunc
}
void native_impl_window::get_window_size(WindowHandle handle, int32_t* outWidth, int32_t* outHeight) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outWidth, "Width out pointer was null.");
	ThrowIfNull(outHeight, "Height out pointer was null.");
	SDL_GetWindowSize(handle, outWidth, outHeight);
}
StartExportedFunc(get_window_size, WindowHandle ptr, int32_t* outWidth, int32_t* outHeight) {
	native_impl_window::get_window_size(ptr, outWidth, outHeight);
	EndExportedFunc
}
void native_impl_window::set_window_fullscreen_display_mode(WindowHandle window, DisplayHandle display, int32_t modeIndex) {
	ThrowIfNull(window, "Window was null.");
	SDL_DisplayMode mode;
	ThrowIfNotZero(SDL_GetDisplayMode(display, modeIndex, &mode), "Could not get display mode data: ", SDL_GetError());
	ThrowIfNotZero(SDL_SetWindowDisplayMode(window, &mode), "Could not set window display mode: ", SDL_GetError());
}
StartExportedFunc(set_window_fullscreen_display_mode, WindowHandle window, DisplayHandle display, int32_t modeIndex) {
	native_impl_window::set_window_fullscreen_display_mode(window, display, modeIndex);
	EndExportedFunc
}
void native_impl_window::get_window_fullscreen_display_mode(WindowHandle handle, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outWidth, "Out width pointer was null.");
	ThrowIfNull(outHeight, "Out height pointer was null.");
	ThrowIfNull(outRefreshRateHz, "Out refresh rate pointer was null.");

	SDL_DisplayMode mode;
	ThrowIfNotZero(SDL_GetWindowDisplayMode(handle, &mode), "Could not get display mode data: ", SDL_GetError());
	*outWidth = mode.w;
	*outHeight = mode.h;
	*outRefreshRateHz = mode.refresh_rate;
}
StartExportedFunc(get_window_fullscreen_display_mode, WindowHandle handle, int32_t* outWidth, int32_t* outHeight, int32_t* outRefreshRateHz) {
	native_impl_window::get_window_fullscreen_display_mode(handle, outWidth, outHeight, outRefreshRateHz);
	EndExportedFunc
}
void native_impl_window::get_window_back_buffer_size_actual(WindowHandle handle, int32_t* outWidth, int32_t* outHeight) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outWidth, "Width out pointer was null.");
	ThrowIfNull(outHeight, "Height out pointer was null.");
	//SDL_GL_GetDrawableSize(handle, outWidth, outHeight); // Theoretically an outdated API -- can try this though if we see issues with GetWindowSizeInPixels
	SDL_GetWindowSizeInPixels(handle, outWidth, outHeight);
}
StartExportedFunc(get_window_back_buffer_size_actual, WindowHandle ptr, int32_t* outWidth, int32_t* outHeight) {
	native_impl_window::get_window_back_buffer_size_actual(ptr, outWidth, outHeight);
	EndExportedFunc
}


void native_impl_window::set_window_position(WindowHandle handle, int32_t newX, int32_t newY) {
	ThrowIfNull(handle, "Window was null.");
	SDL_SetWindowPosition(handle, newX, newY);
}
StartExportedFunc(set_window_position, WindowHandle ptr, int32_t newX, int32_t newY) {
	native_impl_window::set_window_position(ptr, newX, newY);
	EndExportedFunc
}
void native_impl_window::get_window_position(WindowHandle handle, int32_t* outX, int32_t* outY) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outX, "OutX pointer was null.");
	ThrowIfNull(outY, "OutY pointer was null.");
	SDL_GetWindowPosition(handle, outX, outY);
}
StartExportedFunc(get_window_position, WindowHandle ptr, int32_t* outX, int32_t* outY) {
	native_impl_window::get_window_position(ptr, outX, outY);
	EndExportedFunc
}



void native_impl_window::set_window_fullscreen_state(WindowHandle handle, interop_bool fullscreen, interop_bool borderless) {
	ThrowIfNull(handle, "Window was null.");
	int fsSetResult;
	if (!fullscreen) fsSetResult = SDL_SetWindowFullscreen(handle, 0);
	else if (!borderless) fsSetResult = SDL_SetWindowFullscreen(handle, SDL_WINDOW_FULLSCREEN);
	else fsSetResult = SDL_SetWindowFullscreen(handle, SDL_WINDOW_FULLSCREEN_DESKTOP);
	
	ThrowIfNotZero(fsSetResult, "Could not set fullscreen state of window: ", SDL_GetError());
}
StartExportedFunc(set_window_fullscreen_state, WindowHandle ptr, interop_bool fullscreen, interop_bool borderless) {
	native_impl_window::set_window_fullscreen_state(ptr, fullscreen, borderless);
	EndExportedFunc
}
void native_impl_window::get_window_fullscreen_state(WindowHandle handle, interop_bool* outFullscreen, interop_bool* outBorderless) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outFullscreen, "Out fullscreen pointer was null.");
	ThrowIfNull(outBorderless, "Out borderless pointer was null.");
	auto flags = SDL_GetWindowFlags(handle);
	auto isFullscreen = (flags & SDL_WINDOW_FULLSCREEN) != 0;
	auto isBorderless = (flags & SDL_WINDOW_FULLSCREEN_DESKTOP) > SDL_WINDOW_FULLSCREEN;
	*outFullscreen = (isFullscreen || isBorderless) ? interop_bool_true : interop_bool_false;
	*outBorderless = isBorderless ? interop_bool_true : interop_bool_false;
}
StartExportedFunc(get_window_fullscreen_state, WindowHandle ptr, interop_bool* outFullscreen, interop_bool* outBorderless) {
	native_impl_window::get_window_fullscreen_state(ptr, outFullscreen, outBorderless);
	EndExportedFunc
}



void native_impl_window::set_window_cursor_lock_state(WindowHandle handle, interop_bool lockState) {
	ThrowIfNull(handle, "Window was null.");
	auto setModeResult = SDL_SetRelativeMouseMode(lockState == interop_bool_true ? SDL_TRUE : SDL_FALSE);
	ThrowIfNotZero(setModeResult, "Could not set relative mouse mode: ", SDL_GetError());
}
StartExportedFunc(set_window_cursor_lock_state, WindowHandle handle, interop_bool lockState) {
	native_impl_window::set_window_cursor_lock_state(handle, lockState);
	EndExportedFunc
}
void native_impl_window::get_window_cursor_lock_state(WindowHandle handle, interop_bool* outLockState) {
	ThrowIfNull(handle, "Window was null.");
	ThrowIfNull(outLockState, "Out lock state pointer was null.");
	*outLockState = SDL_GetRelativeMouseMode() == SDL_TRUE ? interop_bool_true : interop_bool_false;
}
StartExportedFunc(get_window_cursor_lock_state, WindowHandle handle, interop_bool* outLockState) {
	native_impl_window::get_window_cursor_lock_state(handle, outLockState);
	EndExportedFunc
}



void native_impl_window::dispose_window(WindowHandle handle) {
	ThrowIfNull(handle, "Window was null.");
	SDL_DestroyWindow(handle);
}
StartExportedFunc(dispose_window, WindowHandle handle) {
	native_impl_window::dispose_window(handle);
	EndExportedFunc
}