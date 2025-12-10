// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System.Globalization;
using static Egodystonic.TinyFFR.Assets.Materials.ITextureBuilder;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalBuiltInTexturePathLibrary : IBuiltInTexturePathLibrary {
	public enum BuiltInTextureType {
		None,
		Texel,
		EmbeddedResourceTexture
	}

	public const string LocalBuiltInTexturePrefix = "?tffr_builtin?";
	public const string EmbeddedTextureResourcePrefix = "restex_";
	public const string MapTexelPrefix = "map_";
	public const string ByteValueTexelPrefix = "bytes_";
	public const string ByteValueSeparator = "_";
	// These are just combinations of the key tokens above to provide terseness below
	public const string Tex = LocalBuiltInTexturePrefix + EmbeddedTextureResourcePrefix;
	public const string Map = LocalBuiltInTexturePrefix + MapTexelPrefix;
	public const string Bytes = LocalBuiltInTexturePrefix + ByteValueTexelPrefix;
	public const string Sep = ByteValueSeparator;

	public ReadOnlySpan<char> DefaultColorMap => Map + "color";
	public ReadOnlySpan<char> DefaultNormalMap => Map + "normals";
	public ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicMap => Map + "orm";
	public ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicReflectanceMap => Map + "ormr";
	public ReadOnlySpan<char> DefaultOcclusionMap => Map + "occlusion";
	public ReadOnlySpan<char> DefaultRoughnessMap => Map + "roughness";
	public ReadOnlySpan<char> DefaultMetallicMap => Map + "metallic";
	public ReadOnlySpan<char> DefaultReflectanceMap => Map + "reflectance";
	public ReadOnlySpan<char> DefaultAbsorptionTransmissionMap => Map + "at";
	public ReadOnlySpan<char> DefaultAbsorptionMap => Map + "absorption";
	public ReadOnlySpan<char> DefaultTransmissionMap => Map + "transmission";
	public ReadOnlySpan<char> DefaultEmissiveMap => Map + "emissive";
	public ReadOnlySpan<char> DefaultEmissiveColorMap => Map + "emissive-color";
	public ReadOnlySpan<char> DefaultEmissiveIntensityMap => Map + "emissive-intensity";
	public ReadOnlySpan<char> DefaultAnisotropyMap => Map + "anisotropy";
	public ReadOnlySpan<char> DefaultAnisotropyRadialAngleMap => Map + "anisotropy-angle";
	public ReadOnlySpan<char> DefaultAnisotropyVectorMap => Map + "anisotropy-vector";
	public ReadOnlySpan<char> DefaultAnisotropyStrengthMap => Map + "anisotropy-strength";
	public ReadOnlySpan<char> DefaultClearCoatMap => Map + "clearcoat";
	public ReadOnlySpan<char> DefaultClearCoatThicknessMap => Map + "clearcoat-thickness";
	public ReadOnlySpan<char> DefaultClearCoatRoughnessMap => Map + "clearcoat-roughness";

	public ReadOnlySpan<char> Rgba100Percent => Bytes + $"255{Sep}255{Sep}255{Sep}255";
	public ReadOnlySpan<char> Rgba90Percent => Bytes + $"230{Sep}230{Sep}230{Sep}230";
	public ReadOnlySpan<char> Rgba80Percent => Bytes + $"204{Sep}204{Sep}204{Sep}204";
	public ReadOnlySpan<char> Rgba70Percent => Bytes + $"179{Sep}179{Sep}179{Sep}179";
	public ReadOnlySpan<char> Rgba60Percent => Bytes + $"153{Sep}153{Sep}153{Sep}153";
	public ReadOnlySpan<char> Rgba50Percent => Bytes + $"128{Sep}128{Sep}128{Sep}128";
	public ReadOnlySpan<char> Rgba40Percent => Bytes + $"102{Sep}102{Sep}102{Sep}102";
	public ReadOnlySpan<char> Rgba30Percent => Bytes + $"77{Sep}77{Sep}77{Sep}77";
	public ReadOnlySpan<char> Rgba20Percent => Bytes + $"51{Sep}51{Sep}51{Sep}51";
	public ReadOnlySpan<char> Rgba10Percent => Bytes + $"26{Sep}26{Sep}26{Sep}26";
	public ReadOnlySpan<char> Rgba0Percent => Bytes + $"0{Sep}0{Sep}0{Sep}0";

	public ReadOnlySpan<char> White => Bytes + $"255{Sep}255{Sep}255";
	public ReadOnlySpan<char> Black => Bytes + $"0{Sep}0{Sep}0";
	public ReadOnlySpan<char> Red => Bytes + $"255{Sep}0{Sep}0";
	public ReadOnlySpan<char> Green => Bytes + $"0{Sep}255{Sep}0";
	public ReadOnlySpan<char> Blue => Bytes + $"0{Sep}0{Sep}255";
	public ReadOnlySpan<char> RedGreen => Bytes + $"255{Sep}255{Sep}0";
	public ReadOnlySpan<char> GreenBlue => Bytes + $"0{Sep}255{Sep}255";
	public ReadOnlySpan<char> RedBlue => Bytes + $"255{Sep}0{Sep}255";

	public ReadOnlySpan<char> WhiteOpaque => Bytes + $"255{Sep}255{Sep}255{Sep}255";
	public ReadOnlySpan<char> BlackOpaque => Bytes + $"0{Sep}0{Sep}0{Sep}255";
	public ReadOnlySpan<char> RedOpaque => Bytes + $"255{Sep}0{Sep}0{Sep}255";
	public ReadOnlySpan<char> GreenOpaque => Bytes + $"0{Sep}255{Sep}0{Sep}255";
	public ReadOnlySpan<char> BlueOpaque => Bytes + $"0{Sep}0{Sep}255{Sep}255";
	public ReadOnlySpan<char> RedGreenOpaque => Bytes + $"255{Sep}255{Sep}0{Sep}255";
	public ReadOnlySpan<char> GreenBlueOpaque => Bytes + $"0{Sep}255{Sep}255{Sep}255";
	public ReadOnlySpan<char> RedBlueOpaque => Bytes + $"255{Sep}0{Sep}255{Sep}255";

	public ReadOnlySpan<char> WhiteTransparent => Bytes + $"255{Sep}255{Sep}255{Sep}0";
	public ReadOnlySpan<char> BlackTransparent => Bytes + $"0{Sep}0{Sep}0{Sep}0";
	public ReadOnlySpan<char> RedTransparent => Bytes + $"255{Sep}0{Sep}0{Sep}0";
	public ReadOnlySpan<char> GreenTransparent => Bytes + $"0{Sep}255{Sep}0{Sep}0";
	public ReadOnlySpan<char> BlueTransparent => Bytes + $"0{Sep}0{Sep}255{Sep}0";
	public ReadOnlySpan<char> RedGreenTransparent => Bytes + $"255{Sep}255{Sep}0{Sep}0";
	public ReadOnlySpan<char> GreenBlueTransparent => Bytes + $"0{Sep}255{Sep}255{Sep}0";
	public ReadOnlySpan<char> RedBlueTransparent => Bytes + $"255{Sep}0{Sep}255{Sep}0";

	public ReadOnlySpan<char> UvTestingTexture => Tex + "uv_testing";

	public BuiltInTextureType GetLikelyBuiltInTextureType(ReadOnlySpan<char> filePath) {
		if (!filePath.StartsWith(LocalBuiltInTexturePrefix, StringComparison.Ordinal)) return BuiltInTextureType.None;

		var filePathAfterBuiltInPrefix = filePath[LocalBuiltInTexturePrefix.Length..];
		if (filePathAfterBuiltInPrefix.StartsWith(MapTexelPrefix, StringComparison.Ordinal) || filePathAfterBuiltInPrefix.StartsWith(ByteValueTexelPrefix, StringComparison.Ordinal)) return BuiltInTextureType.Texel;
		if (filePathAfterBuiltInPrefix.StartsWith(EmbeddedTextureResourcePrefix, StringComparison.Ordinal)) return BuiltInTextureType.EmbeddedResourceTexture;
		
		return BuiltInTextureType.None;
	}

	public (EmbeddedResourceResolver.ResourceDataRef DataRef, bool ContainsAlpha, XYPair<int> Dimensions)? TryGetBuiltInEmbeddedResourceTexture(ReadOnlySpan<char> filePath) {
		if (!filePath.StartsWith(Tex)) return null;
		var filePathAfterBuiltInPrefix = filePath[Tex.Length..];

		return filePathAfterBuiltInPrefix switch {
			"uv_testing" => (EmbeddedResourceResolver.GetResource("Assets.uvtex.bin"), false, (2048, 2048)),
			_ => null
		};
	}

	public Pair<TexelRgb24?, TexelRgba32?>? TryGetBuiltInTexel(ReadOnlySpan<char> filePath) {
		if (!filePath.StartsWith(LocalBuiltInTexturePrefix)) return null;
		var filePathAfterBuiltInPrefix = filePath[LocalBuiltInTexturePrefix.Length..];

		if (filePathAfterBuiltInPrefix.StartsWith(MapTexelPrefix)) {
			return filePathAfterBuiltInPrefix[MapTexelPrefix.Length..] switch {
				"color" => (CreateColorTexel(DefaultColor).ToRgb24(), null),
				"normals" => (CreateNormalTexel(DefaultNormalOffset), null),
				"orm" => (CreateOcclusionRoughnessMetallicTexel(DefaultOcclusion, DefaultRoughness, DefaultMetallic), null),
				"ormr" => (null, CreateOcclusionRoughnessMetallicReflectanceTexel(DefaultOcclusion, DefaultRoughness, DefaultMetallic, DefaultReflectance)),
				"occlusion" => (TexelRgb24.FromNormalizedFloats(DefaultOcclusion, DefaultOcclusion, DefaultOcclusion), null),
				"roughness" => (TexelRgb24.FromNormalizedFloats(DefaultRoughness, DefaultRoughness, DefaultRoughness), null),
				"metallic" => (TexelRgb24.FromNormalizedFloats(DefaultMetallic, DefaultMetallic, DefaultMetallic), null),
				"reflectance" => (TexelRgb24.FromNormalizedFloats(DefaultReflectance, DefaultReflectance, DefaultReflectance), null),
				"at" => (null, CreateAbsorptionTransmissionTexel(DefaultAbsorption, DefaultTransmission)),
				"absorption" => (new TexelRgb24(DefaultAbsorption), null),
				"transmission" => (TexelRgb24.FromNormalizedFloats(DefaultTransmission, DefaultTransmission, DefaultTransmission), null),
				"emissive" => (null, CreateEmissiveTexel(DefaultEmissiveColor, DefaultEmissiveIntensity)),
				"emissive-color" => (new TexelRgb24(DefaultEmissiveColor), null),
				"emissive-intensity" => (TexelRgb24.FromNormalizedFloats(DefaultEmissiveIntensity, DefaultEmissiveIntensity, DefaultEmissiveIntensity), null),
				"anisotropy" => (CreateAnisotropyTexel(DefaultAnisotropyRadialAngle, DefaultAnisotropyStrength), null),
				"anisotropy-angle" => (TexelRgb24.FromNormalizedFloats(DefaultAnisotropyRadialAngle.Radians, DefaultAnisotropyRadialAngle.Radians, DefaultAnisotropyRadialAngle.Radians), null),
				"anisotropy-vector" => (CreateAnisotropyTexel(DefaultAnisotropyRadialAngle, 0f), null),
				"anisotropy-strength" => (TexelRgb24.FromNormalizedFloats(DefaultAnisotropyStrength, DefaultAnisotropyStrength, DefaultAnisotropyStrength), null),
				"clearcoat" => (CreateClearCoatTexel(DefaultClearCoatThickness, DefaultClearCoatRoughness), null),
				"clearcoat-thickness" => (TexelRgb24.FromNormalizedFloats(DefaultClearCoatThickness, DefaultClearCoatThickness, DefaultClearCoatThickness), null),
				"clearcoat-roughness" => (TexelRgb24.FromNormalizedFloats(DefaultClearCoatRoughness, DefaultClearCoatRoughness, DefaultClearCoatRoughness), null),
				_ => null
			};
		}

		if (filePathAfterBuiltInPrefix.StartsWith(ByteValueTexelPrefix)) {
			var byteSpecifyingText = filePathAfterBuiltInPrefix[ByteValueTexelPrefix.Length..];
			var byteValueEnumerator = byteSpecifyingText.Split(ByteValueSeparator);
			Span<byte> parsedBytes = stackalloc byte[4];
			var numBytesWritten = 0;

			while (byteValueEnumerator.MoveNext()) {
				if (numBytesWritten == 4) return null;

				if (!Byte.TryParse(byteSpecifyingText[byteValueEnumerator.Current], NumberStyles.None, CultureInfo.InvariantCulture, out var b)) return null;
				parsedBytes[numBytesWritten++] = b;
			}

			return numBytesWritten switch {
				3 => (new TexelRgb24(parsedBytes[0], parsedBytes[1], parsedBytes[2]), null),
				4 => (null, new TexelRgba32(parsedBytes[0], parsedBytes[1], parsedBytes[2], parsedBytes[3])),
				_ => null
			};
		}

		return null;
	}
}