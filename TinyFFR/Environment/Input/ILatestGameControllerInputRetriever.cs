// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection.Metadata;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Retrieves the latest state of a single game controller, or of every connected game controller combined (see <see cref="ILatestInputRetriever.GameControllersCombined"/>).
/// </summary>
/// <remarks>
/// <para>
/// As with the keyboard and mouse, this offers two views of the same input: the <c>New...</c> properties and the <c>...ThisIteration</c> methods describe what
/// changed during the most recent iteration of the <see cref="ApplicationLoop"/>, whereas <see cref="CurrentlyPressedButtons"/> and
/// <see cref="ButtonIsCurrentlyDown"/> describe what is held down right now.
/// </para>
/// <para>
/// The sticks and triggers are analog inputs, so they are exposed as their own types rather than as buttons; each of those also offers a digital
/// (pressed/not pressed) reading for cases where you only care whether the user is pushing it.
/// </para>
/// </remarks>
public interface ILatestGameControllerInputRetriever : IStringSpanNameEnabled {
	/// <summary>
	/// The current position of the left analog stick.
	/// </summary>
	public GameControllerStickPosition LeftStickPosition { get; }
	/// <summary>
	/// The current position of the right analog stick.
	/// </summary>
	public GameControllerStickPosition RightStickPosition { get; }
	/// <summary>
	/// How far the left trigger is currently depressed.
	/// </summary>
	public GameControllerTriggerPosition LeftTriggerPosition { get; }
	/// <summary>
	/// How far the right trigger is currently depressed.
	/// </summary>
	public GameControllerTriggerPosition RightTriggerPosition { get; }

	/// <summary>
	/// Every button press and release that occurred in the most recent iteration, in the order they occurred.
	/// </summary>
	/// <remarks>
	/// Where you only care about presses or only about releases, <see cref="NewButtonDownEvents"/> and <see cref="NewButtonUpEvents"/> say the same thing more directly.
	/// </remarks>
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButtonEvent> NewButtonEvents { get; }
	/// <summary>
	/// Every button that was pressed down in the most recent iteration, in the order they were pressed.
	/// </summary>
	/// <remarks>
	/// A button appears here only on the iteration in which it was first pressed, not on every iteration it remains held for; see <see cref="CurrentlyPressedButtons"/> for the latter.
	/// </remarks>
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> NewButtonDownEvents { get; }
	/// <summary>
	/// Every button that was released in the most recent iteration, in the order they were released.
	/// </summary>
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> NewButtonUpEvents { get; }
	/// <summary>
	/// Every button that is currently held down, including buttons first pressed on an earlier iteration.
	/// </summary>
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> CurrentlyPressedButtons { get; }

	/// <summary>
	/// Returns whether <paramref name="button"/> is currently held down, including if it was first pressed on an earlier iteration.
	/// </summary>
	/// <param name="button">The button to test.</param>
	public bool ButtonIsCurrentlyDown(GameControllerButton button);
	/// <summary>
	/// Returns whether <paramref name="button"/> was pressed down during the most recent iteration.
	/// </summary>
	/// <remarks>
	/// This is <see langword="true"/> only on the iteration in which the button was first pressed, and not on subsequent iterations it remains held for; use
	/// <see cref="ButtonIsCurrentlyDown"/> for the latter.
	/// </remarks>
	/// <param name="button">The button to test.</param>
	public bool ButtonWasPressedThisIteration(GameControllerButton button);
	/// <summary>
	/// Returns whether <paramref name="button"/> was released during the most recent iteration.
	/// </summary>
	/// <param name="button">The button to test.</param>
	public bool ButtonWasReleasedThisIteration(GameControllerButton button);
}