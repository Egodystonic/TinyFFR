// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

/* Made to map directly to SDL's SDL_GameControllerButton, but each value is +1 so we can define "unknown" as 0.
 */
public enum GameControllerButton : int {
	Unknown = 0,
	A = 0 + 1,
	B = 1 + 1,
	X = 2 + 1,
	Y = 3 + 1,
	Back = 4 + 1,
	Guide = 5 + 1,
	Start = 6 + 1,
	LeftStick = 7 + 1,
	RightStick = 8 + 1,
	LeftBumper = 9 + 1,
	RightBumper = 10 + 1,
	DirectionalPadUp = 11 + 1,
	DirectionalPadDown = 12 + 1,
	DirectionalPadLeft = 13 + 1,
	DirectionalPadRight = 14 + 1,
	Misc = 15 + 1,
	Paddle1 = 16 + 1,
	Paddle2 = 17 + 1,
	Paddle3 = 18 + 1,
	Paddle4 = 19 + 1,
	TouchPad = 20 + 1,

	// ========= This is the end of SDL's SDL_GameControllerButton; everything below this line is just TinyFFR =========

	LeftTrigger = 100,
	RightTrigger = 101
}