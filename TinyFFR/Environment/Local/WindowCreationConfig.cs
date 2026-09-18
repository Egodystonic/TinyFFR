// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Configuration for creating a new <see cref="Window"/>.
/// </summary>
public readonly ref struct WindowCreationConfig : IConfigStruct<WindowCreationConfig> {
	/// <summary>
	/// The display the new window should be shown on.
	/// </summary>
	public required Display Display { get; init; }

	/// <summary>
	/// The title for the new window; i.e. the text the operating system shows in its title bar.
	/// </summary>
	public ReadOnlySpan<char> Title { get; init; }

	/// <summary>
	/// Where the new window's top-left corner should sit, measured in pixels from the top-left corner of <see cref="Display"/>, with <c>Y</c> increasing downward. Defaults to <c>(0, 0)</c>.
	/// </summary>
	/// <remarks>
	/// This position is relative to the selected display, not to the desktop as a whole: on a multi-monitor setup, <c>(0, 0)</c> is the top-left corner of
	/// <see cref="Display"/> regardless of how the displays are arranged relative to one another. A window created fullscreen covers its whole display, so this only
	/// determines where it sits once it is not fullscreen.
	/// </remarks>
	public XYPair<int> Position { get; init; } = (0, 0);

	readonly XYPair<int> _size = (800, 600);
	/// <summary>
	/// The size of the new window (<c>X</c> = width, <c>Y</c> = height). Neither component may be negative. Defaults to <c>(800, 600)</c>.
	/// </summary>
	/// <remarks>
	/// This is a size in the operating system's window coordinates, which on displays with scaling enabled is not the same as the number of pixels that will
	/// actually be rendered. A window created fullscreen covers its whole display, so this only determines how large it is once it is not fullscreen.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value with a negative <see cref="XYPair{T}.X"/> or <see cref="XYPair{T}.Y"/>.</exception>
	public XYPair<int> Size {
		get => _size;
		init {
			if (value is { X: < 0 } or { Y: < 0 }) {
				throw new ArgumentOutOfRangeException(nameof(Size), value, $"{nameof(XYPair<int>.X)} and {nameof(XYPair<int>.Y)} components must not be negative.");
			}
			_size = value;
		}
	}

	/// <summary>
	/// Whether and how the new window should occupy its entire <see cref="Display"/>. Defaults to <see cref="WindowFullscreenStyle.NotFullscreen"/>.
	/// </summary>
	public WindowFullscreenStyle FullscreenStyle { get; init; } = WindowFullscreenStyle.NotFullscreen;

	/// <summary>
	/// Constructs a new <see cref="WindowCreationConfig"/> with default values for every setting other than <see cref="Display"/>, which must be specified.
	/// </summary>
	public WindowCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in WindowCreationConfig src) {
		return	SerializationSizeOfResource() // Display
			+	SerializationSizeOfString(src.Title) // Title
			+	SerializationSizeOf<XYPair<int>>() // Position
			+	SerializationSizeOf<XYPair<int>>() // Size
			+	SerializationSizeOfInt(); // FullscreenStyle
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in WindowCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.Display);
		SerializationWriteString(ref dest, src.Title);
		SerializationWrite(ref dest, src.Position);
		SerializationWrite(ref dest, src.Size);
		SerializationWriteInt(ref dest, (int) src.FullscreenStyle);
	}
	/// <inheritdoc/>
	public static WindowCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new WindowCreationConfig {
			Display = SerializationReadResource<Display>(ref src),
			Title = SerializationReadString(ref src),
			Position = SerializationRead<XYPair<int>>(ref src),
			Size = SerializationRead<XYPair<int>>(ref src),
			FullscreenStyle = (WindowFullscreenStyle) SerializationReadInt(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
	}
}