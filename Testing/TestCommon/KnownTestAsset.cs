// Created on 2025-06-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Testing;

public enum KnownTestAsset {
	EgodystonicLogo,
	CrateMesh,
	CrateAlbedoTex,
	CrateNormalTex,
	CrateSpecularTex,
	CloudsHdr,
	BrickAlbedoTex,
	BrickNormalTex,
	BrickOrmTex,
	WhiteTex,
	SwatchTex,
	SwatchAlphaTex,
	MetroIblKtx,
	MetroSkyKtx,
}

public static class KnownTestAssetExtensions {
	public static string GetFilename(this KnownTestAsset @this) {
		return @this switch {
			KnownTestAsset.EgodystonicLogo => "egdLogo.png",
			KnownTestAsset.CrateMesh => "ELCrate.obj",
			KnownTestAsset.CrateAlbedoTex => "ELCrate.png",
			KnownTestAsset.CrateNormalTex => "ELCrate_Normal.png",
			KnownTestAsset.CrateSpecularTex => "ELCrate_Specular.png",
			KnownTestAsset.CloudsHdr => "kloofendal_48d_partly_cloudy_puresky_4k.hdr",
			KnownTestAsset.BrickAlbedoTex => "brickwalltextures/brick_wall_001_diffuse_2k.jpg",
			KnownTestAsset.BrickNormalTex => "brickwalltextures/brick_wall_001_nor_gl_2k.jpg",
			KnownTestAsset.BrickOrmTex => "brickwalltextures/brick_wall_001_arm_2k.jpg",
			KnownTestAsset.WhiteTex => "white.bmp",
			KnownTestAsset.SwatchTex => "swatch.bmp",
			KnownTestAsset.SwatchAlphaTex => "swatch_alpha.png",
			KnownTestAsset.MetroIblKtx => "metro_ibl.ktx",
			KnownTestAsset.MetroSkyKtx => "metro_skybox.ktx",
			_ => throw new ArgumentOutOfRangeException(nameof(@this), @this, "Unknown test asset.")
		};
	}
}