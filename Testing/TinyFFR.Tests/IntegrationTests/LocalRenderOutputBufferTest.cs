// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalRenderOutputBufferTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var colorTex = factory.MaterialBuilder.CreateColorMap(ColorVect.White);
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
		));
		using var ormTex = factory.MaterialBuilder.CreateOrmMap();
		using var mat = factory.MaterialBuilder.CreateOpaqueMaterial(colorTex, normalTex, ormTex);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap();
		using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap();
		using var light = factory.LightBuilder.CreatePointLight(camera.Position);
		var backdropColor = new ColorVect(1f, 0.3f, 0.3f);

		scene.Add(instance);
		scene.Add(light);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			instance.RotateBy(1.3f % Direction.Up);
			instance.RotateBy(0.8f % Direction.Right);

			light.Color = light.Color.WithHueAdjustedBy(1f);
			light.Position = instance.Position + (((instance.Position >> camera.Position) * 0.44f) * ((MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 5f) * 15f) % Direction.Down));
			light.Position += Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 4f) * 0.5f;

			backdropColor = backdropColor.WithHueAdjustedBy(-1f);
			scene.SetBackdrop(backdropColor);
		}
	}

	void RenderSceneToBitmapAndStoreTexels(Scene scene, List<TexelRgba32[]> renderDumpList) {
		
	}
}