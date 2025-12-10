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
using System.Reflection.PortableExecutable;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMaterialEffectsTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(
			display, 
			title: "Effects test: Arrow keys / RShift+RCtrl / PgUpPgDown | C / O / E / A"
		);

		using var backdrop = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, centreTextureOrigin: true);

		using var camera = factory.CameraBuilder.CreateCamera();
		using var light = factory.LightBuilder.CreatePointLight(position: (0f, 0f, 1f));
		
		var uvTexMetadata = factory.AssetLoader.ReadTextureMetadata(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture);
		Assert.AreEqual(false, uvTexMetadata.IncludesAlphaChannel);
		var uvTexDataSpan = new TexelRgb24[uvTexMetadata.Dimensions.Area];
		_ = factory.AssetLoader.ReadTexture(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture, uvTexDataSpan);

		using var uvTestTex = factory.TextureBuilder.CreateTexture(uvTexDataSpan, uvTexMetadata.Dimensions, isLinearColorspace: false);
		using var colorMapBlendTex = factory.TextureBuilder.CreateTexture(
			uvTexDataSpan, 
			new TextureGenerationConfig { Dimensions = uvTexMetadata.Dimensions }, 
			new TextureCreationConfig { IsLinearColorspace = false, ProcessingToApply = TextureProcessingConfig.Invert() }
		);


		using var leftMat = factory.MaterialBuilder.CreateSimpleMaterial(colorMap: uvTestTex, enablePerInstanceEffects: true);
		using var midMat = factory.MaterialBuilder.CreateTestMaterial();
		using var rightMat = factory.MaterialBuilder.CreateTestMaterial();

		using var leftInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, leftMat, new Location(1.8f, 0f, 3f));
		using var midInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, midMat, new Location(0f, 0f, 3f));
		using var rightInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, rightMat, new Location(-1.8f, 0f, 3f));

		leftInstance.MaterialEffects.SetBlendTexture(MaterialEffectMapType.Color, colorMapBlendTex);

		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		scene.Add(light);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		scene.Add(leftInstance);
		scene.Add(midInstance);
		scene.Add(rightInstance);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(144);

		var materialsTransform = Transform2D.None;
		var cycleColorTex = false;

		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;
			var tt = (float) loop.TotalIteratedTime.TotalSeconds;

			var kbm = loop.Input.KeyboardAndMouse;

			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowLeft)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((-1f * dt, 0f));
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowRight)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((1f * dt, 0f));
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowUp)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((0f, 1f * dt));
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowDown)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((0f, -1f * dt));
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightShift)) {
				materialsTransform = materialsTransform.WithAdditionalRotation(45f * dt);
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightControl)) {
				materialsTransform = materialsTransform.WithAdditionalRotation(-45f * dt);
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageUp)) {
				materialsTransform = materialsTransform.WithScalingAdjustedBy(dt);
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageDown)) {
				materialsTransform = materialsTransform.WithScalingAdjustedBy(-dt);
				leftInstance.MaterialEffects.SetTransform(materialsTransform);
				midInstance.MaterialEffects.SetTransform(materialsTransform);
				rightInstance.MaterialEffects.SetTransform(materialsTransform);
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.C)) cycleColorTex = !cycleColorTex;

			if (cycleColorTex) {
				leftInstance.MaterialEffects.SetBlendDistance(MaterialEffectMapType.Color, MathF.Sin(tt));
				midInstance.MaterialEffects.SetBlendDistance(MaterialEffectMapType.Color, MathF.Sin(tt));
				rightInstance.MaterialEffects.SetBlendDistance(MaterialEffectMapType.Color, MathF.Sin(tt));
			}

			light.Position = light.Position with { Y = MathF.Sin(tt) };

			renderer.Render();
		}
	}
}