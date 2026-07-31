// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Collections.Generic;

namespace TinyFFR.Tests.Integrations.Avalonia.Viewer;

public sealed record ModelListEntry(string DisplayName, string? FileName) {
	public bool IsHeader => FileName is null;
	public bool IsSelectable => FileName is not null;
}

public static class ModelCatalog {
	public static IReadOnlyList<ModelListEntry> Items { get; } = Build();

	static ModelListEntry[] Build() {
		var result = new List<ModelListEntry>();

		void AddCategory(string categoryName, params string[] fileNames) {
			result.Add(new ModelListEntry(categoryName, null));
			foreach (var fileName in fileNames) result.Add(new ModelListEntry(fileName, fileName));
		}

		AddCategory(
			"Color / texturing / import",
			"BoxTextured.gltf",
			"BoxTextured.glb",
			"BoxTexturedSelfContained.gltf",
			"BoxTexturedNonPowerOfTwo.glb",
			"Box With Spaces.gltf"
		);
		AddCategory(
			"Normals / tangents / node walk",
			"NormalTangentMirrorTest.glb",
			"NegativeScaleTest.glb",
			"TextureCoordinateTest.glb",
			"CompareNormal.glb"
		);
		AddCategory(
			"Occlusion / roughness / metallic",
			"CompareRoughness.glb",
			"CompareMetallic.glb",
			"MetalRoughSpheres.glb",
			"CompareAmbientOcclusion.glb"
		);
		AddCategory(
			"Anisotropy",
			"AnisotropyStrengthTest.glb",
			"AnisotropyDiscTest.glb"
		);
		AddCategory(
			"Emissive",
			"EmissiveStrengthTest.glb"
		);
		AddCategory(
			"Absorption / transmission",
			"TransmissionTest.glb",
			"CompareTransmission.glb",
			"TransmissionRoughnessTest.glb",
			"AttenuationTest.glb",
			"CompareIor.glb"
		);
		AddCategory(
			"Clear coat",
			"ClearCoatTest.glb"
		);
		AddCategory(
			"Showcase",
			"BarramundiFish.glb",
			"Avocado.glb",
			"DamagedHelmet.glb",
			"showcase_ABeautifulGame.glb",
			"showcase_GlassHurricaneCandleHolder.glb",
			"showcase_MaterialsVariantsShoe.glb",
			"showcase_MosquitoInAmber.glb",
			"showcase_PotOfCoals.glb",
			"showcase_ToyCar.glb",
			"showcase_AnisotropyBarnLamp.glb",
			"showcase_CarConcept.glb",
			"showcase_ChronographWatch.glb",
			"showcase_CommercialRefrigerator.glb"
		);
		AddCategory(
			"Stress test",
			"NodePerformanceTest.glb"
		);

		return result.ToArray();
	}
}
