// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Egodystonic.TinyFFR.Testing.ModelViewer;

public sealed class ModelListEntry : INotifyPropertyChanged {
	bool _isLoaded;
	bool _loadFailed;

	public event PropertyChangedEventHandler? PropertyChanged;

	public string DisplayName { get; }
	public string? FileName { get; }

	public bool IsHeader => FileName is null;
	public bool IsSelectable => FileName is not null && IsLoaded;

	public bool IsLoaded {
		get => _isLoaded;
		set {
			if (_isLoaded == value) return;
			_isLoaded = value;
			Raise();
			Raise(nameof(IsSelectable));
			Raise(nameof(DisplayText));
		}
	}

	public bool LoadFailed {
		get => _loadFailed;
		set {
			if (_loadFailed == value) return;
			_loadFailed = value;
			Raise();
			Raise(nameof(DisplayText));
		}
	}

	public string DisplayText {
		get {
			if (IsHeader || IsLoaded) return DisplayName;
			return LoadFailed ? DisplayName + "   (failed)" : DisplayName + "   (loading...)";
		}
	}

	public ModelListEntry(string displayName, string? fileName) {
		DisplayName = displayName;
		FileName = fileName;
	}

	void Raise([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class ModelCatalog {
	public static ModelListEntry[] Build() {
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

		return result.ToArray();
	}
}
