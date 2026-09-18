// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input.Local;

namespace Egodystonic.TinyFFR.Environment.Input;

/* Made to map directly to SDL's SDL_GameControllerButton, but each value is +1 so we can define "unknown" as 0.
 */
/// <summary>
/// Enumeration of supported gamepad controller buttons.
/// </summary>
/// <remarks>
/// Buttons are identified by their physical position on a standard gamepad layout, not by the label printed on any particular controller. The four
/// face buttons (<see cref="A"/>, <see cref="B"/>, <see cref="X"/>, <see cref="Y"/>) use the Xbox-style names and are identified by their position in the
/// typical face-button quad layout..
/// </remarks>
public enum GameControllerButton : int {
	/// <summary>
	/// Unknown or unrecognised button.
	/// </summary>
	Unknown = 0,
	/// <summary>
	/// The bottom button of the four face buttons (labelled <c>A</c> on Xbox-style controllers, <c>✕</c> on PlayStation-style ones, and <c>B</c> on Nintendo-style ones).
	/// </summary>
	/// <remarks>
	/// The four face buttons are identified by their physical position, not by the label printed on any particular controller; see the remarks on <see cref="GameControllerButton"/> for details.
	/// </remarks>
	A = RawLocalGameControllerEventType.A + 1,
	/// <summary>
	/// The right-hand button of the four face buttons (labelled <c>B</c> on Xbox-style controllers, <c>○</c> on PlayStation-style ones, and <c>A</c> on Nintendo-style ones).
	/// </summary>
	/// <remarks>
	/// The four face buttons are identified by their physical position, not by the label printed on any particular controller; see the remarks on <see cref="GameControllerButton"/> for details.
	/// </remarks>
	B = RawLocalGameControllerEventType.B + 1,
	/// <summary>
	/// The left-hand button of the four face buttons (labelled <c>X</c> on Xbox-style controllers, <c>□</c> on PlayStation-style ones, and <c>Y</c> on Nintendo-style ones).
	/// </summary>
	/// <remarks>
	/// The four face buttons are identified by their physical position, not by the label printed on any particular controller; see the remarks on <see cref="GameControllerButton"/> for details.
	/// </remarks>
	X = RawLocalGameControllerEventType.X + 1,
	/// <summary>
	/// The top button of the four face buttons (labelled <c>Y</c> on Xbox-style controllers, <c>△</c> on PlayStation-style ones, and <c>X</c> on Nintendo-style ones).
	/// </summary>
	/// <remarks>
	/// The four face buttons are identified by their physical position, not by the label printed on any particular controller; see the remarks on <see cref="GameControllerButton"/> for details.
	/// </remarks>
	Y = RawLocalGameControllerEventType.Y + 1,
	/// <summary>
	/// The secondary centre button, conventionally used to go back or open a secondary menu (labelled "View", "Select", "Share", or "Back" depending on the controller).
	/// </summary>
	SelectOrView = RawLocalGameControllerEventType.SelectOrView + 1,
	/// <summary>
	/// The central logo/guide button (the Xbox button, PlayStation button, or Home button depending on the controller).
	/// </summary>
	/// <remarks>
	/// Many operating systems intercept this button for their own purposes, so it may not reliably reach your application.
	/// </remarks>
	Logo = RawLocalGameControllerEventType.Logo + 1,
	/// <summary>
	/// The primary centre button, conventionally used to start the game or open the main menu (labelled "Menu", "Start", or "Options" depending on the controller).
	/// </summary>
	StartOrMenu = RawLocalGameControllerEventType.StartOrMenu + 1,
	/// <summary>
	/// Pressing the left analog stick inwards, like a button (sometimes known as "L3").
	/// </summary>
	LeftStick = RawLocalGameControllerEventType.LeftStick + 1,
	/// <summary>
	/// Pressing the right analog stick inwards, like a button (sometimes known as "R3").
	/// </summary>
	RightStick = RawLocalGameControllerEventType.RightStick + 1,
	/// <summary>
	/// The upper-left shoulder button (sometimes known as "LB" or "L1").
	/// </summary>
	LeftBumper = RawLocalGameControllerEventType.LeftBumper + 1,
	/// <summary>
	/// The upper-right shoulder button (sometimes known as "RB" or "R1").
	/// </summary>
	RightBumper = RawLocalGameControllerEventType.RightBumper + 1,
	/// <summary>
	/// The upward direction on the directional pad (D-pad).
	/// </summary>
	DirectionalPadUp = RawLocalGameControllerEventType.DirectionalPadUp + 1,
	/// <summary>
	/// The downward direction on the directional pad (D-pad).
	/// </summary>
	DirectionalPadDown = RawLocalGameControllerEventType.DirectionalPadDown + 1,
	/// <summary>
	/// The leftward direction on the directional pad (D-pad).
	/// </summary>
	DirectionalPadLeft = RawLocalGameControllerEventType.DirectionalPadLeft + 1,
	/// <summary>
	/// The rightward direction on the directional pad (D-pad).
	/// </summary>
	DirectionalPadRight = RawLocalGameControllerEventType.DirectionalPadRight + 1,
	/// <summary>
	/// An additional button whose purpose varies by controller (for example a share, capture, or microphone button). Only present on some controllers.
	/// </summary>
	Misc = RawLocalGameControllerEventType.Misc + 1,
	/// <summary>
	/// The first of up to four extra paddles on the back of the controller, numbered according to the controller's own labelling. Only present on some controllers.
	/// </summary>
	Paddle1 = RawLocalGameControllerEventType.Paddle1 + 1,
	/// <summary>
	/// The second of up to four extra paddles on the back of the controller, numbered according to the controller's own labelling. Only present on some controllers.
	/// </summary>
	Paddle2 = RawLocalGameControllerEventType.Paddle2 + 1,
	/// <summary>
	/// The third of up to four extra paddles on the back of the controller, numbered according to the controller's own labelling. Only present on some controllers.
	/// </summary>
	Paddle3 = RawLocalGameControllerEventType.Paddle3 + 1,
	/// <summary>
	/// The fourth of up to four extra paddles on the back of the controller, numbered according to the controller's own labelling. Only present on some controllers.
	/// </summary>
	Paddle4 = RawLocalGameControllerEventType.Paddle4 + 1,
	/// <summary>
	/// Pressing the controller's touchpad inwards, like a button. Only present on some controllers.
	/// </summary>
	TouchPad = RawLocalGameControllerEventType.TouchPad + 1,

	// ========= This is the end of SDL's SDL_GameControllerButton; everything below this line is just TinyFFR =========

	/// <summary>
	/// The left trigger, treated as a simple on/off button.
	/// </summary>
	/// <remarks>
	/// The triggers are analog on most controllers: use <see cref="ILatestGameControllerInputRetriever.LeftTriggerPosition"/> instead if you want to know <i>how far</i> the trigger
	/// is being pulled rather than merely whether it is pulled.
	/// </remarks>
	LeftTrigger = RawLocalGameControllerEventType.LeftTrigger + 1,
	/// <summary>
	/// The right trigger, treated as a simple on/off button.
	/// </summary>
	/// <remarks>
	/// The triggers are analog on most controllers: use <see cref="ILatestGameControllerInputRetriever.RightTriggerPosition"/> instead if you want to know <i>how far</i> the trigger
	/// is being pulled rather than merely whether it is pulled.
	/// </remarks>
	RightTrigger = RawLocalGameControllerEventType.RightTrigger + 1
}