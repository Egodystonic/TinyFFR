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
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMaterialsTest {
	const KeyboardOrMouseKey BackdropToggleKey = KeyboardOrMouseKey.B;
	const KeyboardOrMouseKey RotationToggleKey = KeyboardOrMouseKey.R;

	const KeyboardOrMouseKey MapAlphaToggleKey = KeyboardOrMouseKey.A;
	const KeyboardOrMouseKey MapEmissiveToggleKey = KeyboardOrMouseKey.E;
	const KeyboardOrMouseKey MapNormalToggleKey = KeyboardOrMouseKey.N;
	const KeyboardOrMouseKey MapOrmrToggleKey = KeyboardOrMouseKey.O;
	const KeyboardOrMouseKey MapAnisotropicToggleKey = KeyboardOrMouseKey.T;
	const KeyboardOrMouseKey MapClearCoatToggleKey = KeyboardOrMouseKey.C;
	const KeyboardOrMouseKey MapThicknessToggleKey = KeyboardOrMouseKey.K;
	const KeyboardOrMouseKey QualityToggleKey = KeyboardOrMouseKey.Q;

	const string WindowTitleStart = $"settings: B,R,Q shaders: 1-9 maps: A,E,N,O,T,C,K";

	sealed record UserOptions {
		public int BackdropIntensity { get; set; } = 2;
		public bool Rotate { get; set; } = true;

		public int MapAlphaType { get; set; } = 0;
		public bool MapEmissive { get; set; } = false;
		public bool MapNormal { get; set; } = false;
		public int MapOrmrType { get; set; } = 0;
		public bool MapAnisotropic { get; set; } = false;
		public int MapClearCoatType { get; set; } = 0;
		public int MapThicknessLevel { get; set; } = 0;

		public int ShaderType { get; set; } = 8;
		public int ShaderQualityType { get; set; } = 1;

		public string GetWindowTitleString() {
			var mapsStr = "";
			if (ShaderQualityType == 0) mapsStr += " qual=v_high";
			if (ShaderQualityType == 1) mapsStr += " qual=standard";
			if (ShaderQualityType == 2) mapsStr += " qual=v_low";
			if (ShaderType < 4) {
				if (MapAlphaType == 1) mapsStr += " alpha(mask)";
				if (MapAlphaType == 2) mapsStr += " alpha(blend)";
				if (MapEmissive) mapsStr += " emiss";
				if (MapNormal) mapsStr += " norm";
				if (MapOrmrType == 1) mapsStr += " orm";
				if (MapOrmrType == 2) mapsStr += " ormr";
				if (MapAnisotropic) mapsStr += " aniso";
				if (MapClearCoatType == 1) mapsStr += " ccoat(thin/smooth)";
				if (MapClearCoatType == 2) mapsStr += " ccoat(thick/smooth)";
				if (MapClearCoatType == 3) mapsStr += " ccoat(thin/rough)";
				if (MapClearCoatType == 4) mapsStr += " ccoat(thick/rough)";
				if (MapThicknessLevel == 0) mapsStr += " thick=0.01";
				if (MapThicknessLevel == 1) mapsStr += " thick=0.1";
				if (MapThicknessLevel == 2) mapsStr += " thick=0.5";
				if (MapThicknessLevel == 3) mapsStr += " thick=1";
			}
			
			return " || " + ShaderType switch {
				1 => "SIMPLE",
				3 => "TRANSMISSIVE",
				4 => "ANISOMETAL",
				5 => "HEXNORM",
				6 => "GLASS",
				7 => "MIRROR",
				8 => "TEST",
				9 => "STAINEDGLASS",
				_ => "STANDARD"
			} + mapsStr;
		}
	}

	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		var curUserOptions = new UserOptions();

		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(
			display, 
			title: WindowTitleStart + curUserOptions.GetWindowTitleString()
		);

		using var backdrop = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere, subdivisionLevel: 7);

		using var camera = factory.CameraBuilder.CreateCamera();
		using var light = factory.LightBuilder.CreatePointLight(position: (0f, 0f, 1f), castsShadows: true, brightness: 0.5f);
		using var leftLight = factory.LightBuilder.CreatePointLight(position: (2.6f, 0f, 1f), color: ColorVect.RandomOpaque(), castsShadows: true);
		using var rightLight = factory.LightBuilder.CreatePointLight(position: (-2.6f, 0f, 1f), color: ColorVect.RandomOpaque(), castsShadows: true);
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		scene.Add(light);
		scene.Add(leftLight);
		scene.Add(rightLight);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var backMaterialAlbedo = factory.AssetLoader.LoadColorMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex));
		using var backMaterialNormals = factory.AssetLoader.LoadNormalMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex));
		using var backMaterialOrm = factory.AssetLoader.LoadOcclusionRoughnessMetallicMap(CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex));
		using var backInstanceMaterial = factory.MaterialBuilder.CreateStandardMaterial(backMaterialAlbedo, backMaterialNormals, backMaterialOrm);

		var currentMaterialResources = CreateTestMaterial(factory.ResourceAllocator, factory.MaterialBuilder);

		var cubeFrontInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, currentMaterialResources.Materials[0], new Location(1f, 0f, 2f));
		var sphereFrontInstance = factory.ObjectBuilder.CreateModelInstance(sphereMesh, currentMaterialResources.Materials[0], new Location(-1f, 0f, 2f));
		var cubeBackInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, backInstanceMaterial, new Location(1.8f, 0f, 3.5f));
		var sphereBackInstance = factory.ObjectBuilder.CreateModelInstance(sphereMesh, backInstanceMaterial, new Location(-1.8f, 0f, 3.5f));
		scene.Add(cubeFrontInstance);
		scene.Add(sphereFrontInstance);
		scene.Add(cubeBackInstance);
		scene.Add(sphereBackInstance);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		void RecreateMaterial() {
			ResourceGroup newMaterialResources;

			switch (curUserOptions.ShaderType) {
				case 1:
					newMaterialResources = CreateSimpleMaterial(
						factory.ResourceAllocator,
						factory.TextureBuilder,
						factory.MaterialBuilder,
						curUserOptions.MapAlphaType != 0
					);
					break;
				case 3:
					newMaterialResources = CreateTransmissiveMaterial(
						factory.ResourceAllocator,
						factory.TextureBuilder,
						factory.MaterialBuilder,
						curUserOptions.ShaderQualityType switch { 1 => TransmissiveMaterialQuality.SkyboxOnlyReflectionsAndRefraction, _ => TransmissiveMaterialQuality.FullReflectionsAndRefraction },
						curUserOptions.MapAlphaType switch { 1 => TransmissiveMaterialAlphaMode.MaskOnly, 2 => TransmissiveMaterialAlphaMode.FullBlending, _ => null },
						curUserOptions.MapEmissive,
						curUserOptions.MapNormal,
						curUserOptions.MapOrmrType > 0,
						curUserOptions.MapAnisotropic,
						curUserOptions.MapThicknessLevel switch { 1 => 0.1f, 2 => 0.5f, 3 => 1f, _ => 0.01f }
					);
					break;
				case 4:
					newMaterialResources = LoadAnisoMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.MaterialBuilder);
					break;
				case 5:
					newMaterialResources = LoadHexNormMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.MaterialBuilder);
					break;
				case 6:
					newMaterialResources = CreateGlassMaterial(factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder);
					break;
				case 7:
					newMaterialResources = CreateMirrorMaterial(factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder);
					break;
				case 8:
					newMaterialResources = CreateTestMaterial(factory.ResourceAllocator, factory.MaterialBuilder);
					break;
				case 9:
					newMaterialResources = CreateStainedGlassMaterial(factory.ResourceAllocator, factory.AssetLoader, factory.TextureBuilder, factory.MaterialBuilder);
					break;
				default:
					newMaterialResources = CreateStandardMaterial(
						factory.ResourceAllocator,
						factory.TextureBuilder,
						factory.MaterialBuilder,
						curUserOptions.MapAlphaType switch { 1 => StandardMaterialAlphaMode.MaskOnly, 2 => StandardMaterialAlphaMode.FullBlending, _ => null },
						curUserOptions.MapEmissive,
						curUserOptions.MapNormal,
						curUserOptions.MapOrmrType > 0,
						curUserOptions.MapOrmrType > 1,
						curUserOptions.MapAnisotropic,
						curUserOptions.MapClearCoatType
					);
					break;
			}

			cubeFrontInstance.Material = newMaterialResources.Materials[0];
			sphereFrontInstance.Material = newMaterialResources.Materials[0];

			currentMaterialResources.Dispose();
			currentMaterialResources = newMaterialResources;

			window.SetTitle(WindowTitleStart + curUserOptions.GetWindowTitleString());
		}
		void HandleMapAndShaderToggles(ILatestKeyboardAndMouseInputRetriever kbm) {
			var recreationNecessary = false;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				curUserOptions.ShaderType = 1;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				curUserOptions.ShaderType = 2;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) {
				curUserOptions.ShaderType = 3;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow4)) {
				curUserOptions.ShaderType = 4;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow5)) {
				curUserOptions.ShaderType = 5;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow6)) {
				curUserOptions.ShaderType = 6;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow7)) {
				curUserOptions.ShaderType = 7;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow8)) {
				curUserOptions.ShaderType = 8;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow9)) {
				curUserOptions.ShaderType = 9;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapAlphaToggleKey)) {
				curUserOptions.MapAlphaType++;
				if (curUserOptions.MapAlphaType > 2) curUserOptions.MapAlphaType = 0;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapEmissiveToggleKey)) {
				curUserOptions.MapEmissive = !curUserOptions.MapEmissive;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapNormalToggleKey)) {
				curUserOptions.MapNormal = !curUserOptions.MapNormal;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapOrmrToggleKey)) {
				curUserOptions.MapOrmrType++;
				if (curUserOptions.MapOrmrType > 2) curUserOptions.MapOrmrType = 0;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapAnisotropicToggleKey)) {
				curUserOptions.MapAnisotropic = !curUserOptions.MapAnisotropic;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapClearCoatToggleKey)) {
				curUserOptions.MapClearCoatType++;
				if (curUserOptions.MapClearCoatType > 4) curUserOptions.MapClearCoatType = 0;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapThicknessToggleKey)) {
				curUserOptions.MapThicknessLevel++;
				if (curUserOptions.MapThicknessLevel > 3) curUserOptions.MapThicknessLevel = 0;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(QualityToggleKey)) {
				curUserOptions.ShaderQualityType++;
				if (curUserOptions.ShaderQualityType > 2) curUserOptions.ShaderQualityType = 0;
				renderer.SetQuality(curUserOptions.ShaderQualityType switch {
					2 => new RenderQualityConfig(Quality.VeryLow),
					1 => new RenderQualityConfig(Quality.Standard),
					_ => new RenderQualityConfig(Quality.VeryHigh)
				});
				recreationNecessary = true;
			}

			if (recreationNecessary) RecreateMaterial();
		}
		void HandleSettingsToggles(ILatestKeyboardAndMouseInputRetriever kbm) {
			if (kbm.KeyWasPressedThisIteration(BackdropToggleKey)) {
				curUserOptions.BackdropIntensity--;
				if (curUserOptions.BackdropIntensity < 0) curUserOptions.BackdropIntensity = 2;
				switch (curUserOptions.BackdropIntensity) {
					case 0:
						scene.RemoveBackdrop();
						break;
					case 1:
						scene.SetBackdrop(backdrop, backdropIntensity: 0.5f);
						break;
					case 2:
						scene.SetBackdrop(backdrop, backdropIntensity: 1f);
						break;
				}
			}
			if (kbm.KeyWasPressedThisIteration(RotationToggleKey)) {
				curUserOptions.Rotate = !curUserOptions.Rotate;
			}
		}

		try {
			while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
				var dt = (float) loop.IterateOnce().TotalSeconds;
				var tt = (float) loop.TotalIteratedTime.TotalSeconds;
			
				var kbm = loop.Input.KeyboardAndMouse;
				HandleMapAndShaderToggles(kbm);
				HandleSettingsToggles(kbm);

				if (curUserOptions.Rotate) {
					cubeFrontInstance.RotateBy(dt * 30f % Direction.Up);
					sphereFrontInstance.RotateBy(dt * 30f % Direction.Down);
					cubeBackInstance.RotateBy(dt * -20f % Direction.Up);
					sphereBackInstance.RotateBy(dt * -20f % Direction.Down);
				}

				light.Position = light.Position with { Y = MathF.Sin(tt) };
				leftLight.Position = leftLight.Position with { Y = MathF.Sin(tt) };
				rightLight.Position = rightLight.Position with { Y = MathF.Sin(tt) };

				renderer.Render();
			}
		}
		finally {
			scene.Remove(cubeFrontInstance);
			scene.Remove(sphereFrontInstance);
			scene.Remove(cubeBackInstance);
			scene.Remove(sphereBackInstance);
			cubeFrontInstance.Dispose();
			sphereFrontInstance.Dispose();
			cubeBackInstance.Dispose();
			sphereBackInstance.Dispose();
			currentMaterialResources.Dispose();
		}
	}

	ResourceGroup CreateSimpleMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder, bool includeAlpha) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Simple Material Resources"
		);

		Texture colorMap;
		
		if (includeAlpha) {
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
				name: "Simple Material Color Map"
			);
		}
		else {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.Lines(
					new ColorVect(1f, 0f, 0f),
					new ColorVect(0f, 1f, 0f),
					new ColorVect(0f, 0f, 1f),
					new ColorVect(1f, 1f, 1f),
					horizontal: false,
					numRepeats: 4,
					perturbationMagnitude: 0.3f
				),
				false,
				name: "Simple Material Color Map"
			);
		}
		result.Add(colorMap);

		var matConfig = new SimpleMaterialCreationConfig {
			ColorMap = colorMap,
			Name = "Simple Material"
		};
		var mat = matBuilder.CreateSimpleMaterial(matConfig);
		result.Add(mat);

		return result;
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

	ResourceGroup CreateTransmissiveMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder, TransmissiveMaterialQuality quality, TransmissiveMaterialAlphaMode? alphaMode, bool emissive, bool norm, bool ormr, bool aniso, float thickness) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Transmissive Material Resources"
		);

		Texture colorMap;
		Texture atMap;
		Texture? emissiveMap = null;
		Texture? normalMap = null;
		Texture? ormrMap = null;
		Texture? anisotropyMap = null;

		if (alphaMode == null) {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.PlainFill(
					new ColorVect(1f, 1f, 1f, 1f)
				),
				false,
				name: "Transmissive Material Color Map"
			);
		}
		else if (alphaMode == TransmissiveMaterialAlphaMode.FullBlending) {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.Lines(
					new ColorVect(1f, 1f, 1f, 1f).WithPremultipliedAlpha(),
					new ColorVect(1f, 1f, 1f, 0.5f).WithPremultipliedAlpha(),
					new ColorVect(1f, 1f, 1f, 1f).WithPremultipliedAlpha(),
					new ColorVect(1f, 1f, 1f, 0f).WithPremultipliedAlpha(),
					horizontal: false,
					numRepeats: 4
				),
				true,
				name: "Transmissive Material Color Map"
			);
		}
		else {
			colorMap = texBuilder.CreateColorMap(
				TexturePattern.Lines(
					new ColorVect(1f, 1f, 1f, 1f),
					new ColorVect(1f, 1f, 1f, 0.5f),
					new ColorVect(1f, 1f, 1f, 1f),
					new ColorVect(1f, 1f, 1f, 0f),
					horizontal: false,
					numRepeats: 4
				),
				true,
				name: "Transmissive Material Color Map"
			);
		}
		result.Add(colorMap);

		atMap = texBuilder.CreateAbsorptionTransmissionMap(
			TexturePattern.Lines(
				new ColorVect(0f, 1f, 1f),
				new ColorVect(1f, 0f, 1f),
				new ColorVect(1f, 1f, 0f),
				horizontal: true,
				numRepeats: 1
			),
			TexturePattern.Lines<Real>(
				0.7f,
				0.2f,
				horizontal: true,
				numRepeats: 3
			)
		);
		result.Add(atMap);

		if (emissive) {
			emissiveMap = texBuilder.CreateEmissiveMap(
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
				),
				name: "Transmissive Material Emissive Map"
			);
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
				name: "Transmissive Material Normal Map"
			);
			result.Add(normalMap.Value);
		}

		if (ormr) {
			ormrMap = texBuilder.CreateOcclusionRoughnessMetallicReflectanceMap(
				TexturePattern.PlainFill<Real>(1f),
				TexturePattern.Circles<Real>(0.8f, 1f, 0f, paddingSize: (192, 192), repetitions: (1, 1)),
				TexturePattern.Lines<Real>(0f, 1f, 0f, 1f, 0f, numRepeats: 1, horizontal: false),
				TexturePattern.Lines<Real>(1f, 0f, 0f, 1f, 0f, numRepeats: 1, horizontal: false),
				name: "Transmissive Material ORMR Map"
			);
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
					numRepeats: 4
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
					numRepeats: 2
				),
				name: "Transmissive Material Anisotropy Map"
			);
		}

		var matConfig = new TransmissiveMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
			NormalMap = normalMap,
			AlphaMode = alphaMode ?? TransmissiveMaterialAlphaMode.MaskOnly,
			OcclusionRoughnessMetallicReflectanceMap = ormrMap,
			AnisotropyMap = anisotropyMap,
			AbsorptionTransmissionMap = atMap,
			RefractionThickness = thickness,
			Quality = quality,
			Name = "Transmissive Material"
		};
		var mat = matBuilder.CreateTransmissiveMaterial(matConfig);
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

		var albedo = texBuilder.CreateColorMap(ColorVect.White, includeAlpha: false);
		var at = texBuilder.CreateAbsorptionTransmissionMap(ColorVect.Black, transmission: 1f);
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

		var albedo = texBuilder.CreateColorMap(ColorVect.White, includeAlpha: false);
		var at = texBuilder.CreateAbsorptionTransmissionMap(ColorVect.White, transmission: 0f);
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

	ResourceGroup CreateTestMaterial(IResourceAllocator resAllocator, IMaterialBuilder matBuilder) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Test Material Resources"
		);

		var mat = matBuilder.CreateTestMaterial(ignoresLighting: true);
		result.Add(mat);

		return result;
	}
}