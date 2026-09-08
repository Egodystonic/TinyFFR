// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static class BenchmarkAssets {
	public static string CrateMesh { get; private set; } = null!;
	public static string CrateAlbedoTex { get; private set; } = null!;
	public static string CrateNormalTex { get; private set; } = null!;
	public static string CrateSpecularTex { get; private set; } = null!;
	public static string BrickAlbedoTex { get; private set; } = null!;
	public static string BrickNormalTex { get; private set; } = null!;
	public static string BrickOrmTex { get; private set; } = null!;
	public static string WhiteTex { get; private set; } = null!;
	public static string SwatchTex { get; private set; } = null!;
	public static string SwatchAlphaTex { get; private set; } = null!;
	public static string MetroSkyKtx { get; private set; } = null!;
	public static string MetroIblKtx { get; private set; } = null!;
	public static string SansFont { get; private set; } = null!;
	public static string BoxModel { get; private set; } = null!;
	public static string RiggedModel { get; private set; } = null!;
	public static string BakeDirectory { get; private set; } = null!;
	public static string BakedMeshFile { get; private set; } = null!;
	public static string BakedTextureFile { get; private set; } = null!;
	public static string BakedMaterialFile { get; private set; } = null!;

	public static TexturePattern<ColorVect> ColorPattern { get; private set; }
	public static TexturePattern<ColorVect> AlphaColorPattern { get; private set; }
	public static TexturePattern<SphericalTranslation> NormalPattern { get; private set; }
	public static TexturePattern<Real> OcclusionPattern { get; private set; }
	public static TexturePattern<Real> RoughnessPattern { get; private set; }
	public static TexturePattern<Real> MetallicPattern { get; private set; }
	public static TexturePattern<Real> ReflectancePattern { get; private set; }
	public static TexturePattern<Real> TransmissionPattern { get; private set; }
	public static TexturePattern<Real> ThicknessPattern { get; private set; }
	public static TexturePattern<Angle> AnisotropyAnglePattern { get; private set; }
	public static TexturePattern<Real> AnisotropyStrengthPattern { get; private set; }
	public static TexturePattern<Real> EmissiveIntensityPattern { get; private set; }

	public static void Initialize() {
		CrateMesh = CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh);
		CrateAlbedoTex = CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex);
		CrateNormalTex = CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex);
		CrateSpecularTex = CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex);
		BrickAlbedoTex = CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex);
		BrickNormalTex = CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex);
		BrickOrmTex = CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex);
		WhiteTex = CommonTestAssets.FindAsset(KnownTestAsset.WhiteTex);
		SwatchTex = CommonTestAssets.FindAsset(KnownTestAsset.SwatchTex);
		SwatchAlphaTex = CommonTestAssets.FindAsset(KnownTestAsset.SwatchAlphaTex);
		MetroSkyKtx = CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx);
		MetroIblKtx = CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx);
		SansFont = CommonTestAssets.FindAsset("DejaVuSans.ttf");
		BoxModel = CommonTestAssets.FindAsset("models/BoxTextured.glb");
		RiggedModel = CommonTestAssets.FindAsset("models/RiggedSimple.glb");

		BakeDirectory = Path.Combine(Path.GetTempPath(), "tinyffr_benchmark_bakery");
		Directory.CreateDirectory(BakeDirectory);
		BakedMeshFile = Path.Combine(BakeDirectory, "benchmark_mesh.tffr");
		BakedTextureFile = Path.Combine(BakeDirectory, "benchmark_texture.tffr");
		BakedMaterialFile = Path.Combine(BakeDirectory, "benchmark_material.tffr");

		ColorPattern = TexturePattern.ChequerboardBordered(
			new ColorVect(1f, 1f, 1f), 2,
			new ColorVect(1f, 0f, 0f), new ColorVect(0f, 1f, 0f), new ColorVect(0f, 0f, 1f), new ColorVect(0.5f, 0.5f, 0.5f),
			(4, 4)
		);
		AlphaColorPattern = TexturePattern.Chequerboard(new ColorVect(1f, 1f, 1f, 1f, true), new ColorVect(0f, 0f, 1f, 0.4f, true), (8, 8));
		NormalPattern = TexturePattern.Circles(
			SphericalTranslation.ZeroZero,
			new SphericalTranslation(0f, 45f),
			new SphericalTranslation(90f, 45f),
			new SphericalTranslation(180f, 45f),
			new SphericalTranslation(270f, 45f),
			SphericalTranslation.ZeroZero
		);
		OcclusionPattern = TexturePattern.Chequerboard<Real>(0.5f, 1f, 0.8f, (27, 27));
		RoughnessPattern = TexturePattern.Chequerboard<Real>(0.8f, 0.4f, 1f, (27, 27));
		MetallicPattern = TexturePattern.Chequerboard<Real>(0.4f, 0f, (27, 27));
		ReflectancePattern = TexturePattern.Chequerboard<Real>(0.5f, 0.9f, (9, 9));
		TransmissionPattern = TexturePattern.Chequerboard<Real>(0.9f, 0.6f, (6, 6));
		ThicknessPattern = TexturePattern.Chequerboard<Real>(0.3f, 0.7f, (6, 6));
		AnisotropyAnglePattern = TexturePattern.Chequerboard(Angle.Zero, Angle.QuarterCircle, (12, 12));
		AnisotropyStrengthPattern = TexturePattern.Chequerboard<Real>(0.2f, 0.8f, (12, 12));
		EmissiveIntensityPattern = TexturePattern.Chequerboard<Real>(0.1f, 0.6f, (16, 16));
	}
}
