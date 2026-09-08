// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Benchmarks.Harness.AllocationProbe;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static class SmokeProbes {
	const int LoadCount = 200;
	const int CreateCount = 500;
	const int EnumerationPassCount = 200;
	const int MetadataCount = 2_000;
	const int ModelLoadCount = 100;
	const int BakeCount = 50;
	const string ProbeMeshName = "Probe Named Mesh";

	static ILocalTinyFfrFactory Factory => BenchmarkEnvironment.Factory;
	static IResourceAllocator Allocator => BenchmarkEnvironment.Allocator;

	public static IReadOnlyList<ProbeGroup> All { get; } = [
		new("LoadBakedAssets", ProbeLoadBakedAssets),
		new("Quads", ProbeQuads),
		new("BuiltInTextures", ProbeBuiltInTextures),
		new("ResourceGroups", ProbeResourceGroups),
		new("LoadCombinedTextures", ProbeLoadCombinedTextures),
		new("Lights", ProbeLights),
		new("ResourceNamingAndDirectory", ProbeResourceNamingAndDirectory),
		new("LoadModelFile", ProbeLoadModelFile),
		new("BakeAssets", ProbeBakeAssets),
		new("LiveInstances", ProbeLiveInstances)
	];

	const int LiveInstanceCount = 2_000;

	static void ProbeLiveInstances() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Live Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();

		Measure($"hold {LiveInstanceCount} live instances, no scene", () => {
			var instances = Allocator.GetSharedScratchList<ModelInstance>();
			for (var i = 0; i < LiveInstanceCount; ++i) {
				instances.Add(Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Probe Live Instance"));
			}
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}, LiveInstanceCount);
		Measure($"hold {LiveInstanceCount} live instances, no name", () => {
			var instances = Allocator.GetSharedScratchList<ModelInstance>();
			for (var i = 0; i < LiveInstanceCount; ++i) {
				instances.Add(Factory.ObjectBuilder.CreateModelInstance(mesh, material));
			}
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}, LiveInstanceCount);
		using var persistentScene = Factory.SceneBuilder.CreateScene(name: "Probe Persistent Scene");
		Measure($"hold {LiveInstanceCount} live instances in a PERSISTENT scene", () => {
			var instances = Allocator.GetSharedScratchList<ModelInstance>();
			for (var i = 0; i < LiveInstanceCount; ++i) {
				var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Probe Live Instance");
				persistentScene.Add(instance);
				instances.Add(instance);
			}
			persistentScene.RemoveAll();
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}, LiveInstanceCount);
		var persistent = new ModelInstance[LiveInstanceCount];
		for (var i = 0; i < LiveInstanceCount; ++i) persistent[i] = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Probe Persistent Instance");
		Measure($"scene.Add + RemoveAll for {LiveInstanceCount} pre-existing instances", () => {
			for (var i = 0; i < LiveInstanceCount; ++i) persistentScene.Add(persistent[i]);
			persistentScene.RemoveAll();
		}, LiveInstanceCount);

		for (var i = 0; i < LiveInstanceCount; ++i) persistent[i].Dispose();

		Measure($"hold {LiveInstanceCount} live instances (create+scene.Add, then dispose)", () => {
			using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Live Scene");
			var instances = Allocator.GetSharedScratchList<ModelInstance>();
			for (var i = 0; i < LiveInstanceCount; ++i) {
				var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Probe Live Instance");
				scene.Add(instance);
				instances.Add(instance);
			}
			scene.RemoveAll();
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}, LiveInstanceCount);
	}

	static void ProbeLoadBakedAssets() {
		EnsureBakedAssetsExist();

		Measure("baseline (empty)", static () => { });
		Measure($"LoadBakedMesh x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) Factory.AssetLoader.LoadBakedMesh(BenchmarkAssets.BakedMeshFile).Dispose();
		}, LoadCount);
		var longDir = Path.Combine(BenchmarkAssets.BakeDirectory, new string('d', 120));
		Directory.CreateDirectory(longDir);
		var longPath = Path.Combine(longDir, "benchmark_mesh.tffr");
		File.Copy(BenchmarkAssets.BakedMeshFile, longPath, overwrite: true);
		Console.WriteLine($"        (short path {BenchmarkAssets.BakedMeshFile.Length} chars, long path {longPath.Length} chars)");
		Measure($"LoadBakedMesh from {longPath.Length}-char path x{LoadCount}", () => {
			for (var i = 0; i < LoadCount; ++i) Factory.AssetLoader.LoadBakedMesh(longPath).Dispose();
		}, LoadCount);

		Measure($"LoadBakedTexture x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) Factory.AssetLoader.LoadBakedTexture(BenchmarkAssets.BakedTextureFile).Dispose();
		}, LoadCount);
		Measure($"LoadBakedMaterial x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) Factory.AssetLoader.LoadBakedMaterial(BenchmarkAssets.BakedMaterialFile).Dispose();
		}, LoadCount);
	}

	static void ProbeBakeAssets() {
		Measure($"Bake mesh+texture+material x{BakeCount}", static () => {
			for (var i = 0; i < BakeCount; ++i) BakeOneSet();
		}, BakeCount);
		Measure($"create mesh+texture+material, no bake, x{BakeCount} (control)", static () => {
			for (var i = 0; i < BakeCount; ++i) CreateOneBakeableSet();
		}, BakeCount);
		Measure($"[bakery OFF] create mesh+texture+material x{BakeCount}", static () => {
			Factory.AssetBakery.Enabled = false;
			for (var i = 0; i < BakeCount; ++i) CreateOneBakeableSet();
		}, BakeCount);
		Measure($"[bakery ON] create mesh+texture+material x{BakeCount}", static () => {
			Factory.AssetBakery.Enabled = true;
			for (var i = 0; i < BakeCount; ++i) CreateOneBakeableSet();
			Factory.AssetBakery.ClearBakeryMemory();
		}, BakeCount);
		Factory.AssetBakery.Enabled = false;
		Measure($"CreateColorMap + Dispose x{BakeCount}", static () => {
			for (var i = 0; i < BakeCount; ++i) {
				Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Probe Bake Texture").Dispose();
			}
		}, BakeCount);
		using var probeTexture = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Probe Material Texture");
		Measure($"CreateStandardMaterial + Dispose x{BakeCount}", () => {
			for (var i = 0; i < BakeCount; ++i) Factory.MaterialBuilder.CreateStandardMaterial(probeTexture, name: "Probe Bake Material").Dispose();
		}, BakeCount);
		Measure($"CreateTestMaterial + Dispose x{BakeCount}", static () => {
			for (var i = 0; i < BakeCount; ++i) Factory.MaterialBuilder.CreateTestMaterial().Dispose();
		}, BakeCount);
	}

	static void ProbeQuads() {
		using var quadMesh = Factory.MeshBuilder.CreateQuadMesh(name: "Probe Quad Mesh");
		using var cuboidMesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Cuboid Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Quad Scene");

		Measure($"CreateModelInstance + Dispose x{CreateCount} (control)", () => {
			for (var i = 0; i < CreateCount; ++i) Factory.ObjectBuilder.CreateModelInstance(cuboidMesh, material).Dispose();
		}, CreateCount);
		Measure($"CreateQuadInstance + Dispose x{CreateCount}", () => {
			for (var i = 0; i < CreateCount; ++i) {
				Factory.ObjectBuilder.CreateQuadInstance(quadMesh, material, Location.Origin, (0.5f, 0.5f), Direction.Backward, Direction.Up).Dispose();
			}
		}, CreateCount);
		Measure($"CreateCameraLockedQuadInstance + Dispose x{CreateCount}", () => {
			for (var i = 0; i < CreateCount; ++i) {
				Factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, Location.Origin, (0.25f, 0.25f)).Dispose();
			}
		}, CreateCount);

		var quad = Factory.ObjectBuilder.CreateQuadInstance(quadMesh, material, Location.Origin, (0.5f, 0.5f), Direction.Backward, Direction.Up);
		try {
			Measure($"scene Add/Remove quad x{CreateCount}", () => {
				for (var i = 0; i < CreateCount; ++i) {
					scene.Add(quad);
					scene.Remove(quad);
				}
			}, CreateCount);
		}
		finally {
			quad.Dispose();
		}
	}

	static void ProbeBuiltInTextures() {
		Measure($"LoadTexture(built-in color map) x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) {
				Factory.AssetLoader.LoadTexture(Factory.AssetLoader.BuiltInTexturePaths.DefaultColorMap, TextureDataType.ColorSrgb).Dispose();
			}
		}, LoadCount);
		Measure($"LoadTexture(file on disk) x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) Factory.AssetLoader.LoadTexture(BenchmarkAssets.SwatchTex, TextureDataType.ColorSrgb).Dispose();
		}, LoadCount);
		Measure($"LoadTexture(built-in normal map) x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) {
				Factory.AssetLoader.LoadTexture(Factory.AssetLoader.BuiltInTexturePaths.DefaultNormalMap, TextureDataType.LinearDataUnitVector).Dispose();
			}
		}, LoadCount);
		Measure($"ReadTextureMetadata x{MetadataCount}", static () => {
			for (var i = 0; i < MetadataCount; ++i) _ = Factory.AssetLoader.ReadTextureMetadata(Factory.AssetLoader.BuiltInTexturePaths.DefaultColorMap);
		}, MetadataCount);
		Measure($"BuiltInTexturePaths getter x{MetadataCount}", static () => {
			for (var i = 0; i < MetadataCount; ++i) _ = Factory.AssetLoader.BuiltInTexturePaths;
		}, MetadataCount);
	}

	static void ProbeLoadCombinedTextures() {
		Measure($"LoadCombinedTexture x{LoadCount}", static () => {
			for (var i = 0; i < LoadCount; ++i) LoadOneCombinedTexture();
		}, LoadCount);
	}

	static void ProbeLoadModelFile() {
		Measure($"LoadAll + Dispose x{ModelLoadCount}", static () => {
			for (var i = 0; i < ModelLoadCount; ++i) Factory.AssetLoader.LoadAll(BenchmarkAssets.BoxModel).Dispose();
		}, ModelLoadCount);

		using var group = Factory.AssetLoader.LoadAll(BenchmarkAssets.BoxModel, "Probe Model");
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Model Scene");

		Measure($"CreateModelInstances + Dispose x{ModelLoadCount}", () => {
			for (var i = 0; i < ModelLoadCount; ++i) Factory.ObjectBuilder.CreateModelInstances(group.Models).Dispose();
		}, ModelLoadCount);

		using var instances = Factory.ObjectBuilder.CreateModelInstances(group.Models, name: "Probe Model Instances");
		Measure($"scene Add(instances) + RemoveAll x{ModelLoadCount}", () => {
			for (var i = 0; i < ModelLoadCount; ++i) {
				scene.Add(instances);
				scene.RemoveAll();
			}
		}, ModelLoadCount);
	}

	static void ProbeResourceGroups() {
		using var material = Factory.MaterialBuilder.CreateTestMaterial();

		Measure($"CreateMesh(UnitCube) + Dispose x{CreateCount}", static () => {
			for (var i = 0; i < CreateCount; ++i) Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Grouped Mesh").Dispose();
		}, CreateCount);

		using var group = Allocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, "Probe Resource Group", CreateCount * 2);
		var meshes = Allocator.GetSharedScratchList<Mesh>();
		for (var i = 0; i < CreateCount; ++i) {
			var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Grouped Mesh");
			var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Probe Grouped Instance");
			group.Add(mesh);
			group.Add(instance);
			meshes.Add(mesh);
		}

		Measure($"group.ResourceCount x{EnumerationPassCount}", () => {
			for (var i = 0; i < EnumerationPassCount; ++i) _ = group.ResourceCount;
		}, EnumerationPassCount);
		Measure($"group.Meshes property x{EnumerationPassCount}", () => {
			for (var i = 0; i < EnumerationPassCount; ++i) _ = group.Meshes;
		}, EnumerationPassCount);
		Measure($"foreach over group.Meshes x{EnumerationPassCount}", () => {
			for (var i = 0; i < EnumerationPassCount; ++i) {
				foreach (var mesh in group.Meshes) {
					if (mesh == default) continue;
				}
			}
		}, EnumerationPassCount);
		Measure($"foreach over group.ModelInstances x{EnumerationPassCount}", () => {
			for (var i = 0; i < EnumerationPassCount; ++i) {
				foreach (var instance in group.ModelInstances) {
					if (instance == default) continue;
				}
			}
		}, EnumerationPassCount);
	}

	static void ProbeLights() {
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Light Scene");

		Measure($"CreatePointLight + Dispose x{CreateCount}", static () => {
			for (var i = 0; i < CreateCount; ++i) Factory.LightBuilder.CreatePointLight(Location.Origin, StandardColor.Red).Dispose();
		}, CreateCount);
		Measure($"CreateSpotLight + Dispose x{CreateCount}", static () => {
			for (var i = 0; i < CreateCount; ++i) Factory.LightBuilder.CreateSpotLight(Location.Origin, Direction.Forward).Dispose();
		}, CreateCount);
		Measure($"CreateDirectionalLight + Dispose x{CreateCount}", static () => {
			for (var i = 0; i < CreateCount; ++i) Factory.LightBuilder.CreateDirectionalLight(Direction.Down).Dispose();
		}, CreateCount);

		var light = Factory.LightBuilder.CreatePointLight(Location.Origin, StandardColor.Red, name: "Probe Point Light");
		try {
			Measure($"scene Add/Remove light x{CreateCount}", () => {
				for (var i = 0; i < CreateCount; ++i) {
					scene.Add(light);
					scene.Remove(light);
				}
			}, CreateCount);

			scene.Add(light);
			Measure($"light.Color set x{CreateCount}", () => {
				for (var i = 0; i < CreateCount; ++i) light.Color = light.Color.WithHueAdjustedBy(1f);
			}, CreateCount);
			Measure($"light.Position set x{CreateCount}", () => {
				for (var i = 0; i < CreateCount; ++i) light.Position = Location.Origin;
			}, CreateCount);
			Measure($"foreach over scene.ContainedLights x{EnumerationPassCount}", () => {
				for (var i = 0; i < EnumerationPassCount; ++i) {
					foreach (var containedLight in scene.ContainedLights) {
						if (containedLight == default) continue;
					}
				}
			}, EnumerationPassCount);
			scene.Remove(light);
		}
		finally {
			light.Dispose();
		}
	}

	static void ProbeResourceNamingAndDirectory() {
		Measure($"CreateMesh(named) + Dispose x{CreateCount}", static () => {
			for (var i = 0; i < CreateCount; ++i) Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: ProbeMeshName).Dispose();
		}, CreateCount);

		var meshes = Allocator.GetSharedScratchList<Mesh>();
		for (var i = 0; i < CreateCount; ++i) meshes.Add(Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: ProbeMeshName));

		try {
			Measure($"mesh.GetNameLength x{CreateCount}", () => {
				for (var i = 0; i < meshes.Count; ++i) _ = meshes[i].GetNameLength();
			}, CreateCount);
			Measure($"mesh.CopyName x{CreateCount}", () => {
				Span<char> nameBuffer = stackalloc char[ProbeMeshName.Length];
				for (var i = 0; i < meshes.Count; ++i) meshes[i].CopyName(nameBuffer);
			}, CreateCount);
			Measure($"ResourceDirectory.FindByName<Mesh> x{EnumerationPassCount}", static () => {
				for (var i = 0; i < EnumerationPassCount; ++i) _ = Factory.ResourceDirectory.FindByName<Mesh>(ProbeMeshName);
			}, EnumerationPassCount);
			Measure($"GetAllActiveInstances<Mesh> property x{EnumerationPassCount}", static () => {
				for (var i = 0; i < EnumerationPassCount; ++i) _ = Factory.ResourceDirectory.GetAllActiveInstances<Mesh>();
			}, EnumerationPassCount);
			Measure($"foreach over GetAllActiveInstances<Mesh> x{EnumerationPassCount}", static () => {
				for (var i = 0; i < EnumerationPassCount; ++i) {
					foreach (var mesh in Factory.ResourceDirectory.GetAllActiveInstances<Mesh>()) {
						if (mesh == default) continue;
					}
				}
			}, EnumerationPassCount);
		}
		finally {
			for (var i = 0; i < meshes.Count; ++i) meshes[i].Dispose();
		}
	}

	static void LoadOneCombinedTexture() {
		Factory.AssetLoader.LoadCombinedTexture(
			BenchmarkAssets.SwatchTex,
			BenchmarkAssets.WhiteTex,
			new TextureCombinationConfig(
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
			),
			new TextureCreationConfig { DataType = TextureDataType.ColorSrgb }
		).Dispose();
	}

	static void BakeOneSet() {
		Factory.AssetBakery.Enabled = true;

		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Bake Mesh");
		using var texture = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Probe Bake Texture");
		using var material = Factory.MaterialBuilder.CreateStandardMaterial(texture, name: "Probe Bake Material");

		Factory.AssetBakery.Bake(mesh, BenchmarkAssets.BakedMeshFile);
		Factory.AssetBakery.Bake(texture, BenchmarkAssets.BakedTextureFile);
		Factory.AssetBakery.Bake(material, BenchmarkAssets.BakedMaterialFile);
		Factory.AssetBakery.ClearBakeryMemory();
	}

	static void CreateOneBakeableSet() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Probe Bake Mesh");
		using var texture = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Probe Bake Texture");
		using var material = Factory.MaterialBuilder.CreateStandardMaterial(texture, name: "Probe Bake Material");
	}

	static void EnsureBakedAssetsExist() {
		if (File.Exists(BenchmarkAssets.BakedMaterialFile)) return;
		BakeOneSet();
	}
}
