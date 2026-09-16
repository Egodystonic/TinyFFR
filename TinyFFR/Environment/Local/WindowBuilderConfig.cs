// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Configuration for the <see cref="IWindowBuilder"/> itself, supplied when the factory is created. Settings that vary per window are given to <see cref="IWindowBuilder.CreateWindow(in WindowCreationConfig)"/> instead.
/// </summary>
public sealed record WindowBuilderConfig {
	/// <summary>
	/// The default value for <see cref="MaxWindowTitleLength"/>: <c>1024</c>.
	/// </summary>
	public const int DefaultMaxWindowTitleLength = 1024;
	/// <summary>
	/// The default value for <see cref="MaxIconFilePathLengthChars"/>: <c>2048</c>.
	/// </summary>
	public const int DefaultMaxIconFilePathLengthChars = 2048;

	/// <summary>
	/// The maximum length of any window title. Must be at least <c>1</c>. Defaults to <see cref="DefaultMaxWindowTitleLength"/>.
	/// </summary>
	/// <remarks>
	/// A buffer of this size is allocated once and reused for passing titles to and from the operating system, which is why the limit is fixed up front rather than
	/// per window. Titles longer than the limit are truncated rather than rejected. Note that the limit counts UTF-8 bytes rather than characters: every character
	/// in the ASCII range costs one, but accented letters and non-Latin scripts cost two or more each, so a title in such a script will truncate sooner than its
	/// character count alone suggests.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value less than <c>1</c>.</exception>
	public int MaxWindowTitleLength {
		get;
		init {
			if (value <= 0) throw new ArgumentOutOfRangeException(nameof(MaxWindowTitleLength), value, $"Must be at least 1.");
			field = value;
		}
	} = DefaultMaxWindowTitleLength;

	/// <summary>
	/// The maximum length of any file path passed to <see cref="Window.SetIcon"/>. Must be at least <c>1</c>. Defaults to <see cref="DefaultMaxIconFilePathLengthChars"/>.
	/// </summary>
	/// <remarks>
	/// As with <see cref="MaxWindowTitleLength"/>, a buffer of this size is allocated once and reused, and the limit counts UTF-8 bytes rather than characters. The
	/// default is comfortably above the longest path any mainstream operating system accepts.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value less than <c>1</c>.</exception>
	public int MaxIconFilePathLengthChars {
		get;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(MaxIconFilePathLengthChars), value, $"Must be at least 1.");
			}
			field = value;
		}
	} = DefaultMaxIconFilePathLengthChars;
}