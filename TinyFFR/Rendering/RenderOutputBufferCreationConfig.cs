// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using System;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct RenderOutputBufferCreationConfig {
	public static readonly XYPair<int> DefaultTextureDimensions = (2560, 1440);
	public const int MaxTextureDimensionXY = 32_768;
	public const int MinTextureDimensionXY = 1;

	public XYPair<int> TextureDimensions { get; init; } = DefaultTextureDimensions;

	public ReadOnlySpan<char> Name { get; init; }

	public RenderOutputBufferCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(RenderOutputBufferCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (TextureDimensions.Clamp(new(MinTextureDimensionXY), new(MaxTextureDimensionXY)) != TextureDimensions) {
			ThrowArgException(TextureDimensions, $"must have both X and Y values between {MinTextureDimensionXY} and {MaxTextureDimensionXY}.");
		}
	}
}