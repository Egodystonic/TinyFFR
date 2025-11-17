// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalBuiltInTexturePathLibrary : IBuiltInTexturePathLibrary {
	public const string LocalBuiltInTexturePrefix = "?tffr_builtin?";

	public ReadOnlySpan<char> Gray100Percent => LocalBuiltInTexturePrefix + "gray_100";
	public ReadOnlySpan<char> Gray90Percent => LocalBuiltInTexturePrefix + "gray_90";
	public ReadOnlySpan<char> Gray80Percent => LocalBuiltInTexturePrefix + "gray_80";
	public ReadOnlySpan<char> Gray70Percent => LocalBuiltInTexturePrefix + "gray_70";
	public ReadOnlySpan<char> Gray60Percent => LocalBuiltInTexturePrefix + "gray_60";
	public ReadOnlySpan<char> Gray50Percent => LocalBuiltInTexturePrefix + "gray_50";
	public ReadOnlySpan<char> Gray40Percent => LocalBuiltInTexturePrefix + "gray_40";
	public ReadOnlySpan<char> Gray30Percent => LocalBuiltInTexturePrefix + "gray_30";
	public ReadOnlySpan<char> Gray20Percent => LocalBuiltInTexturePrefix + "gray_20";
	public ReadOnlySpan<char> Gray10Percent => LocalBuiltInTexturePrefix + "gray_10";
	public ReadOnlySpan<char> Gray0Percent => LocalBuiltInTexturePrefix + "gray_0";

	public bool IsBuiltIn(ReadOnlySpan<char> filePath) => filePath.StartsWith(LocalBuiltInTexturePrefix);
	
	public TexelRgba32? GetBuiltInTexel(ReadOnlySpan<char> filePath) {
		static TexelRgba32 AllBytesAs(byte b) => new(b, b, b, b);

		if (!IsBuiltIn(filePath)) return null;
		return filePath[LocalBuiltInTexturePrefix.Length..] switch {
			"gray_100" => AllBytesAs(Byte.MaxValue),
			"gray_90" => AllBytesAs((byte) (Byte.MaxValue * 0.9f)),
			"gray_80" => AllBytesAs((byte) (Byte.MaxValue * 0.8f)),
			"gray_70" => AllBytesAs((byte) (Byte.MaxValue * 0.7f)),
			"gray_60" => AllBytesAs((byte) (Byte.MaxValue * 0.6f)),
			"gray_50" => AllBytesAs((byte) (Byte.MaxValue * 0.5f)),
			"gray_40" => AllBytesAs((byte) (Byte.MaxValue * 0.4f)),
			"gray_30" => AllBytesAs((byte) (Byte.MaxValue * 0.3f)),
			"gray_20" => AllBytesAs((byte) (Byte.MaxValue * 0.2f)),
			"gray_10" => AllBytesAs((byte) (Byte.MaxValue * 0.1f)),
			"gray_0" => AllBytesAs(Byte.MinValue),
			_ => null,
		};
	}
}