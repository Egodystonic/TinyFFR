#pragma once

#include "utils_and_constants.h"
#include "sdl/SDL.h"
#include "filament/Fence.h"

typedef SDL_GameController* GameControllerHandle;

PushSafeStructPacking
struct KeyboardOrMouseKeyEvent {
	int32_t KeyCode;
	interop_bool KeyDown;
	uint8_t _padding[3];
};
PopSafeStructPacking
static_assert(sizeof(KeyboardOrMouseKeyEvent) == 8);

PushSafeStructPacking
struct RawGameControllerButtonEvent {
	GameControllerHandle Handle;
	int32_t EventType;
	int16_t _padding;
	int16_t NewValue;
};
PopSafeStructPacking
static_assert(sizeof(RawGameControllerButtonEvent) == 16);

PushSafeStructPacking
struct MouseClickEvent {
	int32_t X;
	int32_t Y;
	int32_t EventType;
	int32_t ClickCount;
};
PopSafeStructPacking
static_assert(sizeof(MouseClickEvent) == 16);


typedef interop_bool(*sdl_keycode_filter_translate_delegate)(SDL_Keycode* keycodeRef);
typedef KeyboardOrMouseKeyEvent*(*kbm_event_buffer_size_double_delegate)();
typedef RawGameControllerButtonEvent*(*controller_event_buffer_size_double_delegate)();
typedef MouseClickEvent*(*click_event_buffer_size_double_delegate)();
typedef void(*handle_new_controller_delegate)(GameControllerHandle handle, const char* utf8NamePtr, int32_t utf8NameLen);

class native_impl_loop {
public:
	static sdl_keycode_filter_translate_delegate keycode_filter_translate_delegate;
	static kbm_event_buffer_size_double_delegate kbm_event_buffer_double_delegate;
	static controller_event_buffer_size_double_delegate controller_event_buffer_double_delegate;
	static click_event_buffer_size_double_delegate click_event_buffer_double_delegate;
	static handle_new_controller_delegate handle_controller_delegate;

	static KeyboardOrMouseKeyEvent* kbm_event_buffer;
	static int32_t kbm_event_buffer_length;
	static RawGameControllerButtonEvent* controller_event_buffer;
	static int32_t controller_event_buffer_length;
	static MouseClickEvent* click_event_buffer;
	static int32_t click_event_buffer_length;

	static void detect_controllers();
	static void iterate_events(int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten, int32_t* outNumClickEventsWritten, int32_t* outMousePosX, int32_t* outMousePosY, int32_t* outMouseDeltaX, int32_t* outMouseDeltaY, interop_bool* outQuitRequested);
	static void set_event_poll_delegates(sdl_keycode_filter_translate_delegate keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate controllerBufferDoubleDelegate, click_event_buffer_size_double_delegate clickBufferDoubleDelegate, handle_new_controller_delegate handleControllerDelegate);
	static void set_event_poll_buffer_pointers(KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, MouseClickEvent* clickEventBuffer, int32_t clickEventBufferLength);
};