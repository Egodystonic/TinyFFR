// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Generation Config for live generation of new ones
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct TextureReadConfig {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool IncludeWAlphaChannel { get; init; } = false;

	public TextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(TextureReadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}
}

public readonly ref struct TextureCreationConfig {
	public bool GenerateMipMaps { get; init; } = true;
	public bool FlipX { get; init; } = false;
	public bool FlipY { get; init; } = false;
	public bool InvertXRedChannel { get; init; } = false;
	public bool InvertYGreenChannel { get; init; } = false;
	public bool InvertZBlueChannel { get; init; } = false;
	public bool InvertWAlphaChannel { get; init; } = false;
	public bool IsLinearColorspace { get; init; } = true;
	public ReadOnlySpan<char> Name { get; init; }

	public TextureCreationConfig() { }

	internal void ThrowIfInvalid() { /* no-op */ }
}

public readonly ref struct TextureGenerationConfig {
	public required int Width { get; init; }
	public required int Height { get; init; }

	public TextureGenerationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(TextureCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Width < 1) {
			ThrowArgException(Width, "must be positive.");
		}
		if (Height < 1) {
			ThrowArgException(Height, "must be positive.");
		}
	}
}