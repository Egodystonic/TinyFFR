// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// A collection of file paths for textures built in to TinyFFR, for use where a map is needed but no file is available for it.
/// </summary>
/// <remarks>
/// <para>
/// Every path here can be passed to any of the asset loader's texture-loading methods exactly as though it were a real file on
/// disc. The textures are generated rather than read, so nothing is shipped alongside your application and loading one is
/// effectively free.
/// </para>
/// <para>
/// The usual reason to want these is that a downloaded material supplies only some of the maps a material type needs (a
/// roughness map but no metallic map, say) and the missing one has to be filled with something sensible.
/// </para>
/// </remarks>
public interface IBuiltInTexturePathLibrary {
	/// <summary>
	/// A colour map that is entirely opaque white, so that a surface using it shows only the colour of the light falling on it.
	/// </summary>
	ReadOnlySpan<char> DefaultColorMap { get; }
	/// <summary>
	/// A normal map describing a perfectly flat surface, i.e. one with no bumps or grooves of its own.
	/// </summary>
	ReadOnlySpan<char> DefaultNormalMap { get; }
	/// <summary>
	/// An ORM map combining <see cref="DefaultOcclusionMap"/>, <see cref="DefaultRoughnessMap"/> and
	/// <see cref="DefaultMetallicMap"/> in to its three channels.
	/// </summary>
	ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicMap { get; }
	/// <summary>
	/// An ORMR map combining <see cref="DefaultOcclusionMap"/>, <see cref="DefaultRoughnessMap"/>,
	/// <see cref="DefaultMetallicMap"/> and <see cref="DefaultReflectanceMap"/> in to its four channels.
	/// </summary>
	ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicReflectanceMap { get; }
	/// <summary>
	/// An occlusion map of <c>1f</c> everywhere, meaning no part of the surface is shadowed by its own shape.
	/// </summary>
	ReadOnlySpan<char> DefaultOcclusionMap { get; }
	/// <summary>
	/// A roughness map of <c>0.4f</c> everywhere, giving a surface that is neither mirror-smooth nor completely matte.
	/// </summary>
	ReadOnlySpan<char> DefaultRoughnessMap { get; }
	/// <summary>
	/// A metallic map of <c>0f</c> everywhere, meaning the surface is not metallic at all.
	/// </summary>
	/// <remarks>
	/// This is the map to supply when a downloaded material has no metallic data, which usually means it was never intended to
	/// look metallic.
	/// </remarks>
	ReadOnlySpan<char> DefaultMetallicMap { get; }
	/// <summary>
	/// A reflectance map of <c>0.5f</c> everywhere, which is the ordinary reflectance of most non-metallic surfaces.
	/// </summary>
	ReadOnlySpan<char> DefaultReflectanceMap { get; }
	/// <summary>
	/// An absorption-transmission map combining <see cref="DefaultAbsorptionMap"/> in its colour channels and
	/// <see cref="DefaultTransmissionMap"/> in its alpha channel.
	/// </summary>
	ReadOnlySpan<char> DefaultAbsorptionTransmissionMap { get; }
	/// <summary>
	/// An absorption map that is entirely black, meaning no colour of light is absorbed and everything passes through the
	/// surface untinted.
	/// </summary>
	ReadOnlySpan<char> DefaultAbsorptionMap { get; }
	/// <summary>
	/// A transmission map of <c>0.5f</c> everywhere, letting half the light through the surface.
	/// </summary>
	ReadOnlySpan<char> DefaultTransmissionMap { get; }
	/// <summary>
	/// An emissive map combining <see cref="DefaultEmissiveColorMap"/> and <see cref="DefaultEmissiveIntensityMap"/>.
	/// </summary>
	ReadOnlySpan<char> DefaultEmissiveMap { get; }
	/// <summary>
	/// An emissive colour map in the warm yellow-white of an incandescent light bulb.
	/// </summary>
	ReadOnlySpan<char> DefaultEmissiveColorMap { get; }
	/// <summary>
	/// An emissive intensity map of <c>1f</c> everywhere, i.e. glowing at full strength.
	/// </summary>
	ReadOnlySpan<char> DefaultEmissiveIntensityMap { get; }
	/// <summary>
	/// An anisotropy map combining <see cref="DefaultAnisotropyRadialAngleMap"/> and
	/// <see cref="DefaultAnisotropyStrengthMap"/>.
	/// </summary>
	ReadOnlySpan<char> DefaultAnisotropyMap { get; }
	/// <summary>
	/// An angle-formatted anisotropy map of <c>0°</c> everywhere.
	/// </summary>
	ReadOnlySpan<char> DefaultAnisotropyRadialAngleMap { get; }
	/// <summary>
	/// A vector-formatted anisotropy map at <c>0°</c> with zero strength, i.e. one that produces no anisotropic effect at all.
	/// </summary>
	ReadOnlySpan<char> DefaultAnisotropyVectorMap { get; }
	/// <summary>
	/// An anisotropy strength map of <c>1f</c> everywhere, i.e. full strength.
	/// </summary>
	ReadOnlySpan<char> DefaultAnisotropyStrengthMap { get; }
	/// <summary>
	/// A clearcoat map combining <see cref="DefaultClearCoatThicknessMap"/> and <see cref="DefaultClearCoatRoughnessMap"/>.
	/// </summary>
	ReadOnlySpan<char> DefaultClearCoatMap { get; }
	/// <summary>
	/// A clearcoat thickness map of <c>1f</c> everywhere, i.e. a maximally thick coat.
	/// </summary>
	ReadOnlySpan<char> DefaultClearCoatThicknessMap { get; }
	/// <summary>
	/// A clearcoat roughness map of <c>0f</c> everywhere, i.e. a completely glossy coat.
	/// </summary>
	ReadOnlySpan<char> DefaultClearCoatRoughnessMap { get; }

	/// <summary>
	/// A four-channel texture with every channel at <c>255</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba100Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>230</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba90Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>204</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba80Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>179</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba70Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>153</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba60Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>128</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba50Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>102</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba40Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>77</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba30Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>51</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba20Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>26</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba10Percent { get; }
	/// <summary>
	/// A four-channel texture with every channel at <c>0</c>.
	/// </summary>
	ReadOnlySpan<char> Rgba0Percent { get; }

	/// <summary>
	/// A three-channel white texture, <c>(255, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> White { get; }
	/// <summary>
	/// A three-channel black texture, <c>(0, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> Black { get; }
	/// <summary>
	/// A three-channel red texture, <c>(255, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> Red { get; }
	/// <summary>
	/// A three-channel green texture, <c>(0, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> Green { get; }
	/// <summary>
	/// A three-channel blue texture, <c>(0, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> Blue { get; }
	/// <summary>
	/// A three-channel yellow texture, <c>(255, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> RedGreen { get; }
	/// <summary>
	/// A three-channel cyan texture, <c>(0, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> GreenBlue { get; }
	/// <summary>
	/// A three-channel magenta texture, <c>(255, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> RedBlue { get; }

	/// <summary>
	/// A four-channel fully opaque white texture, <c>(255, 255, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> WhiteOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque black texture, <c>(0, 0, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> BlackOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque red texture, <c>(255, 0, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> RedOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque green texture, <c>(0, 255, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> GreenOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque blue texture, <c>(0, 0, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> BlueOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque yellow texture, <c>(255, 255, 0, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> RedGreenOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque cyan texture, <c>(0, 255, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> GreenBlueOpaque { get; }
	/// <summary>
	/// A four-channel fully opaque magenta texture, <c>(255, 0, 255, 255)</c>.
	/// </summary>
	ReadOnlySpan<char> RedBlueOpaque { get; }

	/// <summary>
	/// A four-channel fully transparent white texture, <c>(255, 255, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> WhiteTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent black texture, <c>(0, 0, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> BlackTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent red texture, <c>(255, 0, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> RedTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent green texture, <c>(0, 255, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> GreenTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent blue texture, <c>(0, 0, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> BlueTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent yellow texture, <c>(255, 255, 0, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> RedGreenTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent cyan texture, <c>(0, 255, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> GreenBlueTransparent { get; }
	/// <summary>
	/// A four-channel fully transparent magenta texture, <c>(255, 0, 255, 0)</c>.
	/// </summary>
	ReadOnlySpan<char> RedBlueTransparent { get; }

	/// <summary>
	/// A colour map for checking how a mesh's texture coordinates are laid out; this is the map the test material uses.
	/// </summary>
	/// <remarks>
	/// Applying this to a mesh makes it obvious where a texture ends up stretched, mirrored or wrapped unexpectedly.
	/// </remarks>
	ReadOnlySpan<char> UvTestingTexture { get; }
}
