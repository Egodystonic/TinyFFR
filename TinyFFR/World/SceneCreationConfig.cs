// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Configuration for creating a new <see cref="Scene"/>.
/// </summary>
public readonly ref struct SceneCreationConfig : IConfigStruct<SceneCreationConfig> {
	/// <summary>
	/// The default value for <see cref="InitialBackdropColor"/>: a very dark grey (<c>0x080808</c>).
	/// </summary>
	/// <remarks>
	/// Deliberately near-black rather than pure black, so that a new scene provides a trace of ambient light and objects in it are not completely unlit.
	/// </remarks>
	public static readonly ColorVect DefaultInitialBackdropColor = ColorVect.FromRgb24(0x080808);

	/// <summary>
	/// Optional name for the new scene.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }
	/// <summary>
	/// A flat colour to use as the new scene's backdrop, or <see langword="null"/> for no colour backdrop. Defaults to <see cref="DefaultInitialBackdropColor"/>.
	/// </summary>
	/// <remarks>
	/// Ignored if <see cref="InitialBackdrop"/> or <see cref="InitialBackdropTexture"/> is also set, since a scene has only one backdrop.
	/// </remarks>
	public ColorVect? InitialBackdropColor { get; init; } = DefaultInitialBackdropColor;
	/// <summary>
	/// One of the built-in backdrops to start the new scene with, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	public BuiltInSceneBackdrop? InitialBackdrop { get; init; } = null;
	/// <summary>
	/// A loaded image to start the new scene with as its backdrop, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	public BackdropTexture? InitialBackdropTexture { get; init; } = null;

	/// <summary>
	/// Constructs a new <see cref="SceneCreationConfig"/> with default values for every setting.
	/// </summary>
	public SceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (InitialBackdrop != null && !Enum.IsDefined(InitialBackdrop.Value)) throw new ArgumentOutOfRangeException(nameof(InitialBackdrop.Value), InitialBackdrop.Value, null);
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in SceneCreationConfig src) {
		return	SerializationSizeOfNullableResource() // InitialBackdropTexture
			+	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfNullable<ColorVect>() // InitialBackdropColor
			+	SerializationSizeOfNullableInt(); // InitialBackdrop
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in SceneCreationConfig src) {
		SerializationWriteAndAllocateNullableResource(ref dest, src.InitialBackdropTexture);
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteNullable(ref dest, src.InitialBackdropColor);
		SerializationWriteNullableInt(ref dest, (int?) src.InitialBackdrop);
	}
	/// <inheritdoc/>
	public static SceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new SceneCreationConfig {
			InitialBackdropTexture = SerializationReadNullableResource<BackdropTexture>(ref src),
			Name = SerializationReadString(ref src),
			InitialBackdropColor = SerializationReadNullable<ColorVect>(ref src),
			InitialBackdrop = (BuiltInSceneBackdrop?) SerializationReadNullableInt(ref src),
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeNullableResourceHandle(src);
	}
}