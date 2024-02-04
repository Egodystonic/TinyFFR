// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

/* Made to map directly to SDL's SDL_GameControllerButton, but each value is +1 so we can define "unknown" as 0.
 */
public enum GameControllerButton : int {
	Unknown = 0,
	A = RawGameControllerEventType.A + 1,
	B = RawGameControllerEventType.B + 1,
	X = RawGameControllerEventType.X + 1,
	Y = RawGameControllerEventType.Y + 1,
	Back = RawGameControllerEventType.Back + 1,
	Guide = RawGameControllerEventType.Guide + 1,
	Start = RawGameControllerEventType.Start + 1,
	LeftStick = RawGameControllerEventType.LeftStick + 1,
	RightStick = RawGameControllerEventType.RightStick + 1,
	LeftBumper = RawGameControllerEventType.LeftBumper + 1,
	RightBumper = RawGameControllerEventType.RightBumper + 1,
	DirectionalPadUp = RawGameControllerEventType.DirectionalPadUp + 1,
	DirectionalPadDown = RawGameControllerEventType.DirectionalPadDown + 1,
	DirectionalPadLeft = RawGameControllerEventType.DirectionalPadLeft + 1,
	DirectionalPadRight = RawGameControllerEventType.DirectionalPadRight + 1,
	Misc = RawGameControllerEventType.Misc + 1,
	Paddle1 = RawGameControllerEventType.Paddle1 + 1,
	Paddle2 = RawGameControllerEventType.Paddle2 + 1,
	Paddle3 = RawGameControllerEventType.Paddle3 + 1,
	Paddle4 = RawGameControllerEventType.Paddle4 + 1,
	TouchPad = RawGameControllerEventType.TouchPad + 1,

	// ========= This is the end of SDL's SDL_GameControllerButton; everything below this line is just TinyFFR =========

	LeftTrigger = RawGameControllerEventType.LeftTrigger + 1,
	RightTrigger = RawGameControllerEventType.RightTrigger + 1
}