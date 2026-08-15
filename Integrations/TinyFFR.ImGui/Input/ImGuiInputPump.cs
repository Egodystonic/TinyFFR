// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Hexa.NET.ImGui;

namespace Egodystonic.TinyFFR.DearImGui.Input;

sealed class ImGuiInputPump {
	const float AnalogPressThreshold = 0.1f;

	readonly bool _gamepadEnabled;
	readonly float _stickDeadzone;
	readonly float _triggerDeadzone;

	MouseCursorStyle _lastAppliedCursorStyle = MouseCursorStyle.Arrow;

	public ImGuiInputPump(bool gamepadEnabled, float stickDeadzone, float triggerDeadzone) {
		_gamepadEnabled = gamepadEnabled;
		_stickDeadzone = stickDeadzone;
		_triggerDeadzone = triggerDeadzone;
	}

	public void Pump(ImGuiIOPtr io, ILatestInputRetriever input, Window? window, XYPair<float> dpiScale, XYPair<int> subAreaOffset) {
		var kbm = input.KeyboardAndMouse;

		var cursorPos = kbm.MouseCursorPosition;
		io.AddMousePosEvent(cursorPos.X * dpiScale.X - subAreaOffset.X, cursorPos.Y * dpiScale.Y - subAreaOffset.Y);

		foreach (var keyEvent in kbm.NewKeyEvents) {
			var mouseButton = ImGuiKeyMap.TranslateMouseButton(keyEvent.Key);
			if (mouseButton >= 0) {
				io.AddMouseButtonEvent(mouseButton, keyEvent.KeyDown);
				continue;
			}

			var imGuiKey = ImGuiKeyMap.Translate(keyEvent.Key);
			if (imGuiKey == ImGuiKey.None) continue;
			io.AddKeyEvent(imGuiKey, keyEvent.KeyDown);
		}

		var scrollDelta = kbm.MouseScrollWheelDelta;
		if (scrollDelta != 0) io.AddMouseWheelEvent(0f, -scrollDelta);

		io.AddKeyEvent(ImGuiKey.ModCtrl, kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftControl) || kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightControl));
		io.AddKeyEvent(ImGuiKey.ModShift, kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift) || kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightShift));
		io.AddKeyEvent(ImGuiKey.ModAlt, kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftAlt) || kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightAlt));
		io.AddKeyEvent(ImGuiKey.ModSuper, kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftWinKey) || kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightWinKey));

		var transcribedText = kbm.TranscribedText;
		for (var i = 0; i < transcribedText.Length; ++i) {
			io.AddInputCharacter(transcribedText[i]);
		}

		if (_gamepadEnabled) PumpGameControllers(io, input);

		if (window is { } windowActual) ApplyCursor(io, windowActual);
	}

	void PumpGameControllers(ImGuiIOPtr io, ILatestInputRetriever input) {
		if (input.GameControllers.Count == 0) return;
		io.BackendFlags |= ImGuiBackendFlags.HasGamepad;

		var controller = input.GameControllersCombined;

		var mappedButtons = ImGuiKeyMap.MappedGamepadButtons;
		for (var i = 0; i < mappedButtons.Length; ++i) {
			var button = mappedButtons[i];
			io.AddKeyEvent(ImGuiKeyMap.TranslateGamepadButton(button), controller.ButtonIsCurrentlyDown(button));
		}

		AddStick(io, controller.LeftStickPosition, ImGuiKey.GamepadLStickLeft, ImGuiKey.GamepadLStickRight, ImGuiKey.GamepadLStickUp, ImGuiKey.GamepadLStickDown);
		AddStick(io, controller.RightStickPosition, ImGuiKey.GamepadRStickLeft, ImGuiKey.GamepadRStickRight, ImGuiKey.GamepadRStickUp, ImGuiKey.GamepadRStickDown);

		AddAnalog(io, ImGuiKey.GamepadL2, controller.LeftTriggerPosition.GetDisplacementWithDeadzone(_triggerDeadzone));
		AddAnalog(io, ImGuiKey.GamepadR2, controller.RightTriggerPosition.GetDisplacementWithDeadzone(_triggerDeadzone));
	}

	void AddStick(ImGuiIOPtr io, GameControllerStickPosition stick, ImGuiKey leftKey, ImGuiKey rightKey, ImGuiKey upKey, ImGuiKey downKey) {
		ImGuiKeyMap.DecomposeStick(stick, _stickDeadzone, out var left, out var right, out var up, out var down);
		AddAnalog(io, leftKey, left);
		AddAnalog(io, rightKey, right);
		AddAnalog(io, upKey, up);
		AddAnalog(io, downKey, down);
	}

	static void AddAnalog(ImGuiIOPtr io, ImGuiKey key, float value) => io.AddKeyAnalogEvent(key, value > AnalogPressThreshold, value);

	void ApplyCursor(ImGuiIOPtr io, Window window) {
		var style = ImGuiKeyMap.TranslateCursor(ImGui.GetMouseCursor());
		if (style == _lastAppliedCursorStyle) return;
		window.CursorStyle = style;
		_lastAppliedCursorStyle = style;
	}
}
