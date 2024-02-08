#include "pch.h"
#include "environment/native_impl_loop.h"

#include "utils_and_constants.h"

static sdl_keycode_filter_delegate* keycode_filter_delegate;
static kbm_event_buffer_size_double_delegate* kbm_event_buffer_double_delegate;
static controller_event_buffer_size_double_delegate* controller_event_buffer_double_delegate;

void native_impl_loop::set_event_poll_delegates(sdl_keycode_filter_delegate* keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate* kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate* controllerBufferDoubleDelegate) {
	keycode_filter_delegate = keycodeFilterFuncPtr;
	kbm_event_buffer_double_delegate = kbmBufferDoubleDelegate;
	controller_event_buffer_double_delegate = controllerBufferDoubleDelegate;
}
StartExportedFunc(set_event_poll_delegates, sdl_keycode_filter_delegate* keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate* kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate* controllerBufferDoubleDelegate) {
	native_impl_loop::set_event_poll_delegates(keycodeFilterFuncPtr, kbmBufferDoubleDelegate, controllerBufferDoubleDelegate);
	EndExportedFunc
}

void native_impl_loop::iterate_events(KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten) {
	int32_t numKbmEventsWritten = 0;
	int32_t numControllerEventsWritten = 0;

	SDL_Event event;
	while (SDL_PollEvent(&event)) {
		switch (event.type) {
			case SDL_EventType::SDL_KEYDOWN:
			case SDL_EventType::SDL_KEYUP:
				auto keyEvent = event.key;
				if (!keycode_filter_delegate(keyEvent.keysym.sym)) continue;

				if (numKbmEventsWritten == kbmEventBufferLength) {
					kbmEventBuffer = kbm_event_buffer_double_delegate();
					kbmEventBufferLength *= 2;
				}

				kbmEventBuffer[numKbmEventsWritten].KeyCode = keyEvent.keysym.sym;
				kbmEventBuffer[numKbmEventsWritten].KeyDown = event.type == SDL_EventType::SDL_KEYDOWN;
				numKbmEventsWritten++;
				
				break;

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

