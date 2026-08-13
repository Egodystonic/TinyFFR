// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Hexa.NET.ImGui;

namespace Egodystonic.TinyFFR.DearImGui.Input;

sealed class ImGuiInputPump {
	MouseCursorStyle _lastAppliedCursorStyle = MouseCursorStyle.Arrow;
	bool _lastAppliedCursorVisibility = true;

	public void Pump(ImGuiIOPtr io, ILatestInputRetriever input, Window? window, XYPair<float> dpiScale) {
		var kbm = input.KeyboardAndMouse;

		var cursorPos = kbm.MouseCursorPosition;
		io.AddMousePosEvent(cursorPos.X * dpiScale.X, cursorPos.Y * dpiScale.Y);

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

		if (window is { } windowActual) ApplyCursor(io, windowActual);
	}

	void ApplyCursor(ImGuiIOPtr io, Window window) {
		var desiredCursor = ImGui.GetMouseCursor();
		if (desiredCursor == ImGuiMouseCursor.None) {
			if (_lastAppliedCursorVisibility) {
				window.CursorIsVisible = false;
				_lastAppliedCursorVisibility = false;
			}
			return;
		}

		if (!_lastAppliedCursorVisibility) {
			window.CursorIsVisible = true;
			_lastAppliedCursorVisibility = true;
		}

		var style = ImGuiKeyMap.TranslateCursor(desiredCursor);
		if (style == _lastAppliedCursorStyle) return;
		window.CursorStyle = style;
		_lastAppliedCursorStyle = style;
	}
}
