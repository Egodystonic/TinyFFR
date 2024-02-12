#include "pch.h"
#include "environment/native_impl_loop.h"

#include "utils_and_constants.h"
#include "environment/native_impl_window.h"

sdl_keycode_filter_delegate native_impl_loop::keycode_filter_delegate;
kbm_event_buffer_size_double_delegate native_impl_loop::kbm_event_buffer_double_delegate;
controller_event_buffer_size_double_delegate native_impl_loop::controller_event_buffer_double_delegate;
KeyboardOrMouseKeyEvent* native_impl_loop::kbm_event_buffer;
int32_t native_impl_loop::kbm_event_buffer_length;
RawGameControllerButtonEvent* native_impl_loop::controller_event_buffer;
int32_t native_impl_loop::controller_event_buffer_length;

void native_impl_loop::set_event_poll_delegates(sdl_keycode_filter_delegate keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate controllerBufferDoubleDelegate) {
	keycode_filter_delegate = keycodeFilterFuncPtr;
	kbm_event_buffer_double_delegate = kbmBufferDoubleDelegate;
	controller_event_buffer_double_delegate = controllerBufferDoubleDelegate;
}
StartExportedFunc(set_event_poll_delegates, sdl_keycode_filter_delegate keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate controllerBufferDoubleDelegate) {
	native_impl_loop::set_event_poll_delegates(keycodeFilterFuncPtr, kbmBufferDoubleDelegate, controllerBufferDoubleDelegate);
	EndExportedFunc
}
void native_impl_loop::set_event_poll_buffer_pointers(KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength) {
	kbm_event_buffer = kbmEventBuffer;
	kbm_event_buffer_length = kbmEventBufferLength;
	controller_event_buffer = controllerEventBuffer;
	controller_event_buffer_length = controllerEventBufferLength;
}
StartExportedFunc(set_event_poll_buffer_pointers, KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength) {
	native_impl_loop::set_event_poll_buffer_pointers(kbmEventBuffer, kbmEventBufferLength, controllerEventBuffer, controllerEventBufferLength);
	EndExportedFunc
}

void native_impl_loop::iterate_events(int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten, float_t* outMousePosX, float_t* outMousePosY, interop_bool* outQuitRequested) {
	int32_t numKbmEventsWritten = 0;
	int32_t numControllerEventsWritten = 0;
	float_t mousePosX = 0.0;
	float_t mousePosY = 0.0;
	interop_bool quitRequested = interop_bool_false;

	SDL_Event event;
	while (SDL_PollEvent(&event)) {
		switch (event.type) {
			case SDL_EventType::SDL_KEYDOWN:
			case SDL_EventType::SDL_KEYUP:
				auto keyEvent = event.key;
				if (!keycode_filter_delegate(keyEvent.keysym.sym)) continue;

				if (numKbmEventsWritten == kbm_event_buffer_length) {
					kbm_event_buffer = kbm_event_buffer_double_delegate();
					kbm_event_buffer_length *= 2;
				}

				kbm_event_buffer[numKbmEventsWritten].KeyCode = keyEvent.keysym.sym;
				kbm_event_buffer[numKbmEventsWritten].KeyDown = event.type == SDL_EventType::SDL_KEYDOWN;
				numKbmEventsWritten++;
				
				break;


		case SDL_EventType::SDL_MOUSEBUTTONDOWN:
		case SDL_EventType::SDL_MOUSEBUTTONUP:
			auto mouseEvent = event.button;
			if (numKbmEventsWritten == kbm_event_buffer_length) {
				kbm_event_buffer = kbm_event_buffer_double_delegate();
				kbm_event_buffer_length *= 2;
			}

			kbm_event_buffer[numKbmEventsWritten].KeyCode = mouseEvent.
			kbm_event_buffer[numKbmEventsWritten].KeyDown = event.type == SDL_EventType::SDL_KEYDOWN;
			numKbmEventsWritten++;
		}
	}

	*outNumKbmEventsWritten = numKbmEventsWritten;
	*outNumControllerEventsWritten = numControllerEventsWritten;
}
StartExportedFunc(iterate_events, KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten) {
	native_impl_loop::iterate_events(
		kbmEventBuffer,
		kbmEventBufferLength,
		controllerEventBuffer,
		controllerEventBufferLength,
		outNumKbmEventsWritten,
		outNumControllerEventsWritten
	);
	EndExportedFunc
}

