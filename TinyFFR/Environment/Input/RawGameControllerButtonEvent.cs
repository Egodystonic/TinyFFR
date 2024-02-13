// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
readonly struct RawGameControllerButtonEvent {
	[FieldOffset(0)]
	public readonly GameControllerHandle Handle;
	[FieldOffset(8)]
	public readonly RawGameControllerEventType Type;
	[FieldOffset(14)]
	public readonly short NewValue;

	public override string ToString() {
		return $"{Type} = {NewValue}";
	}
}

enum RawGameControllerEventType {
	A = 0,
	B = 1,
	X = 2,
	Y = 3,
	Back = 4,
	Guide = 5,
	Start = 6,
	LeftStick = 7,
	RightStick = 8,
	LeftBumper = 9,
	RightBumper = 10,
	DirectionalPadUp = 11,
	DirectionalPadDown = 12,
	DirectionalPadLeft = 13,
	DirectionalPadRight = 14,
	Misc = 15,
	Paddle1 = 16,
	Paddle2 = 17,
	Paddle3 = 18,
	Paddle4 = 19,
	TouchPad = 20,

	// ========= This is the end of SDL's SDL_GameControllerButton; everything below this line is just TinyFFR =========
	// Note: These values are linked with a constant in native_impl_loop::iterate_events

	LeftStickAxisX = 200, 
	LeftStickAxisY = 201,
	RightStickAxisX = 202,
	RightStickAxisY = 203,
	LeftTrigger = 204,
	RightTrigger = 205,
}