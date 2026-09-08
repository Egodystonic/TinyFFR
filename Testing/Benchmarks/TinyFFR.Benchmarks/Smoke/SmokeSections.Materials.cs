// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	public static void StandardMaterials() {
		using var colorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Standard Color Map");
		using var normalMap = Factory.TextureBuilder.CreateNormalMap(BenchmarkAssets.NormalPattern, "Benchmark Standard Normal Map");
		using var ormMap = Factory.TextureBuilder.CreateOcclusionRoughnessMetallicMap(
			BenchmarkAssets.OcclusionPattern, BenchmarkAssets.RoughnessPattern, BenchmarkAssets.MetallicPattern, "Benchmark Standard ORM Map"
		);
		using var anisotropyMap = Factory.TextureBuilder.CreateAnisotropyMap(BenchmarkAssets.AnisotropyAnglePattern, BenchmarkAssets.AnisotropyStrengthPattern, "Benchmark Standard Anisotropy Map");
		using var emissiveMap = Factory.TextureBuilder.CreateEmissiveMap(BenchmarkAssets.ColorPattern, BenchmarkAssets.EmissiveIntensityPattern, "Benchmark Standard Emissive Map");
		using var clearCoatMap = Factory.TextureBuilder.CreateClearCoatMap(BenchmarkAssets.ThicknessPattern, BenchmarkAssets.RoughnessPattern, "Benchmark Standard Clear Coat Map");

		using var colorOnly = Factory.MaterialBuilder.CreateStandardMaterial(colorMap, name: "Benchmark Color Only Material");
		using var colorAndNormal = Factory.MaterialBuilder.CreateStandardMaterial(colorMap, normalMap, name: "Benchmark Color Normal Material");
		using var full = Factory.MaterialBuilder.CreateStandardMaterial(
			colorMap, normalMap, ormMap, anisotropyMap, emissiveMap, clearCoatMap,
			name: "Benchmark Full Standard Material"
		);
		using var effectsEnabled = Factory.MaterialBuilder.CreateStandardMaterial(
			colorMap, normalMap, ormMap,
			enablePerInstanceEffects: true,
			name: "Benchmark Per Instance Effects Material"
		);
	}

	public static void SpecialMaterials() {
		for (var repeat = 0; repeat < SmokeWorkload.SpecialMaterialRepeatCount; ++repeat) CreateOneSpecialMaterialSet();
	}

	static void CreateOneSpecialMaterialSet() {
		using var colorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Special Color Map");
		using var alphaColorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.AlphaColorPattern, includeAlpha: true, "Benchmark Special Alpha Color Map");
		using var absorptionMap = Factory.TextureBuilder.CreateAbsorptionTransmissionMap(BenchmarkAssets.ColorPattern, BenchmarkAssets.TransmissionPattern, "Benchmark Special Absorption Map");

		using var lightingIgnoring = Factory.MaterialBuilder.CreateLightingIgnoringMaterial(colorMap, name: "Benchmark Lighting Ignoring Material");
		using var colorKeyed = Factory.MaterialBuilder.CreateColorKeyedMaterial(alphaColorMap, name: "Benchmark Color Keyed Material");
		using var transmissive = Factory.MaterialBuilder.CreateTransmissiveMaterial(colorMap, absorptionMap, name: "Benchmark Transmissive Material");
		using var testMaterial = Factory.MaterialBuilder.CreateTestMaterial();
		using var testMaterialUnlit = Factory.MaterialBuilder.CreateTestMaterial(ignoresLighting: true);

		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Default Material Mesh");
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, name: "Benchmark Default Material Instance");
		instance.SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle.Plain3D);
		instance.SetDefaultMaterialBaseColor(StandardColor.Yellow);
		instance.SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle.Wireframe);
		instance.SetMaterial(testMaterial);
	}

	public static void MaterialEffects() {
		for (var repeat = 0; repeat < SmokeWorkload.MaterialEffectRepeatCount; ++repeat) ApplyOneMaterialEffectSet();
	}

	static void ApplyOneMaterialEffectSet() {
		using var colorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Effects Color Map");
		using var blendMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.AlphaColorPattern, includeAlpha: true, "Benchmark Effects Blend Map");
		using var material = Factory.MaterialBuilder.CreateStandardMaterial(colorMap, enablePerInstanceEffects: true, name: "Benchmark Effects Material");
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Effects Mesh");
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Benchmark Effects Instance");

		if (instance.MaterialEffects is { } effects) {
			effects.SetTransform(new Transform2D(scaling: (2f, 2f)));
			effects.SetBlendTexture(MaterialEffectMapType.Color, blendMap);
			effects.SetBlendDistance(MaterialEffectMapType.Color, 0.5f);
		}
	}
}
