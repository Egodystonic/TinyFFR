// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Benchmarks.Harness;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	public static void ProceduralTextures() {
		using var colorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Color Map");
		using var alphaColorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.AlphaColorPattern, includeAlpha: true, "Benchmark Alpha Color Map");
		using var canvasMap = Factory.TextureBuilder.CreateCanvasTexture(BenchmarkAssets.ColorPattern, includeAlpha: true, "Benchmark Canvas Map");
		using var normalMap = Factory.TextureBuilder.CreateNormalMap(BenchmarkAssets.NormalPattern, "Benchmark Normal Map");
		using var ormMap = Factory.TextureBuilder.CreateOcclusionRoughnessMetallicMap(
			BenchmarkAssets.OcclusionPattern, BenchmarkAssets.RoughnessPattern, BenchmarkAssets.MetallicPattern, "Benchmark ORM Map"
		);
		using var ormrMap = Factory.TextureBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			BenchmarkAssets.OcclusionPattern, BenchmarkAssets.RoughnessPattern, BenchmarkAssets.MetallicPattern, BenchmarkAssets.ReflectancePattern, "Benchmark ORMR Map"
		);
		using var emissiveMap = Factory.TextureBuilder.CreateEmissiveMap(BenchmarkAssets.ColorPattern, BenchmarkAssets.EmissiveIntensityPattern, "Benchmark Emissive Map");
		using var anisotropyMap = Factory.TextureBuilder.CreateAnisotropyMap(BenchmarkAssets.AnisotropyAnglePattern, BenchmarkAssets.AnisotropyStrengthPattern, "Benchmark Anisotropy Map");
		using var clearCoatMap = Factory.TextureBuilder.CreateClearCoatMap(BenchmarkAssets.ThicknessPattern, BenchmarkAssets.RoughnessPattern, "Benchmark Clear Coat Map");
		using var absorptionMap = Factory.TextureBuilder.CreateAbsorptionTransmissionMap(BenchmarkAssets.ColorPattern, BenchmarkAssets.TransmissionPattern, "Benchmark Absorption Map");
	}

	public static void TextureMipmappingAndCompression() {
		for (var repeat = 0; repeat < SmokeWorkload.TextureVariantRepeatCount; ++repeat) CreateOneTextureVariantSet();
	}

	static void CreateOneTextureVariantSet() {
		using var mipmapped = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, new TextureCreationConfig {
			DataType = TextureDataType.ColorSrgb,
			GenerateMipMaps = true,
			Name = "Benchmark Mipmapped Color Map"
		});
		using var unmipmapped = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, new TextureCreationConfig {
			DataType = TextureDataType.ColorSrgb,
			GenerateMipMaps = false,
			Name = "Benchmark Unmipmapped Color Map"
		});
		using var compressed = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, new TextureCreationConfig {
			DataType = TextureDataType.ColorSrgb,
			GenerateMipMaps = true,
			CompressionQuality = Quality.Standard,
			Name = "Benchmark Compressed Color Map"
		});
		using var dynamic = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, new TextureCreationConfig {
			DataType = TextureDataType.ColorSrgb,
			AllowsDynamicWrites = true,
			Name = "Benchmark Dynamic Color Map"
		});
	}

	public static void BuiltInTextures() {
		var paths = Factory.AssetLoader.BuiltInTexturePaths;

		for (var repeat = 0; repeat < SmokeWorkload.BuiltInTextureRepeatCount; ++repeat) {
			using var defaultColor = Factory.AssetLoader.LoadTexture(paths.DefaultColorMap, TextureDataType.ColorSrgb, "Benchmark Built In Color Map");
			using var defaultNormal = Factory.AssetLoader.LoadTexture(paths.DefaultNormalMap, TextureDataType.LinearDataUnitVector, "Benchmark Built In Normal Map");
			using var defaultOrm = Factory.AssetLoader.LoadTexture(paths.DefaultOcclusionRoughnessMetallicMap, TextureDataType.LinearData, "Benchmark Built In ORM Map");
			using var defaultOrmr = Factory.AssetLoader.LoadTexture(paths.DefaultOcclusionRoughnessMetallicReflectanceMap, TextureDataType.LinearData, "Benchmark Built In ORMR Map");
			using var defaultOcclusion = Factory.AssetLoader.LoadTexture(paths.DefaultOcclusionMap, TextureDataType.LinearData, "Benchmark Built In Occlusion Map");
			using var defaultRoughness = Factory.AssetLoader.LoadTexture(paths.DefaultRoughnessMap, TextureDataType.LinearData, "Benchmark Built In Roughness Map");
			using var defaultMetallic = Factory.AssetLoader.LoadTexture(paths.DefaultMetallicMap, TextureDataType.LinearData, "Benchmark Built In Metallic Map");
			using var defaultReflectance = Factory.AssetLoader.LoadTexture(paths.DefaultReflectanceMap, TextureDataType.LinearData, "Benchmark Built In Reflectance Map");
			using var defaultAbsorption = Factory.AssetLoader.LoadTexture(paths.DefaultAbsorptionTransmissionMap, TextureDataType.ColorSrgb, "Benchmark Built In Absorption Map");
			using var defaultEmissive = Factory.AssetLoader.LoadTexture(paths.DefaultEmissiveMap, TextureDataType.ColorSrgb, "Benchmark Built In Emissive Map");
			using var defaultAnisotropy = Factory.AssetLoader.LoadTexture(paths.DefaultAnisotropyMap, TextureDataType.LinearData, "Benchmark Built In Anisotropy Map");
			using var defaultClearCoat = Factory.AssetLoader.LoadTexture(paths.DefaultClearCoatMap, TextureDataType.LinearDataTwoChannelMax, "Benchmark Built In Clear Coat Map");
			using var halfAlpha = Factory.AssetLoader.LoadTexture(paths.Rgba50Percent, TextureDataType.ColorSrgb, "Benchmark Built In Half Alpha Map");
		}

		for (var repeat = 0; repeat < SmokeWorkload.BuiltInTextureMetadataRepeatCount; ++repeat) {
			_ = Factory.AssetLoader.ReadTextureMetadata(paths.DefaultColorMap);
			_ = Factory.AssetLoader.ReadTextureMetadata(paths.DefaultNormalMap);
			_ = Factory.AssetLoader.ReadTextureMetadata(paths.UvTestingTexture);
		}
	}

	public static void LoadTextureFiles() {
		for (var repeat = 0; repeat < SmokeWorkload.TextureFileLoadRepeatCount; ++repeat) LoadOneTextureFileSet();
	}

	static void LoadOneTextureFileSet() {
		using var albedo = Factory.AssetLoader.LoadTexture(BenchmarkAssets.CrateAlbedoTex, TextureDataType.ColorSrgb, "Benchmark Loaded Albedo");
		using var normal = Factory.AssetLoader.LoadTexture(BenchmarkAssets.CrateNormalTex, TextureDataType.LinearDataUnitVector, "Benchmark Loaded Normal");
		using var bitmap = Factory.AssetLoader.LoadTexture(BenchmarkAssets.SwatchTex, TextureDataType.ColorSrgb, "Benchmark Loaded Swatch");

		_ = Factory.AssetLoader.ReadTextureMetadata(BenchmarkAssets.SwatchTex);

		using var converted = Factory.AssetLoader.LoadTexture(BenchmarkAssets.CrateNormalTex, new TextureCreationConfig {
			DataType = TextureDataType.LinearDataUnitVector,
			ProcessingToApply = TextureProcessingConfig.Invert(includeRedChannel: false, includeGreenChannel: true, includeBlueChannel: false, includeAlphaChannel: false),
			Name = "Benchmark Converted Normal"
		});
	}

	public static void LoadCombinedTextures() {
		for (var repeat = 0; repeat < SmokeWorkload.CombinedTextureRepeatCount; ++repeat) LoadOneCombinedTexture();
	}

	static void LoadOneCombinedTexture() {
		using var combined = Factory.AssetLoader.LoadCombinedTexture(
			BenchmarkAssets.SwatchTex,
			BenchmarkAssets.WhiteTex,
			new TextureCombinationConfig(
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
			),
			new TextureCreationConfig {
				DataType = TextureDataType.ColorSrgb,
				Name = "Benchmark Combined Texture"
			}
		);
	}
}
