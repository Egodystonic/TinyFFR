// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct TextureLoadConfig {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool IncludeWAlphaChannel { get; init; } = false;

	public bool GenerateMipMaps { get; init; } = true;
	public bool FlipX { get; init; } = false;
	public bool FlipY { get; init; } = false;
	public bool InvertXRedChannel { get; init; } = false;
	public bool InvertYGreenChannel { get; init; } = false;
	public bool InvertZBlueChannel { get; init; } = false;
	public bool InvertWAlphaChannel { get; init; } = false;
	public ReadOnlySpan<char> Name { get; init; }

	public TextureLoadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(TextureLoadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}
}

public readonly ref struct TextureCreationConfig {
	public required int Width { get; init; }
	public required int Height { get; init; }

	public bool GenerateMipMaps { get; init; } = true;
	public bool FlipX { get; init; } = false;
	public bool FlipY { get; init; } = false;
	public bool InvertXRedChannel { get; init; } = false;
	public bool InvertYGreenChannel { get; init; } = false;
	public bool InvertZBlueChannel { get; init; } = false;
	public bool InvertWAlphaChannel { get; init; } = false;
	public ReadOnlySpan<char> Name { get; init; }

	public TextureCreationConfig() { }
	
	public static TextureCreationConfig FromLoadConfig(TextureLoadConfig loadConfig, int width, int height) {
		return new TextureCreationConfig {
			Width = width,
			Height = height,
			GenerateMipMaps = loadConfig.GenerateMipMaps,
			FlipX = loadConfig.FlipX,
			FlipY = loadConfig.FlipY,
			InvertXRedChannel = loadConfig.InvertXRedChannel,
			InvertYGreenChannel = loadConfig.InvertYGreenChannel,
			InvertZBlueChannel = loadConfig.InvertZBlueChannel,
			InvertWAlphaChannel = loadConfig.InvertWAlphaChannel,
			Name = loadConfig.Name
		};
	}

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