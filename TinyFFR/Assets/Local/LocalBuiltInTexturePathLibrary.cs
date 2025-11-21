// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Globalization;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.Assets.Materials.ITextureBuilder;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalBuiltInTexturePathLibrary : IBuiltInTexturePathLibrary {
	public const string LocalBuiltInTexturePrefix = "?tffr_builtin?";
	public const string MapTexelPrefix = "map_";
	public const string ByteValueTexelPrefix = "bytes_";
	public const string ByteValueSeparator = "_";
	// These are just combinations of the key tokens above to provide terseness below
	public const string Map = LocalBuiltInTexturePrefix + MapTexelPrefix;
	public const string Bytes = LocalBuiltInTexturePrefix + ByteValueTexelPrefix;
	public const string Sep = ByteValueSeparator;

	public ReadOnlySpan<char> DefaultColorMap => Map + "color";
	public ReadOnlySpan<char> DefaultNormalMap => Map + "normals";
	public ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicMap => Map + "orm";
	public ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicReflectanceMap => Map + "ormr";
	public ReadOnlySpan<char> DefaultAbsorptionTransmissionMap => Map + "at";
	public ReadOnlySpan<char> DefaultEmissiveMap => Map + "emissive";
	public ReadOnlySpan<char> DefaultAnisotropyMap => Map + "anisotropy";
	public ReadOnlySpan<char> DefaultClearCoatMap => Map + "clearcoat";

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

	public bool IsBuiltIn(ReadOnlySpan<char> filePath) => filePath.StartsWith(LocalBuiltInTexturePrefix);
	
	public Pair<TexelRgb24?, TexelRgba32?>? GetBuiltInTexel(ReadOnlySpan<char> filePath) {
		if (!IsBuiltIn(filePath)) return null;
		
		var filePathAfterBuiltInPrefix = filePath[LocalBuiltInTexturePrefix.Length..];

		if (filePathAfterBuiltInPrefix.StartsWith(MapTexelPrefix)) {
			return filePathAfterBuiltInPrefix[MapTexelPrefix.Length..] switch {
				"color" => (CreateColorTexel(DefaultColor).ToRgb24(), null),
				"normals" => (CreateNormalTexel(DefaultNormalOffset), null),
				"orm" => (CreateOcclusionRoughnessMetallicTexel(DefaultOcclusion, DefaultRoughness, DefaultMetallic), null),
				"ormr" => (null, CreateOcclusionRoughnessMetallicReflectanceTexel(DefaultOcclusion, DefaultRoughness, DefaultMetallic, DefaultReflectance)),
				"at" => (null, CreateAbsorptionTransmissionTexel(DefaultAbsorption, DefaultTransmission)),
				"emissive" => (null, CreateEmissiveTexel(DefaultEmissiveColor, DefaultEmissiveIntensity)),
				"anisotropy" => (CreateAnisotropyTexel(DefaultAnisotropyTangent, DefaultAnisotropyStrength), null),
				"clearcoat" => (CreateClearCoatTexel(DefaultClearCoatThickness, DefaultClearCoatRoughness), null),
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