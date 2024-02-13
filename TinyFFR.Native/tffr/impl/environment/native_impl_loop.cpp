#include "pch.h"
#include "environment/native_impl_loop.h"

#include "utils_and_constants.h"

static const char* GameControllerNameFallbackValue = "Misc Game Controller";
static GameControllerHandle* _gameControllerList = nullptr;
static int32_t _gameControllerListLen = 0;

sdl_keycode_filter_delegate native_impl_loop::keycode_filter_delegate;
kbm_event_buffer_size_double_delegate native_impl_loop::kbm_event_buffer_double_delegate;
controller_event_buffer_size_double_delegate native_impl_loop::controller_event_buffer_double_delegate;
click_event_buffer_size_double_delegate native_impl_loop::click_event_buffer_double_delegate;
handle_new_controller_delegate native_impl_loop::handle_controller_delegate;
KeyboardOrMouseKeyEvent* native_impl_loop::kbm_event_buffer;
int32_t native_impl_loop::kbm_event_buffer_length;
RawGameControllerButtonEvent* native_impl_loop::controller_event_buffer;
int32_t native_impl_loop::controller_event_buffer_length;
MouseClickEvent* native_impl_loop::click_event_buffer;
int32_t native_impl_loop::click_event_buffer_length;

void native_impl_loop::set_event_poll_delegates(sdl_keycode_filter_delegate keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate controllerBufferDoubleDelegate, click_event_buffer_size_double_delegate clickBufferDoubleDelegate, handle_new_controller_delegate handleControllerDelegate) {
	keycode_filter_delegate = keycodeFilterFuncPtr;
	kbm_event_buffer_double_delegate = kbmBufferDoubleDelegate;
	controller_event_buffer_double_delegate = controllerBufferDoubleDelegate;
	click_event_buffer_double_delegate = clickBufferDoubleDelegate;
	handle_controller_delegate = handleControllerDelegate;
}
StartExportedFunc(set_event_poll_delegates, sdl_keycode_filter_delegate keycodeFilterFuncPtr, kbm_event_buffer_size_double_delegate kbmBufferDoubleDelegate, controller_event_buffer_size_double_delegate controllerBufferDoubleDelegate, click_event_buffer_size_double_delegate clickBufferDoubleDelegate, handle_new_controller_delegate handleControllerDelegate) {
	native_impl_loop::set_event_poll_delegates(keycodeFilterFuncPtr, kbmBufferDoubleDelegate, controllerBufferDoubleDelegate, clickBufferDoubleDelegate, handleControllerDelegate);
	EndExportedFunc
}
void native_impl_loop::set_event_poll_buffer_pointers(KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, MouseClickEvent* clickEventBuffer, int32_t clickEventBufferLength) {
	kbm_event_buffer = kbmEventBuffer;
	kbm_event_buffer_length = kbmEventBufferLength;
	controller_event_buffer = controllerEventBuffer;
	controller_event_buffer_length = controllerEventBufferLength;
	click_event_buffer = clickEventBuffer;
	click_event_buffer_length = clickEventBufferLength;
}
StartExportedFunc(set_event_poll_buffer_pointers, KeyboardOrMouseKeyEvent* kbmEventBuffer, int32_t kbmEventBufferLength, RawGameControllerButtonEvent* controllerEventBuffer, int32_t controllerEventBufferLength, MouseClickEvent* clickEventBuffer, int32_t clickEventBufferLength) {
	native_impl_loop::set_event_poll_buffer_pointers(kbmEventBuffer, kbmEventBufferLength, controllerEventBuffer, controllerEventBufferLength, clickEventBuffer, clickEventBufferLength);
	EndExportedFunc
}

void push_new_controller(GameControllerHandle handle) {
	auto controllerName = SDL_GameControllerName(handle);
	if (controllerName == nullptr) controllerName = GameControllerNameFallbackValue;
	auto controllerNameLen = strlen(controllerName);
	if (controllerNameLen >= INT32_MAX) { // To prevent issues narrowing size_t to int32_t below. A controller with a name length of 2 billion chars is nonsense but for security's sake I'll check anyway
		controllerName = GameControllerNameFallbackValue;
		controllerNameLen = strlen(controllerName);
	}



	auto newGameControllerList = new GameControllerHandle[_gameControllerListLen + 1];
	for (auto i = 0; i < _gameControllerListLen; ++i) {
		newGameControllerList[i] = _gameControllerList[i];
	}
	newGameControllerList[_gameControllerListLen] = handle;
	_gameControllerListLen++;

	if (_gameControllerList != nullptr) delete[] _gameControllerList;
	_gameControllerList = newGameControllerList;



	native_impl_loop::handle_controller_delegate(handle, controllerName, static_cast<int32_t>(controllerNameLen));
}
void append_kbm_event(int32_t numEventsWrittenSoFar, int32_t keyCode, interop_bool keyDown) {
	if (numEventsWrittenSoFar == native_impl_loop::kbm_event_buffer_length) {
		if (native_impl_loop::kbm_event_buffer_length > (INT32_MAX / 2) - 1) Throw("Can not expand KBM event buffer any more.");
		native_impl_loop::kbm_event_buffer = native_impl_loop::kbm_event_buffer_double_delegate();
		native_impl_loop::kbm_event_buffer_length *= 2;
	}

	native_impl_loop::kbm_event_buffer[numEventsWrittenSoFar].KeyCode = keyCode;
	native_impl_loop::kbm_event_buffer[numEventsWrittenSoFar].KeyDown = keyDown;
}
void append_controller_event(int32_t numEventsWrittenSoFar, GameControllerHandle handle, int32_t eventCode, int16_t value) {
	if (numEventsWrittenSoFar == native_impl_loop::controller_event_buffer_length) {
		if (native_impl_loop::controller_event_buffer_length > (INT32_MAX / 2) - 1) Throw("Can not expand controller event buffer any more.");
		native_impl_loop::controller_event_buffer = native_impl_loop::controller_event_buffer_double_delegate();
		native_impl_loop::controller_event_buffer_length *= 2;
	}

	native_impl_loop::controller_event_buffer[numEventsWrittenSoFar].Handle = handle;
	native_impl_loop::controller_event_buffer[numEventsWrittenSoFar].EventType = eventCode;
	native_impl_loop::controller_event_buffer[numEventsWrittenSoFar].NewValue = value;
}
void append_controller_event(int32_t numEventsWrittenSoFar, SDL_JoystickID id, int32_t eventCode, int16_t value) {
	for (auto i = 0; i < _gameControllerListLen; ++i) {
		auto handle = _gameControllerList[i];
		if (SDL_JoystickInstanceID(SDL_GameControllerGetJoystick(handle)) == id) {
			append_controller_event(numEventsWrittenSoFar, handle, eventCode, value);
			return;
		}
	}
}
void append_click_event(int32_t numEventsWrittenSoFar, int32_t x, int32_t y, int32_t keyCode, int32_t clickCount) {
	if (numEventsWrittenSoFar == native_impl_loop::click_event_buffer_length) {
		if (native_impl_loop::click_event_buffer_length > (INT32_MAX / 2) - 1) Throw("Can not expand click event buffer any more.");
		native_impl_loop::click_event_buffer = native_impl_loop::click_event_buffer_double_delegate();
		native_impl_loop::click_event_buffer_length *= 2;
	}

	native_impl_loop::click_event_buffer[numEventsWrittenSoFar].X = x;
	native_impl_loop::click_event_buffer[numEventsWrittenSoFar].Y = y;
	native_impl_loop::click_event_buffer[numEventsWrittenSoFar].EventType = keyCode;
	native_impl_loop::click_event_buffer[numEventsWrittenSoFar].ClickCount = clickCount;
}
void native_impl_loop::iterate_events(int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten, int32_t* outNumClickEventsWritten, int32_t* outMousePosX, int32_t* outMousePosY, interop_bool* outQuitRequested) {
	static constexpr int32_t NonSdlKeyStartValue = 380;
	static constexpr int32_t RawGameControllerAxisEventStartValue = 200;
	int32_t numKbmEventsWritten = 0;
	int32_t numControllerEventsWritten = 0;
	int32_t numClickEventsWritten = 0;
	int32_t mousePosX = INT32_MIN;
	int32_t mousePosY = INT32_MIN;
	interop_bool quitRequested = interop_bool_false;

	SDL_Event event;
	while (SDL_PollEvent(&event)) {
		switch (event.type) {
			case SDL_EventType::SDL_MOUSEMOTION: {
				auto motionEvent = event.motion;
				mousePosX = motionEvent.x;
				mousePosY = motionEvent.y;
				break;
			}

			case SDL_EventType::SDL_CONTROLLERAXISMOTION:{
				auto axisEvent = event.caxis;
				append_controller_event(numControllerEventsWritten++, axisEvent.which, RawGameControllerAxisEventStartValue + axisEvent.axis, axisEvent.value);
				break;
			}

			case SDL_EventType::SDL_KEYDOWN:
			case SDL_EventType::SDL_KEYUP: {
				auto keyEvent = event.key;
				if (!keycode_filter_delegate(keyEvent.keysym.sym) || keyEvent.repeat > 0) continue;
				append_kbm_event(numKbmEventsWritten++, keyEvent.keysym.sym, keyEvent.type == SDL_EventType::SDL_KEYDOWN);
				break;
			}


			case SDL_EventType::SDL_MOUSEBUTTONDOWN:
			case SDL_EventType::SDL_MOUSEBUTTONUP: {
				auto mouseEvent = event.button;
				append_kbm_event(numKbmEventsWritten++, (NonSdlKeyStartValue - 1) + mouseEvent.button, mouseEvent.type == SDL_EventType::SDL_MOUSEBUTTONDOWN);
				if (mouseEvent.type == SDL_EventType::SDL_MOUSEBUTTONDOWN) {
					append_click_event(numClickEventsWritten++, mouseEvent.x, mouseEvent.y, (NonSdlKeyStartValue - 1) + mouseEvent.button, mouseEvent.clicks);
				}
				break;
			}

			case SDL_EventType::SDL_CONTROLLERBUTTONDOWN:
			case SDL_EventType::SDL_CONTROLLERBUTTONUP:{
				auto buttonEvent = event.cbutton;
				append_controller_event(numControllerEventsWritten++, buttonEvent.which, buttonEvent.button, buttonEvent.type == SDL_EventType::SDL_CONTROLLERBUTTONDOWN ? INT16_MAX : 0);
				break;
			}

			case SDL_EventType::SDL_MOUSEWHEEL: {
				auto wheelEvent = event.wheel;
				auto keyCode = (NonSdlKeyStartValue + 5) + ((wheelEvent.y * (wheelEvent.direction == SDL_MOUSEWHEEL_FLIPPED ? -1 : 1)) > 0 ? 0 : 1);
				for (auto i = 0; i < abs(wheelEvent.y); ++i) {
					append_kbm_event(numKbmEventsWritten++, keyCode, interop_bool_true);
					append_kbm_event(numKbmEventsWritten++, keyCode, interop_bool_false);
				}
				break;
			}

			case SDL_EventType::SDL_CONTROLLERDEVICEADDED: {
				auto deviceEvent = event.cdevice;
				auto handle = SDL_GameControllerOpen(deviceEvent.which);
				ThrowIfNull(handle, "Could not connect to new game controller: ", SDL_GetError());
				push_new_controller(handle);
				break;
			}

			case SDL_EventType::SDL_QUIT: {
				quitRequested = interop_bool_true;
				break;
			}
		}
	}

	*outNumKbmEventsWritten = numKbmEventsWritten;
	*outNumControllerEventsWritten = numControllerEventsWritten;
	*outNumClickEventsWritten = numClickEventsWritten;
	*outMousePosX = mousePosX;
	*outMousePosY = mousePosY;
	*outQuitRequested = quitRequested;
}
StartExportedFunc(iterate_events, int32_t* outNumKbmEventsWritten, int32_t* outNumControllerEventsWritten, int32_t* outNumClickEventsWritten, int32_t* outMousePosX, int32_t* outMousePosY, interop_bool* outQuitRequested) {
	native_impl_loop::iterate_events(
		outNumKbmEventsWritten,
		outNumControllerEventsWritten,
		outNumClickEventsWritten,
		outMousePosX,
		outMousePosY,
		outQuitRequested
	);
	EndExportedFunc
}

void native_impl_loop::detect_controllers() {
	auto numJoysticks = SDL_NumJoysticks();
	ThrowIfNegative(numJoysticks, "Could not obtain connected game controllers: ", SDL_GetError());
	for (auto i = 0; i < numJoysticks; ++i) {
		if (!SDL_IsGameController(i)) continue;
		auto handle = SDL_GameControllerOpen(i);
		ThrowIfNull(handle, "Could not connect to game controller: ", SDL_GetError());
		push_new_controller(handle);
	}
}
StartExportedFunc(detect_controllers) {
	EndExportedFunc
}

