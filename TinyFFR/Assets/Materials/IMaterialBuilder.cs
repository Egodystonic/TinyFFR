// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Xml.Linq;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Assembles textures in to the materials that give surfaces their appearance. Every object in the world requires a <see cref="Material"/> definition
/// to be rendered.
/// </summary>
/// <remarks>
/// Every material must be disposed when nothing uses it any more, and before the textures it was built from are disposed.
/// </remarks>
public interface IMaterialBuilder {
	/// <summary>
	/// The builder used to create the textures that materials are assembled from.
	/// </summary>
	ITextureBuilder TextureBuilder { get; }

	/// <summary>
	/// A special material that requires no input textures and allows displaying geometry according to a
	/// variety of built-in render modes (see <see cref="DefaultMaterialShadingStyle"/> on a per-object basis.
	/// </summary>
	Material DefaultMaterial { get; }

	/// <summary>
	/// Creates a material showing a UV test pattern, for checking how a mesh's texture coordinates are laid out.
	/// </summary>
	/// <remarks>
	/// Applying this to a mesh makes it obvious where a texture ends up stretched, mirrored or wrapped unexpectedly.
	/// </remarks>
	/// <param name="ignoresLighting">Whether the material should be unaffected by the scene's lighting. Leaving this <see langword="false"/> gives a standard material that also carries a subtle normal-map pattern.</param>
	Material CreateTestMaterial(bool ignoresLighting = false);

	/// <summary>
	/// Creates a simple material that shows its colour map exactly as given, unaffected by any light in the scene.
	/// </summary>
	/// <remarks>
	/// Because nothing about the lighting matters, a surface using one of these looks the same however the scene is lit, which
	/// makes it the right choice for diagnostic overlays or non-realistic workflows.
	/// </remarks>
	/// <param name="colorMap">The texture supplying the surface's colour.</param>
	/// <param name="enablePerInstanceEffects">Whether objects using this material may alter it individually at runtime.
	/// This makes the material markedly more expensive to render whether or not the effects are used; but enables per-instance <see cref="ModelInstance.MaterialEffects">material effects</see>.</param>
	/// <param name="name">The name to give the material. May be left empty.</param>
	Material CreateLightingIgnoringMaterial(Texture colorMap, bool enablePerInstanceEffects = false, ReadOnlySpan<char> name = default) {
		return CreateLightingIgnoringMaterial(new LightingIgnoringMaterialCreationConfig {
			ColorMap = colorMap,
			EnablePerInstanceEffects = enablePerInstanceEffects,
			Name = name
		});
	}
	/// <summary>
	/// Creates a simple material that shows its colour map exactly as given, unaffected by any light in the scene, using the given config.
	/// </summary>
	/// <remarks>
	/// Because nothing about the lighting matters, a surface using one of these looks the same however the scene is lit, which
	/// makes it the right choice for diagnostic overlays or non-realistic workflows.
	/// </remarks>
	/// <param name="config">The full set of settings for the material.</param>
	Material CreateLightingIgnoringMaterial(in LightingIgnoringMaterialCreationConfig config);

	/// <summary>
	/// Creates a material that can be used with <see cref="ModelInstance.SetKeyedMaterialColor"/>.
	/// </summary>
	/// <remarks>
	/// Color-keyed materials use a key teture whose R, G, B, and A values represent a strength mapping of per-object colour values
	/// (set via <see cref="ModelInstance.SetKeyedMaterialColor"/>.
	/// </remarks>
	/// <param name="keyMap">The texture whose four channels key the user-selectable colour intensities. Can be three or four channel.</param>
	/// <param name="blendOutputAlphaWithScene">Whether the result blends with the scene behind it rather than replacing it.
	/// Setting this to <c>true</c> turns on alpha-sorting for this material which allows setting key colours with translucency,
	/// but alpha-sorting can sometimes "flicker" against similar objects sharing the same space in the world. Alpha-sorting also
	/// has a slight additional performance cost.</param>
	/// <param name="name">The name to give the material. May be left empty.</param>
	Material CreateColorKeyedMaterial(Texture keyMap, bool blendOutputAlphaWithScene = false, ReadOnlySpan<char> name = default) {
		return CreateColorKeyedMaterial(new ColorKeyedMaterialCreationConfig {
			KeyMap = keyMap,
			BlendOutputAlphaWithScene = blendOutputAlphaWithScene,
			Name = name
		});
	}
	/// <summary>
	/// Creates a material that can be used with <see cref="ModelInstance.SetKeyedMaterialColor"/>, using the given config.
	/// </summary>
	/// <remarks>
	/// Color-keyed materials use a key teture whose R, G, B, and A values represent a strength mapping of per-object colour values
	/// (set via <see cref="ModelInstance.SetKeyedMaterialColor"/>.
	/// </remarks>
	/// <param name="config">The full set of settings for the material.</param>
	Material CreateColorKeyedMaterial(in ColorKeyedMaterialCreationConfig config);

	/// <summary>
	/// Creates a physically-based material for an opaque surface, which is what most objects in a scene use.
	/// </summary>
	/// <remarks>
	/// Only the colour map is required. Each further map supplied makes the material more expensive to render, so supply only
	/// those actually needed; colour, normal and ORM covers most surfaces.
	/// </remarks>
	/// <param name="colorMap">The texture supplying the surface's base colour.</param>
	/// <param name="normalMap">The texture describing the surface's small-scale bumps and grooves, or <see langword="null"/> for a perfectly smooth surface.</param>
	/// <param name="ormOrOrmrMap">The texture supplying occlusion, roughness and metallic values, optionally with reflectance in a fourth channel, or <see langword="null"/> for none.</param>
	/// <param name="anisotropyMap">The texture describing where the surface reflects light unevenly in different directions, or <see langword="null"/> for none.</param>
	/// <param name="emissiveMap">The texture describing where the surface appears to glow with its own light, or <see langword="null"/> for none.</param>
	/// <param name="clearCoatMap">The texture describing a thin glossy layer over the top of the surface, or <see langword="null"/> for none.</param>
	/// <param name="alphaMode">How the colour map's alpha channel is interpreted, or <see langword="null"/> for the default of <see cref="StandardMaterialAlphaMode.MaskOnly"/>.</param>
	/// <param name="enablePerInstanceEffects">Whether objects using this material may alter it individually at runtime.
	/// This makes the material markedly more expensive to render whether or not the effects are used; but enables per-instance <see cref="ModelInstance.MaterialEffects">material effects</see>.</param>
	/// <param name="name">The name to give the material. May be left empty.</param>
	Material CreateStandardMaterial(Texture colorMap, Texture? normalMap = null, Texture? ormOrOrmrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, Texture? clearCoatMap = null, StandardMaterialAlphaMode? alphaMode = null, bool enablePerInstanceEffects = false, ReadOnlySpan<char> name = default) {
		return CreateStandardMaterial(new StandardMaterialCreationConfig {
			ColorMap = colorMap,
			NormalMap = normalMap,
			OcclusionRoughnessMetallicReflectanceMap = ormOrOrmrMap,
			AnisotropyMap = anisotropyMap,
			EmissiveMap = emissiveMap,
			ClearCoatMap = clearCoatMap,
			AlphaMode = alphaMode ?? StandardMaterialCreationConfig.DefaultAlphaMode,
			EnablePerInstanceEffects = enablePerInstanceEffects,
			Name = name
		});
	}
	/// <summary>
	/// Creates a physically-based material for an opaque surface, which is what most objects in a scene use, using the given config.
	/// </summary>
	/// <param name="config">The full set of settings for the material.</param>
	Material CreateStandardMaterial(in StandardMaterialCreationConfig config);

	/// <summary>
	/// Creates a physically-based material for a surface that light passes through, such as glass or water.
	/// </summary>
	/// <remarks>
	/// Transmissive materials are markedly more expensive to render than standard ones, and stacking several transmissive
	/// objects in front of each other is not currently well-supported.
	/// </remarks>
	/// <param name="colorMap">The texture supplying the surface's base colour.</param>
	/// <param name="absorptionTransmissionMap">The texture describing how light passes through the surface: which colours it absorbs, and how much light gets through at all.</param>
	/// <param name="quality">How much of the scene the surface may reflect and refract, or <see langword="null"/> for the default of <see cref="TransmissiveMaterialQuality.FullReflectionsAndRefraction"/>.</param>
	/// <param name="normalMap">The texture describing the surface's small-scale bumps and grooves, or <see langword="null"/> for a perfectly smooth surface.</param>
	/// <param name="ormrMap">The texture supplying occlusion, roughness, metallic and reflectance values, or <see langword="null"/> for none. Must be a four-channel texture.</param>
	/// <param name="anisotropyMap">The texture describing where the surface reflects light unevenly in different directions, or <see langword="null"/> for none.</param>
	/// <param name="emissiveMap">The texture describing where the surface appears to glow with its own light, or <see langword="null"/> for none.</param>
	/// <param name="alphaMode">How the colour map's alpha channel is interpreted, or <see langword="null"/> for the default of <see cref="TransmissiveMaterialAlphaMode.FullBlending"/>.</param>
	/// <param name="refractionThickness">How thick the surface's material is modelled as being, in metres, or <see langword="null"/> for the default of <c>0.1f</c>.
	/// Set it to roughly how far a typical ray would travel inside the object before emerging.</param>
	/// <param name="enablePerInstanceEffects">Whether objects using this material may alter it individually at runtime.
	/// This makes the material markedly more expensive to render whether or not the effects are used; but enables per-instance <see cref="ModelInstance.MaterialEffects">material effects</see>.</param>
	/// <param name="name">The name to give the material. May be left empty.</param>
	Material CreateTransmissiveMaterial(Texture colorMap, Texture absorptionTransmissionMap, TransmissiveMaterialQuality? quality = null, Texture? normalMap = null, Texture? ormrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, TransmissiveMaterialAlphaMode? alphaMode = null, float? refractionThickness = null, bool enablePerInstanceEffects = false, ReadOnlySpan<char> name = default) {
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
			EnablePerInstanceEffects = enablePerInstanceEffects,
			Name = name
		});
	}
	/// <summary>
	/// Creates a physically-based material for a surface that light passes through, such as glass or water, using the given config.
	/// </summary>
	/// <remarks>
	/// Transmissive materials are markedly more expensive to render than standard ones, and stacking several transmissive
	/// objects in front of each other is not currently well-supported.
	/// </remarks>
	/// <param name="config">The full set of settings for the material.</param>
	Material CreateTransmissiveMaterial(in TransmissiveMaterialCreationConfig config);
}