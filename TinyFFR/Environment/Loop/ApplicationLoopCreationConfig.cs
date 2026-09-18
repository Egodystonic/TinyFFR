// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Environment;

/// <summary>
/// Configuration for creating a new <see cref="ApplicationLoop"/>.
/// </summary>
public readonly ref struct ApplicationLoopCreationConfig : IConfigStruct<ApplicationLoopCreationConfig> {
	internal readonly TimeSpan FrameInterval = TimeSpan.Zero;

	/// <summary>
	/// The maximum number of iterations per second the new loop should run at. Must be positive, or <see langword="null"/> (the default) for no cap.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Note that if <see cref="RendererBuilderConfig.EnableVSync"/> was set to <c>true</c> (i.e. vsync is enabled; the default) usually you'll want to leave this
	/// as <c>null</c> so the render loop follows the target <see cref="Display"/>'s refresh rate.
	/// </para>
	/// <para>
	/// This sets the loop's initial <see cref="ApplicationLoop.TargetFrameRate"/>, which can be changed at any time thereafter. Note that it is a cap and not a
	/// guarantee: the loop will not run faster than this, but it can run slower if an iteration takes longer than the cap allows for.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value that is zero or negative. Use <see langword="null"/> for no cap.</exception>
	public int? FrameRateCapHz {
		get {
			return FrameInterval <= TimeSpan.Zero ? null : (int) Math.Round(TimeSpan.FromSeconds(1d) / FrameInterval, 0, MidpointRounding.AwayFromZero);
		}
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateCapHz), value, $"Frame rate cap must be a positive value, or 'null' for no cap.");
			}
			FrameInterval = value != null ? (TimeSpan.FromSeconds(1d) / value.Value) : TimeSpan.Zero;
		}
	}

	/// <summary>
	/// Optional name for the new application loop.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="ApplicationLoopCreationConfig"/> with default values for every setting.
	/// </summary>
	public ApplicationLoopCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" - Yes, for now. Keeping this as a placeholder for future.
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in ApplicationLoopCreationConfig src) {
		return	SerializationSizeOfNullableInt() // FrameRateCapHz
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ApplicationLoopCreationConfig src) {
		SerializationWriteNullableInt(ref dest, src.FrameRateCapHz);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc/>
	public static ApplicationLoopCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ApplicationLoopCreationConfig {
			FrameRateCapHz = SerializationReadNullableInt(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}