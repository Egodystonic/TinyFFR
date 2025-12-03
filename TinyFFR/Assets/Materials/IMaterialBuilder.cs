// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Xml.Linq;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialBuilder {
	ITextureBuilder TextureBuilder { get; }

	Material CreateTestMaterial(bool ignoresLighting = true);

	Material CreateSimpleMaterial(Texture colorMap, Texture? emissiveMap = null, ReadOnlySpan<char> name = default) {
		return CreateSimpleMaterial(new SimpleMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
			Name = name
		});
	}
	Material CreateSimpleMaterial(in SimpleMaterialCreationConfig config);

	Material CreateStandardMaterial(Texture colorMap, Texture? normalMap = null, Texture? ormOrOrmrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, Texture? clearCoatMap = null, StandardMaterialAlphaMode? alphaMode = null, ReadOnlySpan<char> name = default) {
		return CreateStandardMaterial(new StandardMaterialCreationConfig {
			ColorMap = colorMap,
			NormalMap = normalMap,
			OcclusionRoughnessMetallicReflectanceMap = ormOrOrmrMap,
			AnisotropyMap = anisotropyMap,
			EmissiveMap = emissiveMap,
			ClearCoatMap = clearCoatMap,
			AlphaMode = alphaMode ?? StandardMaterialCreationConfig.DefaultAlphaMode,
			Name = name
		});
	}
	Material CreateStandardMaterial(in StandardMaterialCreationConfig config);

	Material CreateTransmissiveMaterial(Texture colorMap, Texture absorptionTransmissionMap, TransmissiveMaterialQuality? quality = null, Texture? normalMap = null, Texture? ormrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, TransmissiveMaterialAlphaMode? alphaMode = null, float? refractionThickness = null, ReadOnlySpan<char> name = default) {
		return CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
			ColorMap = colorMap,
			AbsorptionTransmissionMap = absorptionTransmissionMap,
			NormalMap = normalMap,
			OcclusionRoughnessMetallicReflectanceMap = ormrMap,
			AnisotropyMap = anisotropyMap,
			EmissiveMap = emissiveMap,
			Quality = quality ?? TransmissiveMaterialCreationConfig.DefaultQuality,
			AlphaMode = alphaMode ?? TransmissiveMaterialCreationConfig.DefaultAlphaMode,
			RefractionThickness = refractionThickness ?? TransmissiveMaterialCreationConfig.DefaultRefractionThickness,
			Name = name
		});
	}
	Material CreateTransmissiveMaterial(in TransmissiveMaterialCreationConfig config);
}