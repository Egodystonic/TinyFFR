// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.DearImGui;

public readonly ref struct ImGuiSceneCreationConfig {
	public bool EnableDocking { get; init; } = true;
	public bool EnableKeyboardNavigation { get; init; } = false;
	public bool EnableGamepadNavigation { get; init; } = false;
	public float GamepadStickDeadzone { get; init; } = GameControllerStickPosition.RecommendedDeadzoneSize;
	public float GamepadTriggerDeadzone { get; init; } = GameControllerTriggerPosition.RecommendedDeadzoneSize;

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
