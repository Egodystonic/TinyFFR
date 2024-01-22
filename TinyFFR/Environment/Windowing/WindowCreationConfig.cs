// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly record struct WindowCreationConfig {
	public XYPair? Position { get; init; } = null;
	
	readonly XYPair? _size = null;
	public XYPair? Size {
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

	public WindowCreationConfig() { }
}