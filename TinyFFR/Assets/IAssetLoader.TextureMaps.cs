// Created on 2026-08-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR.Assets;

public partial interface IAssetLoader {
	#region Color & Canvas
	/// <summary>
	/// Loads a texture as a colour map, which supplies the base colour of a surface.
	/// </summary>
	/// <remarks>
	/// Colour maps are loaded in sRGB colourspace.
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadColorMap(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTexture(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));

	/// <summary>
	/// Asynchronously loads a texture as a colour map, which supplies the base colour of a surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Colour maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadColorMapAsync(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));

	/// <summary>
	/// Loads a texture for drawing flat on a canvas (e.g. via a <see cref="CanvasScene"/>) rather than mapped on to a surface in the world.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Canvas textures skip the colour conversion and filtering a texture in the world goes through, because a canvas element
	/// is always viewed face-on and must appear exactly as authored.
	/// </para>
	/// <para>
	/// Canvas textures are loaded in linear colourspace as the standard <see cref="RenderQualityConfig"/> for <see cref="CanvasScene"/>s disables
	/// the colourspace conversion pipeline.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	Texture LoadCanvasTexture(ReadOnlySpan<char> filePath) => LoadTexture(filePath, TextureCreationConfig.ForCanvasTexture(Path.GetFileName(filePath)));

	/// <summary>
	/// Asynchronously loads a texture for drawing flat on a canvas rather than mapped on to a surface in the world.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Canvas textures skip the colour conversion and filtering a texture in the world goes through, because a canvas element
	/// is always viewed face-on and must appear exactly as authored.
	/// </para>
	/// <para>
	/// Canvas textures are loaded in linear colourspace as the standard <see cref="RenderQualityConfig"/> for <see cref="CanvasScene"/>s disables
	/// the colourspace conversion pipeline.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	TinyFfrAsyncOperation<Texture> LoadCanvasTextureAsync(ReadOnlySpan<char> filePath) => LoadTextureAsync(filePath, TextureCreationConfig.ForCanvasTexture(Path.GetFileName(filePath)));
	#endregion

	#region Normal
	/// <summary>
	/// Loads a normal map, which describes the small-scale bumps and grooves of a surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// TinyFFR expects normal maps in the OpenGL convention. A DirectX-format file can be loaded by setting
	/// <paramref name="isDirectXFormat"/>, which converts it as it loads at a small cost in load time.
	/// </para>
	/// <para>
	/// Normal maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="isDirectXFormat">Whether the file is in the DirectX convention rather than the OpenGL one.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadNormalMap(ReadOnlySpan<char> filePath, bool isDirectXFormat = false, Quality? compressionQuality = null) {
		if (!isDirectXFormat) return LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataUnitVector, compressionQuality, Path.GetFileName(filePath)));
		return LoadTexture(
			filePath,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataUnitVector, compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = new TextureProcessingConfig { InvertYGreenChannel = true }
			}
		);
	}

	/// <summary>
	/// Asynchronously loads a normal map, which describes the small-scale bumps and grooves of a surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// TinyFFR expects normal maps in the OpenGL convention. A DirectX-format file can be loaded by setting
	/// <paramref name="isDirectXFormat"/>, which converts it as it loads at a small cost in load time.
	/// </para>
	/// <para>
	/// Normal maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="isDirectXFormat">Whether the file is in the DirectX convention rather than the OpenGL one.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadNormalMapAsync(ReadOnlySpan<char> filePath, bool isDirectXFormat = false, Quality? compressionQuality = null) {
		if (!isDirectXFormat) return LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataUnitVector, compressionQuality, Path.GetFileName(filePath)));
		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataUnitVector, compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = new TextureProcessingConfig { InvertYGreenChannel = true }
			}
		);
	}
	#endregion

	#region ORM & ORMR
	/// <summary>
	/// Loads an ORM map, whose three channels hold occlusion, roughness and metallic data.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ORM maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)));
	/// <summary>
	/// Loads an ORM (occlusion, roughness, metallic) data map by combining three separate single-channel files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 3 sources.
	/// </para>
	/// <para>
	/// ORM maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionFilePath">The path of the occlusion file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="metallicFilePath">The path of the metallic file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R)
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads an ORM map, whose three channels hold occlusion, roughness and metallic values.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// ORM maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicMapAsync(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)));
	/// <summary>
	/// Asynchronously loads an ORM (occlusion, roughness, metallic) map by combining three separate single-channel files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// ORM maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionFilePath">The path of the occlusion file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="metallicFilePath">The path of the metallic file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicMapAsync(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c);

		return LoadCombinedTextureAsync(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R)
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}

	/// <summary>
	/// Loads an ORMR map, whose four channels hold occlusion, roughness, metallic and reflectance values.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)));
		else return LoadOcclusionRoughnessMetallicReflectanceMap(filePath, BuiltInTexturePaths.DefaultReflectanceMap);
	}
	/// <summary>
	/// Loads an ORMR (occlusion, roughness, metallic, reflectance) map by combining a three-channel ORM file with a separate reflectance file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 2 sources.
	/// </para>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionRoughnessMetallicFilePath">The path of the ORM file to load. Must name an existing file in a supported format.</param>
	/// <param name="reflectanceFilePath">The path of the reflectance file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionRoughnessMetallicFilePath, ReadOnlySpan<char> reflectanceFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionRoughnessMetallicFilePath);
		var b = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			occlusionRoughnessMetallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}
	/// <summary>
	/// Loads an ORMR (occlusion, roughness, metallic, reflectance) map by combining four separate single-channel files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 4 sources.
	/// </para>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionFilePath">The path of the occlusion file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="metallicFilePath">The path of the metallic file to load. Must name an existing file in a supported format.</param>
	/// <param name="reflectanceFilePath">The path of the reflectance file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, ReadOnlySpan<char> reflectanceFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		var d = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c, "+", d)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c, "+", d);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R),
				OutputTextureWAlphaChannelSource = new(TextureD, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads an ORMR map, whose four channels hold occlusion, roughness, metallic and reflectance values.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)));
		else return LoadOcclusionRoughnessMetallicReflectanceMapAsync(filePath, BuiltInTexturePaths.DefaultReflectanceMap);
	}
	/// <summary>
	/// Asynchronously loads an ORMR map by combining a three-channel ORM file with a separate reflectance file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionRoughnessMetallicFilePath">The path of the ORM file to load. Must name an existing file in a supported format.</param>
	/// <param name="reflectanceFilePath">The path of the reflectance file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> occlusionRoughnessMetallicFilePath, ReadOnlySpan<char> reflectanceFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionRoughnessMetallicFilePath);
		var b = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			occlusionRoughnessMetallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}
	/// <summary>
	/// Asynchronously loads an ORMR map by combining four separate single-channel files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// ORMR maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="occlusionFilePath">The path of the occlusion file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="metallicFilePath">The path of the metallic file to load. Must name an existing file in a supported format.</param>
	/// <param name="reflectanceFilePath">The path of the reflectance file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, ReadOnlySpan<char> reflectanceFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		var d = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c, "+", d)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c, "+", d);

		return LoadCombinedTextureAsync(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R),
				OutputTextureWAlphaChannelSource = new(TextureD, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}
	#endregion

	#region AT
	/// <summary>
	/// Loads an absorption-transmission map, which describes how light passes through a see-through surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The colour channels hold the absorption — what the surface takes out of the light passing through it — and the fourth
	/// channel how much light gets through at all. Only transmissive materials use this.
	/// </para>
	/// <para>
	/// AT maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="invertAbsorption">Whether to invert the absorption data as it loads. Set this when supplying an ordinary colour image, whose colours say what is <i>seen</i> through the surface rather than what is absorbed by it.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> filePath, bool invertAbsorption = false, Quality? compressionQuality = null) {
		var includesTransmission = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		if (!includesTransmission) return LoadAbsorptionTransmissionMap(filePath, BuiltInTexturePaths.DefaultTransmissionMap, invertAbsorption, compressionQuality);
		if (!invertAbsorption) return LoadTexture(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));

		return LoadTexture(
			filePath,
			TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = TextureProcessingConfig.Invert(includeAlphaChannel: false)
			}
		);
	}
	/// <summary>
	/// Loads an absorption-transmission map by combining separate absorption and transmission files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 2 sources.
	/// </para>
	/// <para>
	/// The colour channels hold the absorption — what the surface takes out of the light passing through it — and the fourth
	/// channel how much light gets through at all. Only transmissive materials use this.
	/// </para>
	/// <para>
	/// AT maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="absorptionFilePath">The path of the absorption file to load. Must name an existing file in a supported format.</param>
	/// <param name="transmissionFilePath">The path of the transmission file to load. Must name an existing file in a supported format.</param>
	/// <param name="invertAbsorption">Whether to invert the absorption data as it loads. Set this when supplying an ordinary colour image, whose colours say what is <i>seen</i> through the surface rather than what is absorbed by it.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> absorptionFilePath, ReadOnlySpan<char> transmissionFilePath, bool invertAbsorption = false, Quality? compressionQuality = null) {
		var a = Path.GetFileName(absorptionFilePath);
		var b = Path.GetFileName(transmissionFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			absorptionFilePath, invertAbsorption ? TextureProcessingConfig.Invert(includeAlphaChannel: false) : TextureProcessingConfig.None,
			transmissionFilePath, TextureProcessingConfig.None,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R)
			},
			TextureCreationConfig.ForColorTexture(compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads an absorption-transmission map, which describes how light passes through a see-through surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// The colour channels hold the absorption — what the surface takes out of the light passing through it — and the fourth
	/// channel how much light gets through at all. Only transmissive materials use this.
	/// </para>
	/// <para>
	/// AT maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="invertAbsorption">Whether to invert the absorption data as it loads. Set this when supplying an ordinary colour image, whose colours say what is <i>seen</i> through the surface rather than what is absorbed by it.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAbsorptionTransmissionMapAsync(ReadOnlySpan<char> filePath, bool invertAbsorption = false, Quality? compressionQuality = null) {
		var includesTransmission = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		if (!includesTransmission) return LoadAbsorptionTransmissionMapAsync(filePath, BuiltInTexturePaths.DefaultTransmissionMap, invertAbsorption, compressionQuality);
		if (!invertAbsorption) return LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));

		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = TextureProcessingConfig.Invert(includeAlphaChannel: false)
			}
		);
	}
	/// <summary>
	/// Asynchronously loads an absorption-transmission map by combining separate absorption and transmission files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// The colour channels hold the absorption — what the surface takes out of the light passing through it — and the fourth
	/// channel how much light gets through at all. Only transmissive materials use this.
	/// </para>
	/// <para>
	/// AT maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="absorptionFilePath">The path of the absorption file to load. Must name an existing file in a supported format.</param>
	/// <param name="transmissionFilePath">The path of the transmission file to load. Must name an existing file in a supported format.</param>
	/// <param name="invertAbsorption">Whether to invert the absorption data as it loads. Set this when supplying an ordinary colour image, whose colours say what is <i>seen</i> through the surface rather than what is absorbed by it.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAbsorptionTransmissionMapAsync(ReadOnlySpan<char> absorptionFilePath, ReadOnlySpan<char> transmissionFilePath, bool invertAbsorption = false, Quality? compressionQuality = null) {
		var a = Path.GetFileName(absorptionFilePath);
		var b = Path.GetFileName(transmissionFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			absorptionFilePath, invertAbsorption ? TextureProcessingConfig.Invert(includeAlphaChannel: false) : TextureProcessingConfig.None,
			transmissionFilePath, TextureProcessingConfig.None,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R)
			},
			TextureCreationConfig.ForColorTexture(compressionQuality, name)
		);
	}
	#endregion

	#region Emissive
	/// <summary>
	/// Loads an emissive map, which makes parts of a surface appear to glow with their own light.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A three-channel file glows at full strength everywhere it is not black; a four-channel file uses its fourth channel
	/// as the strength of the glow.
	/// </para>
	/// <para>
	/// Emissive maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadEmissiveMap(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));
		else return LoadEmissiveMap(filePath, BuiltInTexturePaths.DefaultEmissiveIntensityMap);
	}
	/// <summary>
	/// Loads an emissive map by combining separate colour and intensity files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 2 sources.
	/// </para>
	/// <para>
	/// A three-channel file glows at full strength everywhere it is not black; a four-channel file uses its fourth channel
	/// as the strength of the glow.
	/// </para>
	/// <para>
	/// Emissive maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="emissiveColorFilePath">The path of the emissive colour file to load. Must name an existing file in a supported format.</param>
	/// <param name="emissiveIntensityFilePath">The path of the emissive intensity file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadEmissiveMap(ReadOnlySpan<char> emissiveColorFilePath, ReadOnlySpan<char> emissiveIntensityFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(emissiveColorFilePath);
		var b = Path.GetFileName(emissiveIntensityFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			emissiveColorFilePath,
			emissiveIntensityFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForColorTexture(compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads an emissive map, which makes parts of a surface appear to glow with their own light.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// A three-channel file glows at full strength everywhere it is not black; a four-channel file uses its fourth channel
	/// as the strength of the glow.
	/// </para>
	/// <para>
	/// Emissive maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadEmissiveMapAsync(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(compressionQuality, Path.GetFileName(filePath)));
		else return LoadEmissiveMapAsync(filePath, BuiltInTexturePaths.DefaultEmissiveIntensityMap);
	}
	/// <summary>
	/// Asynchronously loads an emissive map by combining separate colour and intensity files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// A three-channel file glows at full strength everywhere it is not black; a four-channel file uses its fourth channel
	/// as the strength of the glow.
	/// </para>
	/// <para>
	/// Emissive maps are loaded in sRGB colourspace.
	/// </para>
	/// </remarks>
	/// <param name="emissiveColorFilePath">The path of the emissive colour file to load. Must name an existing file in a supported format.</param>
	/// <param name="emissiveIntensityFilePath">The path of the emissive intensity file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadEmissiveMapAsync(ReadOnlySpan<char> emissiveColorFilePath, ReadOnlySpan<char> emissiveIntensityFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(emissiveColorFilePath);
		var b = Path.GetFileName(emissiveIntensityFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			emissiveColorFilePath,
			emissiveIntensityFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForColorTexture(compressionQuality, name)
		);
	}
	#endregion

	#region Anisotropy
	private sealed class RadialAngleAnisotropyArguments {
		public Orientation2D ZeroDirection { get; set; }
		public AnisotropyRadialAngleRange EncodedRange { get; set; }
		public bool EncodedAnticlockwise { get; set; }
		public ColorChannel? StrengthChannel { get; set; }
	}

	private static readonly Lock _staticMutationLock = new();
	private static readonly unsafe ArrayPoolBackedObjectPool<RadialAngleAnisotropyArguments> _radialAngleAnisotropyArgumentPool = new(&CreateRadialAngleAnisotropyArguments);

	private static RadialAngleAnisotropyArguments CreateRadialAngleAnisotropyArguments() => new();

	private static RadialAngleAnisotropyArguments RentRadialAngleAnisotropyArguments(Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		RadialAngleAnisotropyArguments result;
		lock (_staticMutationLock) result = _radialAngleAnisotropyArgumentPool.Rent();
		result.ZeroDirection = zeroDirection;
		result.EncodedRange = encodedRange;
		result.EncodedAnticlockwise = encodedAnticlockwise;
		result.StrengthChannel = strengthChannel;
		return result;
	}

	private static void ReturnRadialAngleAnisotropyArguments(RadialAngleAnisotropyArguments arguments) {
		lock (_staticMutationLock) _radialAngleAnisotropyArgumentPool.Return(arguments);
	}

	private static void ApplyRadialAngleAnisotropyConversionRgb24(Span<TexelRgb24> texels, object? argument) {
		var arguments = (RadialAngleAnisotropyArguments) argument!;
		try {
			ConvertRadialAngleToVectorFormatAnisotropy(texels, arguments.ZeroDirection, arguments.EncodedRange, arguments.EncodedAnticlockwise, arguments.StrengthChannel);
		}
		finally {
			ReturnRadialAngleAnisotropyArguments(arguments);
		}
	}

	private static void ApplyRadialAngleAnisotropyConversionRgba32(Span<TexelRgba32> texels, object? argument) {
		var arguments = (RadialAngleAnisotropyArguments) argument!;
		try {
			ConvertRadialAngleToVectorFormatAnisotropy(texels, arguments.ZeroDirection, arguments.EncodedRange, arguments.EncodedAnticlockwise, arguments.StrengthChannel);
		}
		finally {
			ReturnRadialAngleAnisotropyArguments(arguments);
		}
	}

	private static unsafe TextureProcessingConfig CreateRadialAngleAnisotropyProcessingConfig(bool includesAlphaChannel, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		return new TextureProcessingConfig {
			PostProcessingFunction = includesAlphaChannel
				? TexelProcessingFunction.Create<TexelRgba32>(&ApplyRadialAngleAnisotropyConversionRgba32)
				: TexelProcessingFunction.Create<TexelRgb24>(&ApplyRadialAngleAnisotropyConversionRgb24),
			PostProcessingArgument = RentRadialAngleAnisotropyArguments(zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
		};
	}

	private static TextureCombinationConfig RadialAngleAnisotropyCombinationConfig => new(
		TextureCombinationScalingStrategy.PixelUpscale,
		new TextureCombinationSource(TextureA, R),
		new TextureCombinationSource(TextureA, G),
		new TextureCombinationSource(TextureB, R)
	);
	
	/// <summary>
	/// Loads an anisotropy map stored in vector form.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> filePath, ColorChannel? strengthChannel, Quality? compressionQuality = null) {
		return strengthChannel switch {
			B => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath))),
			A => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)) with { ProcessingToApply = TextureProcessingConfig.Swizzle(blueSource: A) }),
			_ => LoadAnisotropyMapVectorFormatted(filePath, BuiltInTexturePaths.DefaultAnisotropyStrengthMap, compressionQuality)
		};
	}
	/// <summary>
	/// Loads an anisotropy map by combining a vector-formatted file with a separate strength file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 2 sources.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="vectorFilePath">The path of the vector-formatted anisotropy file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthFilePath">The path of the strength file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> vectorFilePath, ReadOnlySpan<char> strengthFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(vectorFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			vectorFilePath,
			strengthFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads an anisotropy map already stored in the vector form TinyFFR uses internally.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapVectorFormattedAsync(ReadOnlySpan<char> filePath, ColorChannel? strengthChannel, Quality? compressionQuality = null) {
		return strengthChannel switch {
			B => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath))),
			A => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)) with { ProcessingToApply = TextureProcessingConfig.Swizzle(blueSource: A) }),
			_ => LoadAnisotropyMapVectorFormattedAsync(filePath, BuiltInTexturePaths.DefaultAnisotropyStrengthMap, compressionQuality)
		};
	}
	/// <summary>
	/// Asynchronously loads an anisotropy map by combining a vector-formatted file with a separate strength file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="vectorFilePath">The path of the vector-formatted anisotropy file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthFilePath">The path of the strength file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapVectorFormattedAsync(ReadOnlySpan<char> vectorFilePath, ReadOnlySpan<char> strengthFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(vectorFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			vectorFilePath,
			strengthFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name)
		);
	}

	/// <summary>
	/// Converts a span of three-channel anisotropy texels from the angle form to the vector form TinyFFR uses internally.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this to convert an angle-formatted texture ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// </remarks>
	/// <param name="texels">The texels to convert, in place.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgb24> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient);
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength);
			}
		}
	}
	/// <summary>
	/// Converts a span of four-channel anisotropy texels from the angle form to the vector form TinyFFR uses internally.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this to convert an angle-formatted texture ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// </remarks>
	/// <param name="texels">The texels to convert, in place.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgba32> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B or A) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient).ToRgba32();
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength).ToRgba32();
			}
		}
	}
	/// <summary>
	/// Loads an anisotropy map whose direction is stored as an angle in a single channel.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// <para>
	/// Converting angle-formatted data to the vector form used internally takes some time at load. Where that matters,
	/// <see cref="ConvertRadialAngleToVectorFormatAnisotropy(Span{Materials.TexelRgb24}, Orientation2D, AnisotropyRadialAngleRange, bool, ColorChannel?)"/>
	/// can be used to do the conversion ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> filePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel, Quality? compressionQuality = null) {
		var includesAlphaChannel = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		return LoadTexture(
			filePath,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(includesAlphaChannel, zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
			},
			new TextureReadConfig { IncludeWAlphaChannel = includesAlphaChannel, ForceWAlphaChannelPresence = includesAlphaChannel }
		);
	}
	/// <summary>
	/// Loads an anisotropy map from separate angle-formatted direction and strength files, converting as it loads.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// <para>
	/// Converting angle-formatted data to the vector form used internally takes some time at load. Where that matters,
	/// <see cref="ConvertRadialAngleToVectorFormatAnisotropy(Span{Materials.TexelRgb24}, Orientation2D, AnisotropyRadialAngleRange, bool, ColorChannel?)"/>
	/// can be used to do the conversion ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="radialAngleFilePath">The path of the angle-formatted anisotropy file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthFilePath">The path of the strength file to load. Must name an existing file in a supported format.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> radialAngleFilePath, ReadOnlySpan<char> strengthFilePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, Quality? compressionQuality = null) {
		var a = Path.GetFileName(radialAngleFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			radialAngleFilePath,
			strengthFilePath,
			RadialAngleAnisotropyCombinationConfig,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(false, zeroDirection, encodedRange, encodedAnticlockwise, B)
			}
		);
	}

	/// <summary>
	/// Asynchronously loads an anisotropy map whose direction is stored as an angle in a single channel.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// <para>
	/// Converting angle-formatted data to the vector form used internally takes some time at load. Where that matters,
	/// <see cref="ConvertRadialAngleToVectorFormatAnisotropy(Span{Materials.TexelRgb24}, Orientation2D, AnisotropyRadialAngleRange, bool, ColorChannel?)"/>
	/// can be used to do the conversion ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="strengthChannel">Which channel of the file holds the strength of the effect, or <see langword="null"/> if it holds none, in which case every texel is taken to be at full strength.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapRadialAngleFormattedAsync(ReadOnlySpan<char> filePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel, Quality? compressionQuality = null) {
		var includesAlphaChannel = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, Path.GetFileName(filePath)) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(includesAlphaChannel, zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
			},
			new TextureReadConfig { IncludeWAlphaChannel = includesAlphaChannel, ForceWAlphaChannelPresence = includesAlphaChannel }
		);
	}
	/// <summary>
	/// Asynchronously loads an anisotropy map from separate angle-formatted direction and strength files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Anisotropy describes surfaces that reflect light unevenly in different directions — brushed metal, whose highlights
	/// stretch along the direction of the brushing rather than forming a round spot. Vector-formatted maps store the direction as a
	/// vector across the surface in the first two channels, and the strength of the effect in the third.
	/// </para>
	/// <para>
	/// Angle-formatted maps instead store that data as a single channel of values indicating an angle of the effect and another channel
	/// indicating the strength of the effect.
	/// </para>
	/// <para>
	/// Converting angle-formatted data to the vector form used internally takes some time at load. Where that matters,
	/// <see cref="ConvertRadialAngleToVectorFormatAnisotropy(Span{Materials.TexelRgb24}, Orientation2D, AnisotropyRadialAngleRange, bool, ColorChannel?)"/>
	/// can be used to do the conversion ahead of time.
	/// </para>
	/// <para>
	/// Anisotropy maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="radialAngleFilePath">The path of the angle-formatted anisotropy file to load. Must name an existing file in a supported format.</param>
	/// <param name="strengthFilePath">The path of the strength file to load. Must name an existing file in a supported format.</param>
	/// <param name="zeroDirection">Which direction across the surface the channel's lowest value stands for.</param>
	/// <param name="encodedRange">Whether the channel's full range of values covers a complete turn or a half turn.</param>
	/// <param name="encodedAnticlockwise">Whether increasing values turn anticlockwise across the surface. Pass <see langword="false"/> where they turn clockwise instead.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapRadialAngleFormattedAsync(ReadOnlySpan<char> radialAngleFilePath, ReadOnlySpan<char> strengthFilePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, Quality? compressionQuality = null) {
		var a = Path.GetFileName(radialAngleFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			radialAngleFilePath,
			strengthFilePath,
			RadialAngleAnisotropyCombinationConfig,
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, compressionQuality, name) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(false, zeroDirection, encodedRange, encodedAnticlockwise, B)
			}
		);
	}
	#endregion

	#region Clearcoat
	/// <summary>
	/// Loads a clearcoat map, which describes a thin glossy layer over the top of a surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A clearcoat is a thin glossy layer over the top of a surface, like lacquer or wax, which reflects light in its own
	/// right on top of whatever the surface beneath it does. The first channel holds the coat's thickness and the second its
	/// roughness.
	/// </para>
	/// <para>
	/// Clearcoat maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadClearCoatMap(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataTwoChannelMax, compressionQuality, Path.GetFileName(filePath)));
	/// <summary>
	/// Loads a clearcoat map by combining separate thickness and roughness files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A clearcoat is a thin glossy layer over the top of a surface, like lacquer or wax, which reflects light in its own
	/// right on top of whatever the surface beneath it does. The first channel holds the coat's thickness and the second its
	/// roughness.
	/// </para>
	/// <para>
	/// Where separate files hold each piece of data, this loads them and packs them in to one texture's channels. The result
	/// takes the largest width and height of any of the 2 sources.
	/// </para>
	/// <para>
	/// Clearcoat maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="thicknessFilePath">The path of the thickness file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	Texture LoadClearCoatMap(ReadOnlySpan<char> thicknessFilePath, ReadOnlySpan<char> roughnessFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(thicknessFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			thicknessFilePath,
			roughnessFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureA, B)
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataTwoChannelMax, compressionQuality, name)
		);
	}

	/// <summary>
	/// Asynchronously loads a clearcoat map, which describes a thin glossy layer over the top of a surface.
	/// </summary>
	/// <remarks>
	///  <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// A clearcoat is a thin glossy layer over the top of a surface, like lacquer or wax, which reflects light in its own
	/// right on top of whatever the surface beneath it does. The first channel holds the coat's thickness and the second its
	/// roughness.
	/// </para>
	/// <para>
	/// Clearcoat maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadClearCoatMapAsync(ReadOnlySpan<char> filePath, Quality? compressionQuality = null) => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataTwoChannelMax, compressionQuality, Path.GetFileName(filePath)));
	/// <summary>
	/// Asynchronously loads a clearcoat map by combining separate thickness and roughness files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// A clearcoat is a thin glossy layer over the top of a surface, like lacquer or wax, which reflects light in its own
	/// right on top of whatever the surface beneath it does. The first channel holds the coat's thickness and the second its
	/// roughness.
	/// </para>
	/// <para>
	/// Clearcoat maps are loaded in linear colourspace.
	/// </para>
	/// </remarks>
	/// <param name="thicknessFilePath">The path of the thickness file to load. Must name an existing file in a supported format.</param>
	/// <param name="roughnessFilePath">The path of the roughness file to load. Must name an existing file in a supported format.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Note that compression takes time, slowing the load process down. Lower compression quality levels are faster; higher levels should generally only be used when baking assets (via the <see cref="IAssetBakery" />) as they take a considerable amount of time.</param>
	TinyFfrAsyncOperation<Texture> LoadClearCoatMapAsync(ReadOnlySpan<char> thicknessFilePath, ReadOnlySpan<char> roughnessFilePath, Quality? compressionQuality = null) {
		var a = Path.GetFileName(thicknessFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			thicknessFilePath,
			roughnessFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureA, B)
			},
			TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataTwoChannelMax, compressionQuality, name)
		);
	}
	#endregion
}
