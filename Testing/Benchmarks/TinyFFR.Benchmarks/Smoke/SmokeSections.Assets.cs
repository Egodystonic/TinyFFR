// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	static readonly TimeSpan AsyncTimeout = TimeSpan.FromSeconds(30d);

	public static void LoadMeshFile() {
		for (var repeat = 0; repeat < SmokeWorkload.MeshFileLoadCount; ++repeat) {
			using var mesh = Factory.AssetLoader.LoadMesh(BenchmarkAssets.CrateMesh, "Benchmark Loaded Mesh");
			_ = Factory.AssetLoader.ReadMeshMetadata(BenchmarkAssets.CrateMesh);
		}
	}

	public static void LoadModelFile() {
		for (var repeat = 0; repeat < SmokeWorkload.ModelFileLoadCount; ++repeat) {
			using var group = Factory.AssetLoader.LoadAll(BenchmarkAssets.BoxModel, "Benchmark Loaded Model");
			using var instances = Factory.ObjectBuilder.CreateModelInstances(group.Models, name: "Benchmark Loaded Model Instances");
			using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Loaded Model Scene");
			scene.Add(instances);
			scene.RemoveAll();
		}
	}

	public static void SkeletalAnimation() {
		using var group = Factory.AssetLoader.LoadAll(BenchmarkAssets.RiggedModel, "Benchmark Rigged Model");
		using var instances = Factory.ObjectBuilder.CreateModelInstances(group.Models, name: "Benchmark Rigged Model Instances");

		foreach (var instance in instances.Instances) {
			var animations = instance.Animations;
			if (animations.Count == 0) continue;

			var player = instance.GetAnimationPlayer(animations[0]);
			_ = player.DurationSeconds;
			for (var sample = 0; sample < SmokeWorkload.AnimationSampleCount; ++sample) {
				player.SetCompletionFraction(sample / (float) SmokeWorkload.AnimationSampleCount);
				player.SetTimePoint(sample * 0.001f, AnimationWrapStyle.Loop);
			}
			break;
		}
	}

	public static void AsyncLoading() {
		for (var repeat = 0; repeat < SmokeWorkload.AsyncLoadRepeatCount; ++repeat) LoadOneAssetPairAsync();
	}

	static void LoadOneAssetPairAsync() {
		var textureOp = Factory.AssetLoader.LoadTextureAsync(BenchmarkAssets.CrateAlbedoTex, TextureDataType.ColorSrgb, "Benchmark Async Texture");
		var meshOp = Factory.AssetLoader.LoadMeshAsync(BenchmarkAssets.CrateMesh, "Benchmark Async Mesh");

		if (!textureOp.WaitForCompletion(AsyncTimeout)) throw new TimeoutException("Async texture load timed out.");
		if (!meshOp.WaitForCompletion(AsyncTimeout)) throw new TimeoutException("Async mesh load timed out.");

		using var texture = textureOp.GetResultAndDisposeOperation();
		using var mesh = meshOp.GetResultAndDisposeOperation();
	}

	public static void BakeAssets() {
		for (var repeat = 0; repeat < SmokeWorkload.BakeRepeatCount; ++repeat) BakeOneAssetSet();
	}

	static void BakeOneAssetSet() {
		Factory.AssetBakery.Enabled = true;

		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Bake Mesh");
		using var texture = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Bake Texture");
		using var material = Factory.MaterialBuilder.CreateStandardMaterial(texture, name: "Benchmark Bake Material");

		Factory.AssetBakery.Bake(mesh, BenchmarkAssets.BakedMeshFile);
		Factory.AssetBakery.Bake(texture, BenchmarkAssets.BakedTextureFile);
		Factory.AssetBakery.Bake(material, BenchmarkAssets.BakedMaterialFile);
		Factory.AssetBakery.ClearBakeryMemory();
	}

	public static void LoadBakedAssets() {
		EnsureBakedAssetsExist();
		for (var repeat = 0; repeat < SmokeWorkload.BakedLoadRepeatCount; ++repeat) LoadOneBakedAssetSet();
	}

	static void LoadOneBakedAssetSet() {
		using var mesh = Factory.AssetLoader.LoadBakedMesh(BenchmarkAssets.BakedMeshFile, "Benchmark Baked Mesh");
		using var texture = Factory.AssetLoader.LoadBakedTexture(BenchmarkAssets.BakedTextureFile, "Benchmark Baked Texture");
		using var material = Factory.AssetLoader.LoadBakedMaterial(BenchmarkAssets.BakedMaterialFile, "Benchmark Baked Material");
	}

	static void EnsureBakedAssetsExist() {
		if (File.Exists(BenchmarkAssets.BakedMaterialFile)) return;
		BakeOneAssetSet();
	}
}
