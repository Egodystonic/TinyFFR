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
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalAssetImportTest {
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

		ExecuteTextureCombineAndReadTests(factory);

		Assert.AreEqual(new TextureReadMetadata((1024, 1024), false), factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex)));
		Assert.AreEqual(new TextureReadMetadata((1024, 1024), false), factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex)));
		Assert.AreEqual(new TextureReadMetadata((1024, 1024), false), factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex)));

		var texBuffer = (new TexelRgb24[1024 * 1024]).AsSpan();
		factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex), texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledAlbedoPixelValues);
		factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex), texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledNormalPixelValues);
		factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex), texBuffer);
		AssertTexelSamples(texBuffer, _expectedSampledSpecularPixelValues);

		Assert.AreEqual(new MeshReadMetadata(662, 480), factory.AssetLoader.ReadMeshMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh)));
		var meshVertexBuffer = new MeshVertex[662];
		var meshTriangleBuffer = new VertexTriangle[480];
		factory.AssetLoader.ReadMesh(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh), meshVertexBuffer, meshTriangleBuffer);
		var minLoc = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
		var maxLoc = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
		foreach (var v in meshVertexBuffer) {
			var loc = v.Location;
			minLoc = new(Single.Min(minLoc.X, loc.X), Single.Min(minLoc.Y, loc.Y), Single.Min(minLoc.Z, loc.Z));
			maxLoc = new(Single.Max(maxLoc.X, loc.X), Single.Max(maxLoc.Y, loc.Y), Single.Max(maxLoc.Z, loc.Z));
		}
		var calculatedOrigin = ((minLoc.AsVect() + maxLoc.AsVect()) * 0.5f).AsLocation();
		Assert.AreEqual(new Location(0f, 11.344924f, 1.9073486E-06f), calculatedOrigin);

		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Asset Import Test");
		window.SetIcon(CommonTestAssets.FindAsset(KnownTestAsset.EgodystonicLogo));
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var albedo = factory.AssetLoader.LoadColorMap(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex));
		using var normal = factory.AssetLoader.LoadNormalMap(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex));
		using var orm = factory.AssetLoader.LoadCombinedTexture(
			aFilePath: CommonTestAssets.FindAsset(KnownTestAsset.WhiteTex),
			aProcessingConfig: TextureProcessingConfig.None,
			bFilePath: CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex),
			bProcessingConfig: TextureProcessingConfig.None,
			cFilePath: CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex),
			cProcessingConfig: TextureProcessingConfig.None,
			combinationConfig: new(
				new(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
				new(TextureCombinationSourceTexture.TextureB, ColorChannel.R),
				new(TextureCombinationSourceTexture.TextureC, ColorChannel.R)
			),
			finalOutputConfig: new TextureCreationConfig { IsLinearColorspace = false, ProcessingToApply = new() { InvertYGreenChannel = true, InvertZBlueChannel = true } }
		);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(albedo, normal, orm);
		using var mesh = factory.AssetLoader.LoadMesh(
			CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh), 
			new MeshCreationConfig { LinearRescalingFactor = 0.03f, OriginTranslation = calculatedOrigin.AsVect() }
		);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 1.3f);
		using var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
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
			window.SetTitle("Backdrop brightness level " + PercentageUtils.ConvertFractionToPercentageString(cbBrightness));
		}
	}

	void ExecuteTextureCombineAndReadTests(ILocalTinyFfrFactory factory) {
		var swatchTexPath = CommonTestAssets.FindAsset(KnownTestAsset.SwatchTex);
		var swatchAlphaTexPath = CommonTestAssets.FindAsset(KnownTestAsset.SwatchAlphaTex);
		var whiteTexPath = CommonTestAssets.FindAsset(KnownTestAsset.WhiteTex);
		var egdTexPath = CommonTestAssets.FindAsset(KnownTestAsset.EgodystonicLogo);
		var rgbBuffer = new TexelRgb24[100];
		var rgbaBuffer = new TexelRgba32[100];

		void AssertSwatchBufferAndClear() {
			Assert.AreEqual(new TexelRgb24(255, 255, 255), rgbBuffer[0]);
			Assert.AreEqual(new TexelRgb24(0, 0, 0), rgbBuffer[1]);
			Assert.AreEqual(new TexelRgb24(128, 128, 128), rgbBuffer[2]);
			Assert.AreEqual(new TexelRgb24(255, 255, 0), rgbBuffer[3]);
			Assert.AreEqual(new TexelRgb24(0, 255, 255), rgbBuffer[4]);
			Assert.AreEqual(new TexelRgb24(255, 0, 255), rgbBuffer[5]);
			Assert.AreEqual(new TexelRgb24(255, 0, 0), rgbBuffer[6]);
			Assert.AreEqual(new TexelRgb24(0, 255, 0), rgbBuffer[7]);
			Assert.AreEqual(new TexelRgb24(0, 0, 255), rgbBuffer[8]);
			Array.Clear(rgbBuffer);
		}

		// swatch
		var swatchMetadata = factory.AssetLoader.ReadTextureMetadata(swatchTexPath);
		Assert.AreEqual(new XYPair<int>(3, 3), swatchMetadata.Dimensions);
		Assert.AreEqual(false, swatchMetadata.IncludesAlphaChannel);

		var texelCount = factory.AssetLoader.ReadTexture(swatchTexPath, rgbBuffer);
		Assert.AreEqual(9, texelCount);

		AssertSwatchBufferAndClear();

		texelCount = factory.AssetLoader.ReadTexture(swatchTexPath, rgbaBuffer);
		Assert.AreEqual(9, texelCount);

		for (var i = 0; i < 9; ++i) {
			Assert.AreEqual((byte) 255, rgbaBuffer[i].A);
			rgbBuffer[i] = rgbaBuffer[i].ToRgb24();
		}
		AssertSwatchBufferAndClear();

		// swatch_alpha
		var swatchAlphaMetadata = factory.AssetLoader.ReadTextureMetadata(swatchAlphaTexPath);
		Assert.AreEqual(new XYPair<int>(3, 3), swatchAlphaMetadata.Dimensions);
		Assert.AreEqual(true, swatchAlphaMetadata.IncludesAlphaChannel);

		texelCount = factory.AssetLoader.ReadTexture(swatchAlphaTexPath, rgbBuffer);
		Assert.AreEqual(9, texelCount);

		AssertSwatchBufferAndClear();

		texelCount = factory.AssetLoader.ReadTexture(swatchAlphaTexPath, rgbaBuffer);
		Assert.AreEqual(9, texelCount);

		for (var i = 0; i < 9; ++i) {
			Assert.AreEqual((byte) 127, rgbaBuffer[i].A);
			rgbBuffer[i] = rgbaBuffer[i].ToRgb24();
		}
		AssertSwatchBufferAndClear();


		// Combination testing
		var expectation = new TexelRgba32[] {
			new(255 - 0, 255 - 128, 255 - 127, 255 - 128),
			new(255 - 255, 255 - 0, 255 - 127, 255 - 0),
			new(255 - 0, 255 - 255, 255 - 127, 255 - 255),

			new(255 - 255, 255 - 255, 255 - 127, 255 - 255),
			new(255 - 255, 255 - 0, 255 - 127, 255 - 255),
			new(255 - 0, 255 - 255, 255 - 127, 255 - 0),

			new(255 - 255, 255 - 0, 255 - 127, 255 - 255),
			new(255 - 0, 255 - 0, 255 - 127, 255 - 0),
			new(255 - 128, 255 - 255, 255 - 127, 255 - 0),
		};

		// 2x combine in to RGBA buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			new TextureCombinationConfig("bGaRbAaB"), TextureProcessingConfig.Invert(), rgbaBuffer
		);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// 2x combine in to RGB buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			new TextureCombinationConfig("bGaRbAaB"), TextureProcessingConfig.Invert(), rgbBuffer
		);
		for (var i = 0; i < 9; ++i) rgbaBuffer[i] = rgbBuffer[i].ToRgba32(expectation[i].A);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// 3x combine in to RGBA buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			new TextureCombinationConfig("bGaRbAcB"), TextureProcessingConfig.Invert(), rgbaBuffer
		);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// 3x combine in to RGB buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			new TextureCombinationConfig("bGcRbAaB"), TextureProcessingConfig.Invert(), rgbBuffer
		);
		for (var i = 0; i < 9; ++i) rgbaBuffer[i] = rgbBuffer[i].ToRgba32(expectation[i].A);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// 4x combine in to RGBA buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			new TextureCombinationConfig("dGaRbAcB"), TextureProcessingConfig.Invert(), rgbaBuffer
		);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// 4x combine in to RGB buffer
		factory.AssetLoader.ReadCombinedTexture(
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			swatchTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: true, aroundHorizontalCentre: false),
			swatchAlphaTexPath, TextureProcessingConfig.Flip(aroundVerticalCentre: false, aroundHorizontalCentre: true),
			new TextureCombinationConfig("bGcRdAaB"), TextureProcessingConfig.Invert(), rgbBuffer
		);
		for (var i = 0; i < 9; ++i) rgbaBuffer[i] = rgbBuffer[i].ToRgba32(expectation[i].A);
		Assert.IsTrue(rgbaBuffer[..9].SequenceEqual(expectation));
		Array.Clear(rgbaBuffer);

		// combination metadata
		var twoTexCombineMetadata = factory.AssetLoader.ReadCombinedTextureMetadata(swatchTexPath, whiteTexPath);
		Assert.AreEqual(new XYPair<int>(3, 3), twoTexCombineMetadata.Dimensions);
		Assert.AreEqual(false, twoTexCombineMetadata.IncludesAlphaChannel);

		var threeTexCombineMetadata = factory.AssetLoader.ReadCombinedTextureMetadata(swatchAlphaTexPath, whiteTexPath, swatchTexPath);
		Assert.AreEqual(new XYPair<int>(3, 3), threeTexCombineMetadata.Dimensions);
		Assert.AreEqual(true, threeTexCombineMetadata.IncludesAlphaChannel);

		var fourTexCombineMetadata = factory.AssetLoader.ReadCombinedTextureMetadata(swatchAlphaTexPath, whiteTexPath, swatchTexPath, egdTexPath);
		Assert.AreEqual(new XYPair<int>(128, 128), fourTexCombineMetadata.Dimensions);
		Assert.AreEqual(true, fourTexCombineMetadata.IncludesAlphaChannel);
	}
}