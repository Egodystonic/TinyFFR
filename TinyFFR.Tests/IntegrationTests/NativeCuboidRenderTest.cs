// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeCuboidRenderTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = (ILocalRendererFactory) new LocalRendererFactory();

		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(10f, 7f, 2f), new MeshCreationConfig { Name = "Clive the Cuboid" });
		using var camera = factory.CameraBuilder.CreateCamera(new CameraCreationConfig {
			Position = (Direction.Backward * 1f).AsLocation(),
			ViewDirection = Direction.Forward,
			UpDirection = Direction.Up
		});
		using var mat = factory.AssetLoader.MaterialBuilder.CreateBasicSolidColorMat(0x00FF00, new MaterialCreationConfig { Name = "Matthew the Material" });
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
		
		Console.WriteLine(camera);
		Console.WriteLine(mesh);
		Console.WriteLine(mat);
		Console.WriteLine(instance);
	}
}