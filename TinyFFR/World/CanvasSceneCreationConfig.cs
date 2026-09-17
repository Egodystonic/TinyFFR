// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Configuration for creating a new <see cref="CanvasScene"/>.
/// </summary>
public readonly ref struct CanvasSceneCreationConfig : IConfigStruct<CanvasSceneCreationConfig> {
	/// <summary>
	/// The underlying scene configuration this canvas is built on.
	/// </summary>
	/// <remarks>
	/// Defaults to a scene with no backdrop colour, since a canvas is normally drawn over other content rather than filling the image itself.
	/// </remarks>
	public SceneCreationConfig BaseConfig { get; private init; } = new() { InitialBackdropColor = null };

	/// <summary>
	/// Optional name for the new canvas.
	/// </summary>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// A colour to fill the new canvas with behind its contents, or <see langword="null"/> (the default) for a transparent background.
	/// </summary>
	public ColorVect? InitialBackgroundColor {
		get => BaseConfig.InitialBackdropColor;
		init => BaseConfig = BaseConfig with { InitialBackdropColor = value };
	}

	/// <summary>
	/// Constructs a new <see cref="CanvasSceneCreationConfig"/> with default values for every setting.
	/// </summary>
	public CanvasSceneCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="CanvasSceneCreationConfig"/> built on the given scene configuration.
	/// </summary>
	/// <param name="baseConfig">The configuration to use as this config's <see cref="BaseConfig"/>.</param>
	public CanvasSceneCreationConfig(SceneCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	internal SceneCreationConfig ToSceneCreationConfig() => BaseConfig;

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in CanvasSceneCreationConfig src) {
		return SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in CanvasSceneCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc/>
	public static CanvasSceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new CanvasSceneCreationConfig {
			BaseConfig = SerializationReadSubConfig<SceneCreationConfig>(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SceneCreationConfig.DisposeAllocatedHeapStorage(src[sizeof(int)..]);
	}
}
