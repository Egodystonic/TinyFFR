// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

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
		TestAnisotropyConversion();

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

		Assert.AreEqual(new MeshReadMetadata(602, 480), factory.AssetLoader.ReadMeshMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh)));
		var meshVertexBuffer = new MeshVertex[602];
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
			finalOutputConfig: new TextureCreationConfig { IsLinearColorspace = true, ProcessingToApply = new() { InvertYGreenChannel = true, InvertZBlueChannel = true } }
		);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(albedo, normal, orm);
		using var mesh = factory.AssetLoader.LoadMesh(
			CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh), 
			new MeshCreationConfig { LinearRescalingFactor = 0.03f, OriginTranslation = calculatedOrigin.AsVect() }
		);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 1.3f);
		using var cubemap = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr), new BackdropTextureCreationConfig(), BackdropTextureResolution.RoughDraft);
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

		scene.SetBackdrop(cubemap, 0.77f);
		scene.Remove(instance);
		ExecuteMapLoadTests(factory, window, loop, camera, scene, renderer);
	}

	void TestAnisotropyConversion() {
		var texel = new TexelRgb24();
		var tSpan = new Span<TexelRgb24>(ref texel);

		texel = new(0, 127, 0);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, true, ColorChannel.G);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(0f, 0.5f), texel);

		texel = new(64, 26, 0);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Left, AnisotropyRadialAngleRange.ZeroTo360, true, ColorChannel.G);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(270f, 0.105f), texel);

		texel = new(64, 26, 0);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, ColorChannel.G);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(90f, 0.105f), texel);

		texel = new(64, 26, 0);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, true, ColorChannel.G);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(45.5f, 0.105f), texel);

		texel = new(64, 26, 0);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, false, ColorChannel.G);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(-90f, 0.105f), texel);

		texel = new(64, 0, 26);
		IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(tSpan, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, false, ColorChannel.B);
		Assert.AreEqual(ITextureBuilder.CreateAnisotropyTexel(-90f, 0.105f), texel);
	}

	void ExecuteMapLoadTests(LocalTinyFfrFactory factory, Window window, ApplicationLoop loop, Camera camera, Scene scene, Renderer renderer) {
		var cube = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		var defaultMat = factory.MaterialBuilder.CreateTestMaterial(false);
		var instance = factory.ObjectBuilder.CreateModelInstance(cube, defaultMat);
		var lights = new List<PointLight>();
		lights.Add(factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Forward * 2f));
		lights.Add(factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Backward * 2f));
		lights.Add(factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Right * 2f));
		lights.Add(factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Left * 2f));
		foreach (var l in lights) scene.Add(l);

		instance.Position = Location.Origin;
		instance.Scaling = new Vect(0.4f);
		scene.Add(instance);

		camera.Position = new Location(0f, 0f, -1f);
		var instanceToCameraVect = instance.Position >> camera.Position;
		camera.UpDirection = Direction.Up;

		window.SetTitle("Press Space");
		var curTestIndex = 0;

		var resourcesToBeDisposed = new List<IDisposableResource>();

		while (!loop.Input.UserQuitRequested) {
			_ = loop.IterateOnce();
			renderer.Render();

			camera.Position = instance.Position + instanceToCameraVect * (Direction.Up % ((float) loop.TotalIteratedTime.TotalSeconds * 20f));
			camera.ViewDirection = (camera.Position >> instance.Position).Direction;

			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				curTestIndex++;
				switch (curTestIndex) {
					case 1: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadColorMap(factory.AssetLoader.BuiltInTexturePaths.White));
						Assert.AreEqual("?tffr_builtin?bytes_255_255_255", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be white cube");
						break;
					}
					case 2: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadNormalMap(factory.AssetLoader.BuiltInTexturePaths.RedGreen));
						Assert.AreEqual("?tffr_builtin?bytes_255_255_0", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							normalMap: (Texture) resourcesToBeDisposed[^1]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be white cube with weird normals");
						break;
					}
					case 3: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadOcclusionRoughnessMetallicMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultOcclusionMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultRoughnessMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultMetallicMap
						));
						Assert.AreEqual("?tffr_builtin?map_occlusion+?tffr_builtin?map_roughness+?tffr_builtin?map_metallic", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadOcclusionRoughnessMetallicReflectanceMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultOcclusionRoughnessMetallicMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultReflectanceMap
						));
						Assert.AreEqual("?tffr_builtin?map_orm+?tffr_builtin?map_reflectance", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadOcclusionRoughnessMetallicReflectanceMap(
							factory.AssetLoader.BuiltInTexturePaths.Red,
							factory.AssetLoader.BuiltInTexturePaths.Green,
							factory.AssetLoader.BuiltInTexturePaths.Blue,
							factory.AssetLoader.BuiltInTexturePaths.White
						));
						Assert.AreEqual("?tffr_builtin?bytes_255_0_0+?tffr_builtin?bytes_0_255_0+?tffr_builtin?bytes_0_0_255+?tffr_builtin?bytes_255_255_255", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							ormOrOrmrMap: (Texture) resourcesToBeDisposed[^1]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be white cube with reflectance");
						break;
					}
					case 4: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadAbsorptionTransmissionMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultAbsorptionTransmissionMap
						));
						Assert.AreEqual("?tffr_builtin?map_at", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadAbsorptionTransmissionMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultAbsorptionMap
						));
						Assert.AreEqual("?tffr_builtin?map_absorption+?tffr_builtin?map_transmission", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadAbsorptionTransmissionMap(
							factory.AssetLoader.BuiltInTexturePaths.RedOpaque,
							factory.AssetLoader.BuiltInTexturePaths.White,
							invertAbsorption: true
						));
						Assert.AreEqual("?tffr_builtin?bytes_255_0_0_255+?tffr_builtin?bytes_255_255_255", resourcesToBeDisposed[^1].GetNameAsNewStringObject());

						resourcesToBeDisposed.Add(factory.TextureBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(metallic: 0f, reflectance: 1f, roughness: 0.15f));
						var newMat = factory.MaterialBuilder.CreateTransmissiveMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							ormrMap: (Texture) resourcesToBeDisposed[^1],
							absorptionTransmissionMap: (Texture) resourcesToBeDisposed[^2],
							refractionThickness: 1f
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be reddish translucent cube");
						break;
					}
					case 5: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadEmissiveMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultEmissiveColorMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultEmissiveIntensityMap
						));
						Assert.AreEqual("?tffr_builtin?map_emissive-color+?tffr_builtin?map_emissive-intensity", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						
						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							emissiveMap: (Texture) resourcesToBeDisposed[^1]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be glowing cube");
						break;
					}
					case 6: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadAnisotropyMapVectorFormatted(
							factory.AssetLoader.BuiltInTexturePaths.DefaultAnisotropyVectorMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultAnisotropyStrengthMap
						));
						Assert.AreEqual("?tffr_builtin?map_anisotropy-vector+?tffr_builtin?map_anisotropy-strength", resourcesToBeDisposed[^1].GetNameAsNewStringObject());
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadAnisotropyMapRadialAngleFormatted(
							factory.AssetLoader.BuiltInTexturePaths.DefaultAnisotropyRadialAngleMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultAnisotropyStrengthMap,
							Orientation2D.Up, AnisotropyRadialAngleRange.ZeroTo180, true
						));
						Assert.AreEqual("?tffr_builtin?map_anisotropy-angle+?tffr_builtin?map_anisotropy-strength", resourcesToBeDisposed[^1].GetNameAsNewStringObject());

						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							anisotropyMap: (Texture) resourcesToBeDisposed[^1]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be cube with anisotropic highlights");
						break;
					}
					case 7: {
						var prevMat = instance.Material;
						resourcesToBeDisposed.Add(factory.AssetLoader.LoadClearCoatMap(
							factory.AssetLoader.BuiltInTexturePaths.DefaultClearCoatThicknessMap,
							factory.AssetLoader.BuiltInTexturePaths.DefaultClearCoatRoughnessMap
						));
						Assert.AreEqual("?tffr_builtin?map_clearcoat-thickness+?tffr_builtin?map_clearcoat-roughness", resourcesToBeDisposed[^1].GetNameAsNewStringObject());

						var newMat = factory.MaterialBuilder.CreateStandardMaterial(
							colorMap: (Texture) resourcesToBeDisposed[0],
							clearCoatMap: (Texture) resourcesToBeDisposed[^1]
						);
						instance.Material = newMat;
						prevMat.Dispose();
						window.SetTitle("Press Space // should be cube with clearcoat");
						break;
					}
					default: return;
				}
			}
		}

		scene.Remove(instance);
		foreach (var l in lights) {
			scene.Remove(l);
			l.Dispose();
		}
		var mat = instance.Material;
		instance.Dispose();
		mat.Dispose();
		cube.Dispose();
		foreach (var r in resourcesToBeDisposed) r.Dispose();
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