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
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMaterialEffectsTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	void AssertInvalidRequestsDoNotCauseIssues(ILocalTinyFfrFactory factory) {
		// This prods against non-effect materials and also against effect materials with blend maps for non-loaded map types
		using var colorMap = factory.TextureBuilder.CreateColorMap();
		using var atMap = factory.TextureBuilder.CreateAbsorptionTransmissionMap();
		using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
		using var simpleMat = factory.MaterialBuilder.CreateSimpleMaterial(colorMap);
		using var standardMat = factory.MaterialBuilder.CreateStandardMaterial(colorMap);
		using var transmissiveMat = factory.MaterialBuilder.CreateTransmissiveMaterial(colorMap, atMap);
		using var simpleMatWithEffects = factory.MaterialBuilder.CreateSimpleMaterial(colorMap, enablePerInstanceEffects: true);
		using var standardMatWithEffects = factory.MaterialBuilder.CreateStandardMaterial(colorMap, enablePerInstanceEffects: true);
		using var transmissiveMatWithEffects = factory.MaterialBuilder.CreateTransmissiveMaterial(colorMap, atMap, enablePerInstanceEffects: true);

		using var simpleObj = factory.ObjectBuilder.CreateModelInstance(cubeMesh, simpleMat);
		using var standardObj = factory.ObjectBuilder.CreateModelInstance(cubeMesh, standardMat);
		using var transmissiveObj = factory.ObjectBuilder.CreateModelInstance(cubeMesh, transmissiveMat);

		Assert.IsNull(simpleObj.MaterialEffects);
		Assert.IsNull(standardObj.MaterialEffects);
		Assert.IsNull(transmissiveObj.MaterialEffects);

		simpleObj.Material = simpleMatWithEffects;
		standardObj.Material = standardMatWithEffects;
		transmissiveObj.Material = transmissiveMatWithEffects;

		void TestEffectsController(MaterialEffectController c) {
			c.SetBlendDistance(MaterialEffectMapType.Color, Single.NaN);
			c.SetBlendDistance(MaterialEffectMapType.OcclusionRoughnessMetallic, Single.PositiveInfinity);
			c.SetBlendDistance(MaterialEffectMapType.OcclusionRoughnessMetallicReflectance, Single.NegativeInfinity);
			c.SetBlendDistance(MaterialEffectMapType.Emissive, Single.NaN);
			c.SetBlendDistance(MaterialEffectMapType.AbsorptionTransmission, Single.PositiveInfinity);

			c.SetBlendTexture(MaterialEffectMapType.Color, colorMap);
			c.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallic, colorMap);
			c.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallicReflectance, colorMap);
			c.SetBlendTexture(MaterialEffectMapType.Emissive, colorMap);
			c.SetBlendTexture(MaterialEffectMapType.AbsorptionTransmission, colorMap);
		}

		TestEffectsController(simpleObj.MaterialEffects!.Value);
		TestEffectsController(standardObj.MaterialEffects!.Value);
		TestEffectsController(transmissiveObj.MaterialEffects!.Value);
	}

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(
			display, 
			title: "Effects test: Arrow keys, RShift, RCtrl, PgUpPgDown | 0-9, ` | C, O, E, A"
		);

		using var backdrop = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, centreTextureOrigin: true);

		using var camera = factory.CameraBuilder.CreateCamera(initialPosition: (0f, 1.4f, 0f));
		camera.LookAt((0f, 0f, 3f), Direction.Up);
		camera.MoveBy(camera.ViewDirection * 0.4f);
		using var light = factory.LightBuilder.CreateDirectionalLight(direction: (1f, -1f, 0f), castsShadows: false);
		AssertInvalidRequestsDoNotCauseIssues(factory);

		var uvTexMetadata = factory.AssetLoader.ReadTextureMetadata(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture);
		Assert.AreEqual(false, uvTexMetadata.IncludesAlphaChannel);
		var uvTexDataSpan = new TexelRgb24[uvTexMetadata.Dimensions.Area];
		_ = factory.AssetLoader.ReadTexture(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture, uvTexDataSpan);

		using var colorMap = factory.TextureBuilder.CreateTexture(uvTexDataSpan.Select(t => t.ToRgba32()).ToArray(), uvTexMetadata.Dimensions, isLinearColorspace: false);
		using var colorMapBlend = factory.TextureBuilder.CreateTexture(
			uvTexDataSpan.Select(texel => texel.R <= texel.G ? texel.ToRgba32() : new TexelRgba32(0, 0, 0, 0)).ToArray(), 
			new TextureGenerationConfig { Dimensions = uvTexMetadata.Dimensions }, 
			new TextureCreationConfig { IsLinearColorspace = false, ProcessingToApply = TextureProcessingConfig.None }
		);
		using var emissiveMap = factory.TextureBuilder.CreateEmissiveMap(
			TexturePattern.Rectangles(
				interiorSize: TexturePatternDefaultValues.RectanglesDefaultInteriorSize,
				borderSize: new XYPair<int>(16, 16),
				paddingSize: TexturePatternDefaultValues.RectanglesDefaultPaddingSize,
				interiorValue: ColorVect.White,
				borderRightValue: new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(135f),
				borderTopValue: new ColorVect(1f, 1f, 0f).WithHueAdjustedBy(135f),
				borderLeftValue: new ColorVect(0f, 1f, 0f).WithHueAdjustedBy(135f),
				borderBottomValue: new ColorVect(0f, 0f, 1f).WithHueAdjustedBy(135f),
				paddingValue: ColorVect.Black,
				repetitions: (1, 1)
			),
			TexturePattern.PlainFill<Real>(0f)
		);
		using var emissiveMapBlend = factory.TextureBuilder.CreateEmissiveMap(
			TexturePattern.Rectangles(
				interiorSize: TexturePatternDefaultValues.RectanglesDefaultInteriorSize,
				borderSize: new XYPair<int>(16, 16),
				paddingSize: TexturePatternDefaultValues.RectanglesDefaultPaddingSize,
				interiorValue: ColorVect.White,
				borderRightValue: new ColorVect(1f, 0f, 0f),
				borderTopValue: new ColorVect(1f, 1f, 0f),
				borderLeftValue: new ColorVect(0f, 1f, 0f),
				borderBottomValue: new ColorVect(0f, 0f, 1f),
				paddingValue: ColorVect.Black,
				repetitions: (1, 1)
			),
			TexturePattern.Rectangles<Real>(
				interiorValue: 0f,
				borderValue: 1f,
				paddingValue: 0f,
				repetitions: (1, 1),
				borderSize: (16, 16)
			)
		);
		using var ormMap = factory.TextureBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			1f, 0f, 0f, 1f
		);
		using var ormMapBlend = factory.TextureBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			1f, 1f, 1f, 1f
		);
		using var atMap = factory.TextureBuilder.CreateAbsorptionTransmissionMap(
			ColorVect.Black, 1f
		);
		using var atMapBlend = factory.TextureBuilder.CreateAbsorptionTransmissionMap(
			new ColorVect(0f, 1f, 1f), 0.5f
		);

		using var leftMat = factory.MaterialBuilder.CreateSimpleMaterial(
			colorMap: colorMap,
			enablePerInstanceEffects: true
		);
		using var midMat = factory.MaterialBuilder.CreateStandardMaterial(
			colorMap: colorMap,
			emissiveMap: emissiveMap,
			ormOrOrmrMap: ormMap,
			alphaMode: StandardMaterialAlphaMode.FullBlending,
			enablePerInstanceEffects: true
		);
		using var rightMat = factory.MaterialBuilder.CreateTransmissiveMaterial(
			colorMap: factory.TextureBuilder.CreateColorMap(TexturePattern.Chequerboard(ColorVect.White, new ColorVect(0.7f, 0.7f, 0.7f)), includeAlpha: false),
			absorptionTransmissionMap: atMap,
			emissiveMap: emissiveMap,
			ormrMap: ormMap,
			alphaMode: TransmissiveMaterialAlphaMode.FullBlending,
			refractionThickness: 1f,
			enablePerInstanceEffects: true
		);

		using var leftInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, leftMat, new Location(1.8f, 0f, 3f));
		using var midInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, midMat, new Location(0f, 0f, 3f), 45f % Direction.Up);
		using var rightInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, rightMat, new Location(-1.8f, 0f, 3f));

		leftInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Color, colorMapBlend);
		midInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Color, colorMapBlend);
		rightInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Color, colorMapBlend);

		leftInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Emissive, emissiveMapBlend);
		midInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Emissive, emissiveMapBlend);
		rightInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.Emissive, emissiveMapBlend);

		leftInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallic, ormMapBlend);
		midInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallic, ormMapBlend);
		rightInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallic, ormMapBlend);

		leftInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.AbsorptionTransmission, atMapBlend);
		midInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.AbsorptionTransmission, atMapBlend);
		rightInstance.MaterialEffects?.SetBlendTexture(MaterialEffectMapType.AbsorptionTransmission, atMapBlend);

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
		var cycleEmissiveTex = false;
		var cycleOrmTex = false;
		var cycleAtTex = false;

		void SetTransforms(Transform2D t) {
			leftInstance.MaterialEffects?.SetTransform(t);
			midInstance.MaterialEffects?.SetTransform(t);
			rightInstance.MaterialEffects?.SetTransform(t);
		}
		void SetMapDistances(MaterialEffectMapType mapType, float dist) {
			leftInstance.MaterialEffects?.SetBlendDistance(mapType, dist);
			midInstance.MaterialEffects?.SetBlendDistance(mapType, dist);
			rightInstance.MaterialEffects?.SetBlendDistance(mapType, dist);
		}

		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;
			var tt = (float) loop.TotalIteratedTime.TotalSeconds;

			var kbm = loop.Input.KeyboardAndMouse;

			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowLeft)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((-1f * dt, 0f));
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowRight)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((1f * dt, 0f));
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowUp)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((0f, 1f * dt));
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowDown)) {
				materialsTransform = materialsTransform.WithAdditionalTranslation((0f, -1f * dt));
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightShift)) {
				materialsTransform = materialsTransform.WithAdditionalRotation(45f * dt);
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightControl)) {
				materialsTransform = materialsTransform.WithAdditionalRotation(-45f * dt);
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageUp)) {
				materialsTransform = materialsTransform.WithScalingAdjustedBy(dt);
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageDown)) {
				materialsTransform = materialsTransform.WithScalingAdjustedBy(-dt);
				SetTransforms(materialsTransform);
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.C)) cycleColorTex = !cycleColorTex;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.E)) cycleEmissiveTex = !cycleEmissiveTex;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.O)) cycleOrmTex = !cycleOrmTex;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.A)) cycleAtTex = !cycleAtTex;

			foreach (var key in kbm.NewKeyDownEvents) {
				if (key.GetNumericValue() is { } number) {
					if (number < 1) number = 10;
					SetMapDistances(MaterialEffectMapType.Color, number * 0.1f);
					SetMapDistances(MaterialEffectMapType.Emissive, number * 0.1f);
					SetMapDistances(MaterialEffectMapType.OcclusionRoughnessMetallic, number * 0.1f);
					SetMapDistances(MaterialEffectMapType.AbsorptionTransmission, number * 0.1f);
					cycleColorTex = false;
					cycleEmissiveTex = false;
					cycleOrmTex = false;
					cycleAtTex = false;
				}
				else if (key == KeyboardOrMouseKey.Backtick) {
					SetMapDistances(MaterialEffectMapType.Color, 0f);
					SetMapDistances(MaterialEffectMapType.Emissive, 0f);
					SetMapDistances(MaterialEffectMapType.OcclusionRoughnessMetallic, 0f);
					SetMapDistances(MaterialEffectMapType.AbsorptionTransmission, 0f);
					cycleColorTex = false;
					cycleEmissiveTex = false;
					cycleOrmTex = false;
					cycleAtTex = false;
				}
			}

			var sinusoidalBlendDist = Math.Clamp(MathF.Sin(tt), -0.5f, 0.5f) + 0.5f;
			if (cycleColorTex) {
				SetMapDistances(MaterialEffectMapType.Color, sinusoidalBlendDist);
			}
			if (cycleEmissiveTex) {
				SetMapDistances(MaterialEffectMapType.Emissive, sinusoidalBlendDist);
			}
			if (cycleOrmTex) {
				SetMapDistances(MaterialEffectMapType.OcclusionRoughnessMetallic, sinusoidalBlendDist);
			}
			if (cycleAtTex) {
				SetMapDistances(MaterialEffectMapType.AbsorptionTransmission, sinusoidalBlendDist);
			}

			renderer.Render();
		}
	}
}