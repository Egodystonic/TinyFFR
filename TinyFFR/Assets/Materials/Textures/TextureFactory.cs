// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

static class TextureFactory {
	public static PooledHeapMemory<TTexel> AllocateTemporaryTexelBuffer<TTexel>(HeapPool pool, int width, int height) where TTexel : unmanaged, ITexel<TTexel> {
		if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be positive.");
		if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), height, $"Height must be positive.");
		return pool.Borrow<TTexel>(width * height, TTexel.SerializationByteSpanLength);
	}

	public static Texture GenerateSolidColorTexture(IMaterialBuilder builder, ColorVect color, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(builder);

		var config = new TextureCreationConfig {
			GenerateMipMaps = false,
			Width = 1,
			Height = 1,
			Name = name
		};

		// ReSharper disable once CompareOfFloatsByEqualityOperator In this case we want an explicit comparison
		if (color.Alpha == 1f) {
			var texel = (TexelRgb24) color;
			return builder.CreateTexture(new ReadOnlySpan<TexelRgb24>(in texel), config);
		}
		else {
			var texel = (TexelRgba32) color;
			return builder.CreateTexture(new ReadOnlySpan<TexelRgba32>(in texel), config);
		}
	}
}