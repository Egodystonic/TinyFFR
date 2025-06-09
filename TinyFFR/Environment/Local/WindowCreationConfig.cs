// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly ref struct WindowCreationConfig {
	public required Display Display { get; init; }

	public ReadOnlySpan<char> Title { get; init; }

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

	public WindowFullscreenStyle FullscreenStyle { get; init; } = WindowFullscreenStyle.NotFullscreen;

	public WindowCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}