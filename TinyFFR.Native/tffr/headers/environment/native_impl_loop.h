#pragma once

#include "utils_and_constants.h"
#include "sdl/SDL.h"

typedef SDL_GameController* GameControllerHandle;

PushSafeStructPacking
struct KeyboardOrMouseKeyEvent {
	int32_t KeyCode;
	interop_bool KeyDown;
private:
	uint8_t padding[3];
};
PopSafeStructPacking
static_assert(sizeof(KeyboardOrMouseKeyEvent) == 8);

PushSafeStructPacking
struct RawGameControllerButtonEvent {
	GameControllerHandle Handle;
	int32_t EventType;
	int16_t NewValueX;
	int16_t NewValueY;
};
PopSafeStructPacking
static_assert(sizeof(RawGameControllerButtonEvent) == 16);


typedef interop_bool(*sdl_keycode_filter_delegate)(SDL_Keycode keycode);
typedef KeyboardOrMouseKeyEvent*(*kbm_event_buffer_size_double_delegate)();
typedef RawGameControllerButtonEvent*(*controller_event_buffer_size_double_delegate)();

class native_impl_loop {
public:
	static void iterate_events(KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten);
	static void set_event_poll_delegates(sdl_keycode_filter_delegate* keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate* kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate* controllerBufferDoubleDelegate);
};