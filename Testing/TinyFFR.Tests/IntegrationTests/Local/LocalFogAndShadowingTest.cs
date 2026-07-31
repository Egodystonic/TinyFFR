// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
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
class LocalFogAndShadowingTest {
	enum CameraLightMode { None, Point, Spot }

	static readonly FogDensity[] FogCycle = {
		FogDensity.VeryThin, FogDensity.Thin, FogDensity.Moderate, FogDensity.Thick, FogDensity.VeryThick
	};

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Fog And Shadowing Test");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 1.7f, -8f));
		using var cameraController = camera.CreateController<FreeFlyingCameraController>();

		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var resGroup = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, name: "Fog Test Resources");

		// --- Shared brick material (used by columns and as a floor tile material) ---
		var brickAlbedo = factory.AssetLoader.LoadColorMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex));
		var brickNormal = factory.AssetLoader.LoadNormalMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex));
		var brickOrm = factory.AssetLoader.LoadOcclusionRoughnessMetallicMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex));
		var brickMaterial = factory.MaterialBuilder.CreateStandardMaterial(brickAlbedo, brickNormal, brickOrm, name: "Brick");
		resGroup.Add(brickAlbedo);
		resGroup.Add(brickNormal);
		resGroup.Add(brickOrm);
		resGroup.Add(brickMaterial);

		// --- Floor tile material palette (six 'fancy' materials copied from LocalMaterialsTest) ---
		using var stainedGlassRes = CreateStainedGlassMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.TextureBuilder, factory.MaterialBuilder);
		using var mirrorRes = CreateMirrorMaterial(factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder);
		using var glassRes = CreateGlassMaterial(factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder);
		using var hexNormRes = LoadHexNormMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.MaterialBuilder);
		using var anisoRes = LoadAnisoMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.MaterialBuilder);
		using var standardRes = CreateStandardMaterial(
			factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder,
			alphaMode: null, emissive: true, norm: true, orm: true, r: true, aniso: true, clearcoatType: 1
		);
		var palette = new List<Material> {
			stainedGlassRes.Materials[0], mirrorRes.Materials[0], glassRes.Materials[0],
			hexNormRes.Materials[0], anisoRes.Materials[0], standardRes.Materials[0]
		};

		// --- Geometry ---
		var floorMesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f, 0.05f, 1f), name: "Floor Tile");
		var columnMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Column");
		resGroup.Add(floorMesh);
		resGroup.Add(columnMesh);

		var instances = new List<ModelInstance>();

		for (var i = 0; i < 10; ++i) {
			for (var j = 0; j < 10; ++j) {
				var mat = palette[(i + j) % palette.Count];
				var tile = factory.ObjectBuilder.CreateModelInstance(floorMesh, mat, initialPosition: new Location(i - 4.5f, 0f, j - 4.5f));
				scene.Add(tile);
				instances.Add(tile);
			}
		}

		var rng = new Random(12345);
		for (var i = 1; i < 10; ++i) {
			for (var j = 1; j < 10; ++j) {
				if (rng.NextSingle() >= 0.2f) continue;
				var height = 0.5f + rng.NextSingle() * 3.5f;
				var hover = rng.NextSingle() < 0.35f ? 0.2f + rng.NextSingle() * 0.8f : 0f;
				var posY = hover + height / 2f;
				var column = factory.ObjectBuilder.CreateModelInstance(
					columnMesh, brickMaterial,
					initialPosition: new Location(i - 5f, posY, j - 5f),
					initialScaling: new Vect(0.3f, height, 0.3f)
				);
				scene.Add(column);
				instances.Add(column);
			}
		}

		// --- Perimeter wall (2m high brick, around the 10x10 floor) ---
		const float wallHeight = 1f;
		const float wallThickness = 0.3f;
		const float wallLength = 10f;
		var wallSpecs = new (Location Position, Vect Scaling)[] {
			(new Location(0f, wallHeight / 2f, 5f), new Vect(wallLength, wallHeight, wallThickness)),
			(new Location(0f, wallHeight / 2f, -5f), new Vect(wallLength, wallHeight, wallThickness)),
			(new Location(5f, wallHeight / 2f, 0f), new Vect(wallThickness, wallHeight, wallLength)),
			(new Location(-5f, wallHeight / 2f, 0f), new Vect(wallThickness, wallHeight, wallLength)),
		};
		foreach (var (position, scaling) in wallSpecs) {
			var wall = factory.ObjectBuilder.CreateModelInstance(
				columnMesh, brickMaterial,
				initialPosition: position,
				initialScaling: scaling
			);
			scene.Add(wall);
			instances.Add(wall);
		}

		// --- Lights ---
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(
			direction: new Direction(-0.3f, -1f, 0.2f),
			color: StandardColor.LightingSunRiseSet,
			showSunDisc: true,
			castsShadows: true
		);
		scene.Add(sunlight);
		var sunEnabled = true;

		var sunElevations = new[] { 88f, 60f, 20f, 5f };
		var sunElevationIndex = 0;
		var sunAzimuthDeg = 0f;

		var cameraLightMode = CameraLightMode.None;
		PointLight? droppedPoint = null;
		SpotLight? droppedSpot = null;
		void ClearDroppedLights() {
			if (droppedPoint is { } p) { scene.Remove(p); p.Dispose(); droppedPoint = null; }
			if (droppedSpot is { } s) { scene.Remove(s); s.Dispose(); droppedSpot = null; }
		}

		// --- Quality (Ultra base, cyclable shadow quality) ---
		var shadowQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Ultra).ShadowQuality;
		RenderQualityConfig BuildConfig() => new RenderQualityConfig(BuiltInQualityConfiguration.Ultra) { ShadowQuality = shadowQuality };
		static Quality CycleQuality(Quality q) => q >= Quality.VeryHigh ? Quality.VeryLow : (Quality) ((int) q + 1);
		renderer.SetQuality(BuildConfig());

		var fogIndex = -1; // -1 == off

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		try {
			while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
				var dt = loop.IterateOnce().AsDeltaTime();
				var kbm = loop.Input.KeyboardAndMouse;

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
					sunEnabled = !sunEnabled;
					if (sunEnabled) scene.Add(sunlight);
					else scene.Remove(sunlight);
				}

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
					sunElevationIndex = (sunElevationIndex + 1) % sunElevations.Length;
				}

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.F)) {
					fogIndex++;
					if (fogIndex >= FogCycle.Length) fogIndex = -1;
					if (fogIndex < 0) scene.RemoveFog();
					else scene.AddFog(FogCycle[fogIndex]);
				}

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.G)) {
					shadowQuality = CycleQuality(shadowQuality);
					renderer.SetQuality(BuildConfig());
				}

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.L)) {
					ClearDroppedLights();
					cameraLightMode = (CameraLightMode) (((int) cameraLightMode + 1) % 3);
					if (cameraLightMode == CameraLightMode.Point) {
						droppedPoint = factory.LightBuilder.CreatePointLight(position: camera.Position, castsShadows: true);
						scene.Add(droppedPoint.Value);
					}
					else if (cameraLightMode == CameraLightMode.Spot) {
						droppedSpot = factory.LightBuilder.CreateSpotLight(
							position: camera.Position,
							coneDirection: camera.ViewDirection,
							coneAngle: 60f,
							castsShadows: true
						);
						scene.Add(droppedSpot.Value);
					}
				}

				if (sunEnabled) {
					sunAzimuthDeg += 8f * dt;
					var e = sunElevations[sunElevationIndex] * MathF.PI / 180f;
					var a = sunAzimuthDeg * MathF.PI / 180f;
					sunlight.Direction = new Direction(-MathF.Cos(e) * MathF.Cos(a), -MathF.Sin(e), -MathF.Cos(e) * MathF.Sin(a));
				}

				window.SetTitle(
					$"[1]Sun:{sunEnabled} [2]Elev:{sunElevations[sunElevationIndex]:0}° [L]Light:{cameraLightMode} [G]ShadowQ:{shadowQuality} " +
					$"[F]Fog:{(fogIndex < 0 ? "Off" : FogCycle[fogIndex].ToString())} | " +
					$"{loop.FramesPerSecondRecentAverage:0000} FPS"
				);

				DefaultCameraInputHandler.TickKbm(kbm, cameraController, dt, window);
				DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, cameraController, dt);
				DefaultCameraInputHandler.Progress(cameraController, dt);

				renderer.Render();
			}
		}
		finally {
			ClearDroppedLights();
			scene.RemoveAll();
			foreach (var instance in instances) instance.Dispose();
		}
	}

	ResourceGroup CreateStandardMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder, StandardMaterialAlphaMode? alphaMode, bool emissive, bool norm, bool orm, bool r, bool aniso, int clearcoatType) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Standard Material Resources"
		);

		Texture colorMap;
		Texture? emissiveMap = null;
		Texture? normalMap = null;
		Texture? ormrMap = null;
		Texture? anisotropyMap = null;
		Texture? clearcoatMap = null;

		if (alphaMode == StandardMaterialAlphaMode.FullBlending) {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.Lines(
					new ColorVect(1f, 0f, 0f, 0.5f).WithPremultipliedAlpha(),
					new ColorVect(0f, 1f, 0f, 1f).WithPremultipliedAlpha(),
					new ColorVect(0f, 0f, 1f, 0.5f).WithPremultipliedAlpha(),
					new ColorVect(1f, 1f, 1f, 0f).WithPremultipliedAlpha(),
					horizontal: false,
					numRepeats: 4,
					perturbationMagnitude: 0.3f
				),
				true,
				name: "Standard Material Color Map"
			);
		}
		else {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.Lines(
					new ColorVect(1f, 0f, 0f, 0.5f),
					new ColorVect(0f, 1f, 0f, 1f),
					new ColorVect(0f, 0f, 1f, 0.5f),
					new ColorVect(1f, 1f, 1f, 0f),
					horizontal: false,
					numRepeats: 4,
					perturbationMagnitude: 0.3f
				),
				alphaMode != null,
				name: "Stamdard Material Color Map"
			);
		}

		result.Add(colorMap);

		if (emissive) {
			emissiveMap = texBuilder.CreateEmissiveMap(
				TexturePattern.Rectangles(
					interiorSize: TexturePatternDefaultValues.RectanglesDefaultInteriorSize,
					borderSize: new XYPair<int>(16, 16),
					paddingSize: TexturePatternDefaultValues.RectanglesDefaultPaddingSize,
					interiorValue: ColorVect.WhiteOpaque,
					borderRightValue: new ColorVect(1f, 0f, 0f),
					borderTopValue: new ColorVect(1f, 1f, 0f),
					borderLeftValue: new ColorVect(0f, 1f, 0f),
					borderBottomValue: new ColorVect(0f, 0f, 1f),
					paddingValue: ColorVect.BlackOpaque,
					repetitions: (1, 1)
				),
				TexturePattern.Rectangles<Real>(
					interiorValue: 0f,
					borderValue: 1f,
					paddingValue: 0f,
					repetitions: (1, 1),
					borderSize: (16, 16)
				),
				name: "Standard Material Emissive Map"
			);
			result.Add(emissiveMap.Value);
		}

		if (norm) {
			normalMap = texBuilder.CreateNormalMap(
				TexturePattern.Rectangles(
					interiorSize: new XYPair<int>(24, 24),
					borderSize: new XYPair<int>(8, 8),
					paddingSize: new XYPair<int>(4, 4),
					interiorValue: SphericalTranslation.ZeroZero,
					paddingValue: SphericalTranslation.ZeroZero,
					borderRightValue: new SphericalTranslation(0f, 45f),
					borderTopValue: new SphericalTranslation(90f, 45f),
					borderLeftValue: new SphericalTranslation(180f, 45f),
					borderBottomValue: new SphericalTranslation(270f, 45f),
					repetitions: (12, 12)
				),
				name: "Standard Material Normal Map"
			);
			result.Add(normalMap.Value);
		}

		if (orm) {
			if (r) {
				ormrMap = texBuilder.CreateOcclusionRoughnessMetallicMap(
					TexturePattern.ChequerboardBordered<Real>(1f, 64, 0f, cellResolution: 12),
					TexturePattern.Lines<Real>(0f, 0.25f, 0.5f, 0.75f, 1f, horizontal: true),
					TexturePattern.Lines<Real>(0f, 0.25f, 0.5f, 0.75f, 1f, horizontal: false),
					name: "Standard Material ORM Map"
				);
			}
			else {
				ormrMap = texBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
					TexturePattern.ChequerboardBordered<Real>(1f, 64, 0f, cellResolution: 12),
					TexturePattern.Lines<Real>(0f, 0.25f, 0.5f, 0.75f, 1f, horizontal: true),
					TexturePattern.Lines<Real>(0f, 0.25f, 0.5f, 0.75f, 1f, horizontal: false),
					TexturePattern.Circles<Real>(0.5f, 1f, 0f, repetitions: (1, 1)),
					name: "Standard Material ORMR Map"
				);
			}
			result.Add(ormrMap.Value);
		}

		if (aniso) {
			anisotropyMap = texBuilder.CreateAnisotropyMap(
				TexturePattern.Lines(
					Angle.From2DPolarAngle(Orientation2D.Right)!.Value,
					Angle.From2DPolarAngle(Orientation2D.Up)!.Value,
					Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value,
					Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value,
					horizontal: false,
					numRepeats: 4,
					perturbationMagnitude: 0.3f
				),
				TexturePattern.Lines<Real>(
					1f,
					1f,
					1f,
					1f,
					0f,
					0f,
					0f,
					0f,
					horizontal: false,
					numRepeats: 2,
					perturbationMagnitude: 0.3f
				),
				name: "Standard Material Anisotropy Map"
			);
		}

		if (clearcoatType > 0) {
			clearcoatMap = texBuilder.CreateClearCoatMap(
				clearcoatType % 2 == 1 ? 0.3f : 1f, clearcoatType > 2 ? 1f : 0f, name: "Standard Material Clear"
			);
		}

		var matConfig = new StandardMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
			NormalMap = normalMap,
			AlphaMode = alphaMode ?? StandardMaterialCreationConfig.DefaultAlphaMode,
			OcclusionRoughnessMetallicReflectanceMap = ormrMap,
			AnisotropyMap = anisotropyMap,
			ClearCoatMap = clearcoatMap,
			Name = "Standard Material"
		};
		var mat = matBuilder.CreateStandardMaterial(matConfig);
		result.Add(mat);

		return result;
	}

	ResourceGroup LoadAnisoMaterial(IResourceAllocator resAllocator, IAssetLoader assetLoader, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "AnisoMetal Material Resources"
		);

		var albedo = assetLoader.LoadColorMap(CommonTestAssets.FindAsset("aniso_metal/albedo.jpg"));
		var orm = assetLoader.LoadOcclusionRoughnessMetallicMap(
			CommonTestAssets.FindAsset("aniso_metal/occlusion.jpg"),
			CommonTestAssets.FindAsset("aniso_metal/roughness.jpg"),
			CommonTestAssets.FindAsset("aniso_metal/metallic.jpg")
		);
		var aniso = assetLoader.LoadAnisotropyMapRadialAngleFormatted(
			CommonTestAssets.FindAsset("aniso_metal/aniso_angle.jpg"),
			CommonTestAssets.FindAsset("aniso_metal/aniso_strength.jpg"),
			Orientation2D.Up,
			AnisotropyRadialAngleRange.ZeroTo360,
			encodedAnticlockwise: true
		);

		result.Add(albedo);
		result.Add(orm);
		result.Add(aniso);

		var mat = matBuilder.CreateStandardMaterial(albedo, ormOrOrmrMap: orm, anisotropyMap: aniso);
		result.Add(mat);

		return result;
	}

	ResourceGroup LoadHexNormMaterial(IResourceAllocator resAllocator, IAssetLoader assetLoader, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "HexNorm Material Resources"
		);

		var albedo = assetLoader.LoadColorMap(CommonTestAssets.FindAsset("hex_metal/albedo.jpg"));
		var orm = assetLoader.LoadOcclusionRoughnessMetallicMap(
			CommonTestAssets.FindAsset("hex_metal/occlusion.jpg"),
			CommonTestAssets.FindAsset("hex_metal/roughness.jpg"),
			CommonTestAssets.FindAsset("hex_metal/metallic.jpg")
		);
		var norm = assetLoader.LoadNormalMap(
			CommonTestAssets.FindAsset("hex_metal/norm_dx.png"),
			isDirectXFormat: true
		);

		result.Add(albedo);
		result.Add(orm);
		result.Add(norm);

		var mat = matBuilder.CreateStandardMaterial(albedo, ormOrOrmrMap: orm, normalMap: norm);
		result.Add(mat);

		return result;
	}

	ResourceGroup CreateGlassMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Glass Material Resources"
		);

		var albedo = texBuilder.CreateColorMap(ColorVect.WhiteOpaque, includeAlpha: false);
		var at = texBuilder.CreateAbsorptionTransmissionMap(ColorVect.BlackOpaque, transmission: 1f);
		var ormr = texBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusion: 1f,
			roughness: 0f,
			metallic: 0f,
			reflectance: 0.4f
		);
		var norm = texBuilder.CreateNormalMap(
			TexturePattern.Rectangles(
				interiorSize: new XYPair<int>(24, 24),
				borderSize: new XYPair<int>(8, 8),
				paddingSize: new XYPair<int>(4, 4),
				interiorValue: SphericalTranslation.ZeroZero,
				paddingValue: SphericalTranslation.ZeroZero,
				borderRightValue: new SphericalTranslation(0f, 45f),
				borderTopValue: new SphericalTranslation(90f, 45f),
				borderLeftValue: new SphericalTranslation(180f, 45f),
				borderBottomValue: new SphericalTranslation(270f, 45f),
				repetitions: (12, 12)
			)
		);
		result.Add(norm);

		result.Add(albedo);
		result.Add(at);
		result.Add(ormr);

		var mat = matBuilder.CreateTransmissiveMaterial(
			albedo,
			at,
			quality: TransmissiveMaterialQuality.FullReflectionsAndRefraction,
			ormrMap: ormr,
			normalMap: norm,
			refractionThickness: 0.1f,
			name: "Glass Material"
		);
		result.Add(mat);

		return result;
	}

	ResourceGroup CreateMirrorMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Mirror Material Resources"
		);

		var albedo = texBuilder.CreateColorMap(ColorVect.WhiteOpaque, includeAlpha: false);
		var at = texBuilder.CreateAbsorptionTransmissionMap(ColorVect.WhiteOpaque, transmission: 0f);
		var ormr = texBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusion: 1f,
			roughness: 0f,
			metallic: 1f,
			reflectance: 1f
		);
		var norm = texBuilder.CreateNormalMap(
			TexturePattern.Rectangles(
				interiorSize: new XYPair<int>(24, 24),
				borderSize: new XYPair<int>(8, 8),
				paddingSize: new XYPair<int>(4, 4),
				interiorValue: SphericalTranslation.ZeroZero,
				paddingValue: SphericalTranslation.ZeroZero,
				borderRightValue: new SphericalTranslation(0f, 15f),
				borderTopValue: new SphericalTranslation(90f, 15f),
				borderLeftValue: new SphericalTranslation(180f, 15f),
				borderBottomValue: new SphericalTranslation(270f, 15f),
				repetitions: (12, 12)
			)
		);
		result.Add(norm);

		result.Add(albedo);
		result.Add(at);
		result.Add(ormr);

		var mat = matBuilder.CreateTransmissiveMaterial(
			albedo,
			at,
			quality: TransmissiveMaterialQuality.FullReflectionsAndRefraction,
			ormrMap: ormr,
			normalMap: norm,
			refractionThickness: 0.01f,
			name: "Mirror Material"
		);
		result.Add(mat);

		return result;
	}

	ResourceGroup CreateStainedGlassMaterial(IResourceAllocator resAllocator, IAssetLoader assetLoader, ITextureBuilder texBuilder, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Stained Glass Material Resources"
		);

		var albedo = assetLoader.LoadColorMap(CommonTestAssets.FindAsset("stained_glass/albedo.jpg"));
		var at = assetLoader.LoadAbsorptionTransmissionMap(
			absorptionFilePath: CommonTestAssets.FindAsset("stained_glass/inverted_absorption.jpg"),
			transmissionFilePath: assetLoader.BuiltInTexturePaths.Rgba90Percent,
			invertAbsorption: true
		);
		var ormr = texBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusion: 1f,
			roughness: 0.2f,
			metallic: 0f,
			reflectance: 0.8f
		);
		var norm = assetLoader.LoadNormalMap(CommonTestAssets.FindAsset("stained_glass/normal.jpg"));
		result.Add(albedo);
		result.Add(at);
		result.Add(ormr);
		result.Add(norm);

		var mat = matBuilder.CreateTransmissiveMaterial(
			albedo,
			at,
			quality: TransmissiveMaterialQuality.FullReflectionsAndRefraction,
			ormrMap: ormr,
			normalMap: norm,
			refractionThickness: 0.1f,
			name: "Stained Glass Material"
		);
		result.Add(mat);

		return result;
	}
}
