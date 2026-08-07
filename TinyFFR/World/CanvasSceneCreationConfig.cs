// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct CanvasSceneCreationConfig : IConfigStruct<CanvasSceneCreationConfig> {
	public SceneCreationConfig BaseConfig { get; private init; } = new() { InitialBackdropColor = null };

	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public ColorVect? InitialBackgroundColor {
		get => BaseConfig.InitialBackdropColor;
		init => BaseConfig = BaseConfig with { InitialBackdropColor = value };
	}

	public CanvasSceneCreationConfig() { }
	public CanvasSceneCreationConfig(SceneCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	internal SceneCreationConfig ToSceneCreationConfig() => BaseConfig;

	public static int GetHeapStorageFormattedLength(in CanvasSceneCreationConfig src) {
		return SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in CanvasSceneCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static CanvasSceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new CanvasSceneCreationConfig {
			BaseConfig = SerializationReadSubConfig<SceneCreationConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SceneCreationConfig.DisposeAllocatedHeapStorage(src[sizeof(int)..]);
	}
}
