// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

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

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(CameraCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Width < 1) {
			ThrowArgException(Width, "Width and height must both be positive values.");
		}
		if (Height < 1) {
			ThrowArgException(Height, "Width and height must both be positive values.");
		}
	}
}