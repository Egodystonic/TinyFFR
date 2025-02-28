// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly ref struct MeshLoadConfig {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool FixCommonExportErrors { get; init; } = true;
	public bool OptimizeForGpu { get; init; } = true;

	public bool FlipTriangles { get; init; } = false;
	public bool InvertTextureU { get; init; } = false;
	public bool InvertTextureV { get; init; } = false;
	public ReadOnlySpan<char> Name { get; init; }

	public MeshLoadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(MeshLoadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}
}

public readonly ref struct MeshCreationConfig {
	public bool FlipTriangles { get; init; } = false;
	public bool InvertTextureU { get; init; } = false;
	public bool InvertTextureV { get; init; } = false;
	public ReadOnlySpan<char> Name { get; init; }

	public MeshCreationConfig() { }

	public static MeshCreationConfig FromLoadConfig(MeshLoadConfig loadConfig) {
		return new MeshCreationConfig {
			FlipTriangles = loadConfig.FlipTriangles,
			InvertTextureU = loadConfig.InvertTextureU,
			InvertTextureV = loadConfig.InvertTextureV,
			Name = loadConfig.Name
		};
	}

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}