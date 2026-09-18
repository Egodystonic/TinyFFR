// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.DearImGui;

/// <summary>
/// Controls how an <see cref="ImGuiScene"/> is created: which ImGui features are enabled, and how controller input is filtered.
/// </summary>
public readonly ref struct ImGuiSceneCreationConfig {
	/// <summary>
	/// Whether ImGui windows may be docked to one another and to the edges of the drawing area. Defaults to <see langword="true"/>.
	/// </summary>
	public bool EnableDocking { get; init; } = true;
	/// <summary>
	/// Whether the interface can be navigated with the keyboard. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Enabling this lets the arrow keys and tab move between widgets, but those keys then belong to the interface rather than to
	/// your scene, which is why it is off by default.
	/// </remarks>
	public bool EnableKeyboardNavigation { get; init; } = false;
	/// <summary>
	/// Whether the interface can be navigated with a game controller. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// As with keyboard navigation, enabling this gives the controller's sticks and face buttons to the interface rather than to
	/// your scene.
	/// </remarks>
	public bool EnableGamepadNavigation { get; init; } = false;
	/// <summary>
	/// How far a controller stick must be pushed before the interface reacts, in the range <c>0f &lt;= n &lt; 1f</c>. Defaults to the recommended deadzone for controller sticks.
	/// </summary>
	/// <remarks>
	/// Sticks rarely rest at exactly centre, so readings below this threshold are discarded to stop an untouched stick drifting
	/// through the interface. Only relevant when <see cref="EnableGamepadNavigation"/> is <see langword="true"/>.
	/// </remarks>
	public float GamepadStickDeadzone { get; init; } = GameControllerStickPosition.RecommendedDeadzoneSize;
	/// <summary>
	/// How far a controller trigger must be pressed before the interface reacts, in the range <c>0f &lt;= n &lt; 1f</c>. Defaults to the recommended deadzone for controller triggers.
	/// </summary>
	/// <remarks>
	/// Only relevant when <see cref="EnableGamepadNavigation"/> is <see langword="true"/>.
	/// </remarks>
	public float GamepadTriggerDeadzone { get; init; } = GameControllerTriggerPosition.RecommendedDeadzoneSize;

	/// <summary>
	/// Constructs a new <see cref="ImGuiSceneCreationConfig"/> with default values for every property.
	/// </summary>
	public ImGuiSceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (!Single.IsFinite(GamepadStickDeadzone) || GamepadStickDeadzone is < 0f or >= 1f) {
			throw new ArgumentOutOfRangeException(nameof(GamepadStickDeadzone), GamepadStickDeadzone, $"{nameof(GamepadStickDeadzone)} must be at least 0 and less than 1.");
		}
		if (!Single.IsFinite(GamepadTriggerDeadzone) || GamepadTriggerDeadzone is < 0f or >= 1f) {
			throw new ArgumentOutOfRangeException(nameof(GamepadTriggerDeadzone), GamepadTriggerDeadzone, $"{nameof(GamepadTriggerDeadzone)} must be at least 0 and less than 1.");
		}
	}
}
