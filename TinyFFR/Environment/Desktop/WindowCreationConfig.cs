// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly record struct WindowCreationConfig {
	public required Display Display { get; init; }

	public XYPair Position { get; init; } = (0, 0); // TODO explain in XMLDoc that this is relative positioning on the selected Display

	readonly XYPair _size = (800, 600);
	public XYPair Size {
		get => _size;
		init {
			if (value is { X: < 0f } or { Y: < 0f }) {
				throw new ArgumentOutOfRangeException(nameof(Size), value, $"{nameof(XYPair.X)} and {nameof(XYPair.Y)} components must not be negative.");
			}
			_size = value;
		}
	}

	readonly string _title = "Tiny FFR Application";
	public string Title {
		get => _title;
		init {
			ArgumentNullException.ThrowIfNull(value, nameof(Title));
			_title = value;
		}
	}

	public WindowFullscreenStyle FullscreenStyle { get; init; } = WindowFullscreenStyle.NotFullscreen;

	public WindowCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (Title == null) throw InvalidObjectException.InvalidDefault<WindowCreationConfig>();
		Display.ThrowIfInvalid();
	}
}