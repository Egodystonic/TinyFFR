// Created on 2025-06-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Testing;

public enum KnownTestAsset {
	EgodystonicLogo,
	CrateMesh,
	CrateAlbedoTex,
	CrateNormalTex,
	CrateSpecularTex,
	CloudsHdr
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
			_ => throw new ArgumentOutOfRangeException(nameof(@this), @this, "Unknown test asset.")
		};
	}
}