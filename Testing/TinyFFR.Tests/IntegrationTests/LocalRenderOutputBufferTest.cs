// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalRenderOutputBufferTest {
	static readonly XYPair<int> RenderDimensions = (480, 270);
	static readonly (string Name, ColorVect Color)[] SceneColors = [
		("purple", new(0.3f, 0f, 0.3f)),
		("green", new(0.8f, 1f, 0.8f))
	];
	Window w;

	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		w = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var colorTex = factory.MaterialBuilder.CreateColorMap(ColorVect.White, "color");
		using var normalTex = factory.MaterialBuilder.CreateNormalMap(TexturePattern.Rectangles(
			interiorSize: (256, 256),
			borderSize: (64, 64),
			paddingSize: (0, 0),
			interiorValue: Direction.Forward,
			borderRightValue: (-1f, 0f, 1f),
			borderTopValue: (0f, 1f, 1f),
			borderLeftValue: (1f, 0f, 1f),
			borderBottomValue: (0f, -1f, 1f),
			paddingValue: Direction.Forward,
			repetitions: (8, 8)
		), "normal");
		using var ormTex = factory.MaterialBuilder.CreateOrmMap(name: "orm");
		using var mat = factory.MaterialBuilder.CreateOpaqueMaterial(colorTex, normalTex, ormTex, name: "mat");
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var cube = factory.ObjectBuilder.CreateModelInstance(
			mesh,
			mat,
			Direction.Forward * 2.2f + Location.Origin,
			Direction.Up % 30f + Direction.Right % 14f
		);
		using var light = factory.LightBuilder.CreatePointLight(camera.Position);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);

		var bitmapDir = SetUpCleanTestDir("bitmaps");
		Console.WriteLine("Bitmaps being written to " + bitmapDir);
		var texelDumps = SceneColors.ToDictionary(tuple => tuple.Name, _ => new List<TexelRgba32[]>());
		for (var frameBufferCount = 0; frameBufferCount <= RendererCreationConfig.MaxGpuSynchronizationFrameBufferCount; ++frameBufferCount) {
			var scenes = SceneColors.ToDictionary(tuple => tuple.Name, tuple => {
				var result = factory.SceneBuilder.CreateScene(includeBackdrop: true, backdropColor: tuple.Color);
				result.Add(cube);
				result.Add(light);
				result.Dispose();

				result = factory.SceneBuilder.CreateScene(includeBackdrop: true, backdropColor: tuple.Color);
				result.Add(cube);
				result.Add(light);
				return result;
			});

			try {
				foreach (var kvp in scenes) {
					RenderSceneToBitmapAndStoreTexels(
						factory.RendererBuilder,
						loop,
						kvp.Value,
						camera,
						texelDumps[kvp.Key],
						Path.Combine(bitmapDir, kvp.Key + "_wait_" + frameBufferCount + ".bmp"),
						frameBufferCount,
						waitExplicitly: true
					);
					RenderSceneToBitmapAndStoreTexels(
						factory.RendererBuilder,
						loop,
						kvp.Value,
						camera,
						texelDumps[kvp.Key],
						Path.Combine(bitmapDir, kvp.Key + "_cycle_" + frameBufferCount + ".bmp"),
						frameBufferCount,
						waitExplicitly: false
					);
				}
			}
			finally {
				foreach (var scene in scenes.Values) scene.Dispose();
			}
		}

		foreach (var list in texelDumps.Values) {
			Assert.AreEqual(RendererCreationConfig.MaxGpuSynchronizationFrameBufferCount + 1, list.Count);
			for (var i = 1; i < list.Count; ++i) {
				Assert.IsTrue(list[0].SequenceEqual(list[i]));
			}
		}
	}

	void RenderSceneToBitmapAndStoreTexels(IRendererBuilder builder, ApplicationLoop loop, Scene scene, Camera camera, List<TexelRgba32[]> renderDumpList, string bitmapFilePath, int frameBufferCount, bool waitExplicitly) {
		Console.WriteLine("..." + Path.GetFileNameWithoutExtension(bitmapFilePath));

		using var buffer = builder.CreateRenderOutputBuffer(textureDimensions: RenderDimensions);
		using var renderer = builder.CreateRenderer(scene, camera, buffer);

		buffer.ReadNextFrame((size, texels) => {
			Console.WriteLine("> callback for " + Path.GetFileNameWithoutExtension(bitmapFilePath));
			Assert.AreEqual(RenderDimensions, size);
			renderDumpList.Add(texels.ToArray());

			ImageUtils.SaveBitmap(bitmapFilePath, size, texels, new() { IncludeAlphaChannel = false });
		});

		_ = loop.IterateOnce();
		if (waitExplicitly) {
			renderer.RenderAndWaitForGpu();
		}
		else {
			renderer.Render();
			for (var i = 0; i < frameBufferCount; ++i) {
				_ = loop.IterateOnce();
				renderer.Render();
			}
		}

		var r = builder.CreateRenderer(scene, camera, w);
		for (var i = 0; i < 100; ++i) {
			_ = loop.IterateOnce();
			r.Render();
		}
		r.Dispose();
	}
}