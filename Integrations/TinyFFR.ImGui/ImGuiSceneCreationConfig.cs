// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.DearImGui;

/// <summary>
/// Controls how an <see cref="ImGuiScene"/> is created: which ImGui features are enabled, and how controller input is filtered.
/// </summary>
public readonly ref struct ImGuiSceneCreationConfig : IConfigStruct<ImGuiSceneCreationConfig> {
	/// <summary>
	/// The default value for <see cref="EnableDocking"/>: <see langword="true"/>.
	/// </summary>
	public const bool DefaultEnableDocking = true;
	/// <summary>
	/// The default value for <see cref="EnableKeyboardNavigation"/>: <see langword="false"/>.
	/// </summary>
	public const bool DefaultEnableKeyboardNavigation = false;
	/// <summary>
	/// The default value for <see cref="EnableGamepadNavigation"/>: <see langword="false"/>.
	/// </summary>
	public const bool DefaultEnableGamepadNavigation = false;
	/// <summary>
	/// The default value for <see cref="AutoManageTextInputTranscription"/>: <see langword="true"/>.
	/// </summary>
	public const bool DefaultAutoManageTextInputTranscription = true;

	/// <summary>
	/// Whether ImGui windows may be docked to one another and to the edges of the drawing area. Defaults to <see langword="true"/>.
	/// </summary>
	public bool EnableDocking { get; init; } = DefaultEnableDocking;
	/// <summary>
	/// Whether the interface can be navigated with the keyboard. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Enabling this lets the arrow keys and tab move between widgets, but those keys then belong to the interface rather than to
	/// your scene, which is why it is off by default.
	/// </remarks>
	public bool EnableKeyboardNavigation { get; init; } = DefaultEnableKeyboardNavigation;
	/// <summary>
	/// Whether the interface can be navigated with a game controller. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// As with keyboard navigation, enabling this gives the controller's sticks and face buttons to the interface rather than to
	/// your scene.
	/// </remarks>
	public bool EnableGamepadNavigation { get; init; } = DefaultEnableGamepadNavigation;
	/// <summary>
	/// Whether the interface keeps the application loop's text transcription setting in step with ImGui's own text input needs. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ImGui text fields need the characters a user's keystrokes actually produce rather than raw key codes, which is what
	/// <see cref="ApplicationLoop.EnableInputTextTranscription"/> supplies. Whilst this is enabled, each
	/// <c>BeginFrame</c> switches that setting on whenever ImGui reports that it wants text input (i.e. whilst a text field is
	/// focused) and off again afterwards, so text fields simply work and transcription is not left on when nothing is being typed
	/// in to.
	/// </para>
	/// <para>
	/// Turn this off if your application drives its own text entry from
	/// <see cref="ILatestKeyboardAndMouseInputRetriever.TranscribedText"/> and therefore needs to own that setting itself. The
	/// interface will then never write to it, and you must enable transcription yourself or ImGui's text fields will accept
	/// nothing.
	/// </para>
	/// </remarks>
	public bool AutoManageTextInputTranscription { get; init; } = DefaultAutoManageTextInputTranscription;
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

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in ImGuiSceneCreationConfig src) {
		return	SerializationSizeOfBool()	// EnableDocking
			+	SerializationSizeOfBool()	// EnableKeyboardNavigation
			+	SerializationSizeOfBool()	// EnableGamepadNavigation
			+	SerializationSizeOfBool()	// AutoManageTextInputTranscription
			+	SerializationSizeOfFloat()	// GamepadStickDeadzone
			+	SerializationSizeOfFloat();	// GamepadTriggerDeadzone
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ImGuiSceneCreationConfig src) {
		SerializationWriteBool(ref dest, src.EnableDocking);
		SerializationWriteBool(ref dest, src.EnableKeyboardNavigation);
		SerializationWriteBool(ref dest, src.EnableGamepadNavigation);
		SerializationWriteBool(ref dest, src.AutoManageTextInputTranscription);
		SerializationWriteFloat(ref dest, src.GamepadStickDeadzone);
		SerializationWriteFloat(ref dest, src.GamepadTriggerDeadzone);
	}
	/// <inheritdoc />
	public static ImGuiSceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var enableDocking = SerializationReadBool(ref src);
		var enableKeyboardNavigation = SerializationReadBool(ref src);
		var enableGamepadNavigation = SerializationReadBool(ref src);
		var autoManageTextInputTranscription = SerializationReadBool(ref src);
		var gamepadStickDeadzone = SerializationReadFloat(ref src);
		var gamepadTriggerDeadzone = SerializationReadFloat(ref src);
		return new() {
			EnableDocking = enableDocking,
			EnableKeyboardNavigation = enableKeyboardNavigation,
			EnableGamepadNavigation = enableGamepadNavigation,
			AutoManageTextInputTranscription = autoManageTextInputTranscription,
			GamepadStickDeadzone = gamepadStickDeadzone,
			GamepadTriggerDeadzone = gamepadTriggerDeadzone
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
