// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly record struct WindowConfig {
	public required Display Display { get; init; }

	public XYPair<int> Position { get; init; } = (0, 0); // TODO explain in XMLDoc that this is relative positioning on the selected Display

	readonly XYPair<int> _size = (800, 600);
	public XYPair<int> Size {
		get => _size;
		init {
			if (value is { X: < 0 } or { Y: < 0 }) {
				throw new ArgumentOutOfRangeException(nameof(Size), value, $"{nameof(XYPair<int>.X)} and {nameof(XYPair<int>.Y)} components must not be negative.");
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

	public WindowConfig() { }

	internal void ThrowIfInvalid() {
		if (Title == null) throw InvalidObjectException.InvalidDefault<WindowConfig>();
	}
}