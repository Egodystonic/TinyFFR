// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Generation Config for live generation of new ones
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct MeshReadConfig {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool FixCommonExportErrors { get; init; } = true;
	public bool OptimizeForGpu { get; init; } = true;

	public MeshReadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(MeshReadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}
}

public readonly ref struct MeshGenerationConfig {
	public Transform2D TextureTransform { get; init; } = Transform2D.None;

	public MeshGenerationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}

public readonly ref struct MeshCreationConfig {
	public bool FlipTriangles { get; init; } = false;
	public bool InvertTextureU { get; init; } = false;
	public bool InvertTextureV { get; init; } = false;
	public ReadOnlySpan<char> Name { get; init; }

	public MeshCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}