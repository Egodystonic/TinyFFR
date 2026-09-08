// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using BenchmarkDotNet.Attributes;
using Egodystonic.TinyFFR.Benchmarks.Harness;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

public unsafe class SmokeBenchmark : TinyFfrBenchmark {

	[Benchmark] public void ProceduralMeshes() => TinyFfrThread.Invoke(&SmokeSections.ProceduralMeshes);
	[Benchmark] public void MeshVertexMutation() => TinyFfrThread.Invoke(&SmokeSections.MeshVertexMutation);
	[Benchmark] public void DynamicVertexBuffers() => TinyFfrThread.Invoke(&SmokeSections.DynamicVertexBuffers);
	[Benchmark] public void MutableGridMeshes() => TinyFfrThread.Invoke(&SmokeSections.MutableGridMeshes);

	[Benchmark] public void ProceduralTextures() => TinyFfrThread.Invoke(&SmokeSections.ProceduralTextures);
	[Benchmark] public void TextureMipmappingAndCompression() => TinyFfrThread.Invoke(&SmokeSections.TextureMipmappingAndCompression);
	[Benchmark] public void BuiltInTextures() => TinyFfrThread.Invoke(&SmokeSections.BuiltInTextures);
	[Benchmark] public void LoadTextureFiles() => TinyFfrThread.Invoke(&SmokeSections.LoadTextureFiles);
	[Benchmark] public void LoadCombinedTextures() => TinyFfrThread.Invoke(&SmokeSections.LoadCombinedTextures);

	[Benchmark] public void StandardMaterials() => TinyFfrThread.Invoke(&SmokeSections.StandardMaterials);
	[Benchmark] public void SpecialMaterials() => TinyFfrThread.Invoke(&SmokeSections.SpecialMaterials);
	[Benchmark] public void MaterialEffects() => TinyFfrThread.Invoke(&SmokeSections.MaterialEffects);

	[Benchmark] public void Lights() => TinyFfrThread.Invoke(&SmokeSections.Lights);
	[Benchmark] public void ShadowsAndFog() => TinyFfrThread.Invoke(&SmokeSections.ShadowsAndFog);
	[Benchmark] public void Backdrops() => TinyFfrThread.Invoke(&SmokeSections.Backdrops);
	[Benchmark] public void ModelInstances() => TinyFfrThread.Invoke(&SmokeSections.ModelInstances);
	[Benchmark] public void ScenePrimitives() => TinyFfrThread.Invoke(&SmokeSections.ScenePrimitives);
	[Benchmark] public void SceneQueries() => TinyFfrThread.Invoke(&SmokeSections.SceneQueries);
	[Benchmark] public void CameraAndControllers() => TinyFfrThread.Invoke(&SmokeSections.CameraAndControllers);

	[Benchmark] public void RenderFrame() => TinyFfrThread.Invoke(&SmokeSections.RenderFrame);
	[Benchmark] public void FrameReadback() => TinyFfrThread.Invoke(&SmokeSections.FrameReadback);
	[Benchmark] public void BufferAsDynamicTexture() => TinyFfrThread.Invoke(&SmokeSections.BufferAsDynamicTexture);
	[Benchmark] public void RenderQualityAndCulling() => TinyFfrThread.Invoke(&SmokeSections.RenderQualityAndCulling);
	[Benchmark] public void ViewportSubAreas() => TinyFfrThread.Invoke(&SmokeSections.ViewportSubAreas);
	[Benchmark] public void PixelPicking() => TinyFfrThread.Invoke(&SmokeSections.PixelPicking);
	[Benchmark] public void Compositing() => TinyFfrThread.Invoke(&SmokeSections.Compositing);
	[Benchmark] public void LoopIteration() => TinyFfrThread.Invoke(&SmokeSections.LoopIteration);

	[Benchmark] public void Fonts() => TinyFfrThread.Invoke(&SmokeSections.Fonts);
	[Benchmark] public void TextInstances() => TinyFfrThread.Invoke(&SmokeSections.TextInstances);
	[Benchmark] public void Quads() => TinyFfrThread.Invoke(&SmokeSections.Quads);
	[Benchmark] public void CanvasScenes() => TinyFfrThread.Invoke(&SmokeSections.CanvasScenes);

	[Benchmark] public void LoadMeshFile() => TinyFfrThread.Invoke(&SmokeSections.LoadMeshFile);
	[Benchmark] public void LoadModelFile() => TinyFfrThread.Invoke(&SmokeSections.LoadModelFile);
	[Benchmark] public void SkeletalAnimation() => TinyFfrThread.Invoke(&SmokeSections.SkeletalAnimation);
	[Benchmark] public void AsyncLoading() => TinyFfrThread.Invoke(&SmokeSections.AsyncLoading);
	[Benchmark] public void BakeAssets() => TinyFfrThread.Invoke(&SmokeSections.BakeAssets);
	[Benchmark] public void LoadBakedAssets() => TinyFfrThread.Invoke(&SmokeSections.LoadBakedAssets);

	[Benchmark] public void ResourceGroups() => TinyFfrThread.Invoke(&SmokeSections.ResourceGroups);
	[Benchmark] public void ResourceNamingAndDirectory() => TinyFfrThread.Invoke(&SmokeSections.ResourceNamingAndDirectory);
	[Benchmark] public void AllocatorCollections() => TinyFfrThread.Invoke(&SmokeSections.AllocatorCollections);
	[Benchmark] public void DisplayDiscovery() => TinyFfrThread.Invoke(&SmokeSections.DisplayDiscovery);

}
