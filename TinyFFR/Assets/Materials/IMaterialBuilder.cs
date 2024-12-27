// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Textures;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialBuilder {
	Texture CreateSolidColorTexture(ColorVect color, ReadOnlySpan<char> name = default) => TextureFactory.GenerateSolidColorTexture(this, color, name);
	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;

	Material CreateStandardMaterial(Texture albedo, ReadOnlySpan<char> name = default) {
		return CreateStandardMaterial(new StandardMaterialCreationConfig {
			Albedo = albedo,
			Name = name
		});
	}
	Material CreateStandardMaterial(in StandardMaterialCreationConfig config);
}