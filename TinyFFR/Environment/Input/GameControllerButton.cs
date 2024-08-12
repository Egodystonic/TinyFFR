// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input.Local;

namespace Egodystonic.TinyFFR.Environment.Input;

/* Made to map directly to SDL's SDL_GameControllerButton, but each value is +1 so we can define "unknown" as 0.
 */
public enum GameControllerButton : int {
	Unknown = 0,
	A = RawLocalGameControllerEventType.A + 1,
	B = RawLocalGameControllerEventType.B + 1,
	X = RawLocalGameControllerEventType.X + 1,
	Y = RawLocalGameControllerEventType.Y + 1,
	SelectOrView = RawLocalGameControllerEventType.SelectOrView + 1,
	Logo = RawLocalGameControllerEventType.Logo + 1,
	StartOrMenu = RawLocalGameControllerEventType.StartOrMenu + 1,
	LeftStick = RawLocalGameControllerEventType.LeftStick + 1,
	RightStick = RawLocalGameControllerEventType.RightStick + 1,
	LeftBumper = RawLocalGameControllerEventType.LeftBumper + 1,
	RightBumper = RawLocalGameControllerEventType.RightBumper + 1,
	DirectionalPadUp = RawLocalGameControllerEventType.DirectionalPadUp + 1,
	DirectionalPadDown = RawLocalGameControllerEventType.DirectionalPadDown + 1,
	DirectionalPadLeft = RawLocalGameControllerEventType.DirectionalPadLeft + 1,
	DirectionalPadRight = RawLocalGameControllerEventType.DirectionalPadRight + 1,
	Misc = RawLocalGameControllerEventType.Misc + 1,
	Paddle1 = RawLocalGameControllerEventType.Paddle1 + 1,
	Paddle2 = RawLocalGameControllerEventType.Paddle2 + 1,
	Paddle3 = RawLocalGameControllerEventType.Paddle3 + 1,
	Paddle4 = RawLocalGameControllerEventType.Paddle4 + 1,
	TouchPad = RawLocalGameControllerEventType.TouchPad + 1,

	// ========= This is the end of SDL's SDL_GameControllerButton; everything below this line is just TinyFFR =========

	LeftTrigger = RawLocalGameControllerEventType.LeftTrigger + 1,
	RightTrigger = RawLocalGameControllerEventType.RightTrigger + 1
}