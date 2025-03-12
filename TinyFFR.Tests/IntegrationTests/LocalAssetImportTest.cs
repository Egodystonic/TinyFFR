// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalAssetImportTest {
	const string AlbedoFile = "IntegrationTests\\ELCrate.png";
	const string NormalFile = "IntegrationTests\\ELCrate_Normal.png";
	const string SpecularFile = "IntegrationTests\\ELCrate_Specular.png";
	const string MeshFile = "IntegrationTests\\ELCrate.obj";
	const string SkyboxFile = "IntegrationTests\\kloofendal_48d_partly_cloudy_puresky_4k.hdr";
	// These values were sampled/taken from an external paint program
	static readonly Dictionary<int, TexelRgb24> _expectedSampledAlbedoPixelValues = new() {
		[1024 * 0000 + 0000] = new(0, 0, 0),
		[1024 * 1023 + 0000] = new(66, 56, 41),
		[1024 * 1023 + 1023] = new(122, 110, 89),
		[1024 * 0000 + 1023] = new(0, 0, 0),
	};
	static readonly Dictionary<int, TexelRgb24> _expectedSampledNormalPixelValues = new() {
		[1024 * 0180 + 0374] = new(127, 127, 255),
		[1024 * 0345 + 0786] = new(127, 228, 205),
		[1024 * 0412 + 1001] = new(133, 126, 255),
		[1024 * 0497 + 0284] = new(245, 123, 176),
	};
	static readonly Dictionary<int, TexelRgb24> _expectedSampledSpecularPixelValues = new() {
		[1024 * 0187 + 0344] = new(237, 237, 237),
		[1024 * 0270 + 0360] = new(23, 23, 23),
		[1024 * 0803 + 0373] = new(159, 159, 159),
		[1024 * 1023 + 1023] = new(0, 0, 0),
	};

	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	void AssertTexelSamples(Span<TexelRgb24> readData, Dictionary<int, TexelRgb24> expectation) {
		foreach (var kvp in expectation) {
			Assert.AreEqual(kvp.Value, readData[kvp.Key], $"Failed at texel {kvp.Key}.");
		}
	}

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		Assert.AreEqual(new TextureReadMetadata(1024, 1024), factory.AssetLoader.ReadTextureMetadata(AlbedoFile));
		Assert.AreEqual(new TextureReadMetadata(1024, 1024), factory.AssetLoader.ReadTextureMetadata(NormalFile));
		Assert.AreEqual(new TextureReadMetadata(1024, 1024), factory.AssetLoader.ReadTextureMetadata(SpecularFile));

		var texBuffer = (new TexelRgb24[1024 * 1024]).AsSpan();
		factory.AssetLoader.ReadTexture(AlbedoFile, texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledAlbedoPixelValues);
		factory.AssetLoader.ReadTexture(NormalFile, texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledNormalPixelValues);
		factory.AssetLoader.ReadTexture(SpecularFile, texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledSpecularPixelValues);

		Assert.AreEqual(new MeshReadMetadata(662, 480), factory.AssetLoader.ReadMeshMetadata(MeshFile));
		var meshVertexBuffer = new MeshVertex[662];
		var meshTriangleBuffer = new VertexTriangle[480];
		factory.AssetLoader.ReadMesh(MeshFile, meshVertexBuffer, meshTriangleBuffer);
		var minLoc = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
		var maxLoc = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
		foreach (var v in meshVertexBuffer) {
			var loc = v.Location;
			minLoc = new(Single.Min(minLoc.X, loc.X), Single.Min(minLoc.Y, loc.Y), Single.Min(minLoc.Z, loc.Z));
			maxLoc = new(Single.Max(maxLoc.X, loc.X), Single.Max(maxLoc.Y, loc.Y), Single.Max(maxLoc.Z, loc.Z));
		}
		var calculatedOrigin = ((minLoc.AsVect() + maxLoc.AsVect()) * 0.5f).AsLocation();
		Assert.AreEqual(new Location(0f, 11.344924f, 1.9073486E-06f), calculatedOrigin);

		var display = factory.DisplayDiscoverer.Recommended!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Asset Import Test");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var albedo = factory.AssetLoader.LoadTexture(AlbedoFile);
		using var normal = factory.AssetLoader.LoadTexture(NormalFile);
		using var orm = factory.AssetLoader.LoadAndCombineOrmTextures(roughnessMapFilePath: SpecularFile, metallicMapFilePath: SpecularFile, config: new() { InvertYGreenChannel = true });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(albedo, normal, orm);
		using var mesh = factory.AssetLoader.LoadMesh(MeshFile, new MeshCreationConfig { LinearRescalingFactor = 0.03f, OriginTranslation = calculatedOrigin.AsVect() });
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 1.3f);
		using var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(SkyboxFile);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(instance);
		scene.SetBackdrop(cubemap, 0f);
		
		var instanceToCameraVect = instance.Position >> camera.Position;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8.9d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			instanceToCameraVect = instanceToCameraVect.RotatedBy(2.3f % Direction.Up);
			camera.Position = instance.Position + instanceToCameraVect + (Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 1.67f));
			camera.ViewDirection = (camera.Position >> instance.Position).Direction;
			var cbBrightness = (float) Math.Floor(loop.TotalIteratedTime.TotalSeconds) * 0.25f;
			scene.SetBackdrop(cubemap, cbBrightness);
			window.Title = "Backdrop brightness level " + PercentageUtils.ConvertFractionToPercentageString(cbBrightness);
		}
	}
}