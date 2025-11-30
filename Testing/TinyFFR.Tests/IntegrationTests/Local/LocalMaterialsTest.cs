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
class LocalMaterialsTest {
	const KeyboardOrMouseKey BackdropToggleKey = KeyboardOrMouseKey.B;
	const KeyboardOrMouseKey RotationToggleKey = KeyboardOrMouseKey.R;

	const KeyboardOrMouseKey MapAlphaToggleKey = KeyboardOrMouseKey.A;
	const KeyboardOrMouseKey MapEmissiveToggleKey = KeyboardOrMouseKey.E;
	const KeyboardOrMouseKey MapNormalToggleKey = KeyboardOrMouseKey.N;
	const KeyboardOrMouseKey MapOrmrToggleKey = KeyboardOrMouseKey.O;
	const KeyboardOrMouseKey MapAnisotropicToggleKey = KeyboardOrMouseKey.T;
	const KeyboardOrMouseKey MapClearCoatToggleKey = KeyboardOrMouseKey.C;

	const string WindowTitleStart = $"settings: B,R shaders: 1,2,3 maps: A,E,N,O,T,C";

	sealed record UserOptions {
		public int BackdropIntensity { get; set; } = 2;
		public bool Rotate { get; set; } = true;

		public int MapAlphaType { get; set; } = 0;
		public bool MapEmissive { get; set; } = false;
		public bool MapNormal { get; set; } = false;
		public int MapOrmrType { get; set; } = 0;
		public bool MapAnisotropic { get; set; } = false;
		public int MapClearCoatType { get; set; } = 0;

		public int ShaderType { get; set; } = 1;

		public string GetWindowTitleString() {
			var mapsStr = "";
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

			return " || " + ShaderType switch {
				1 => "SIMPLE",
				3 => "TRANSMISSIVE",
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

		using var backdrop = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere, subdivisionLevel: 1);

		using var camera = factory.CameraBuilder.CreateCamera();
		using var light = factory.LightBuilder.CreatePointLight(position: (0f, 0f, 1f));
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		scene.Add(light);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		var currentMaterialResources = CreateSimpleMaterial(factory.ResourceAllocator, factory.TextureBuilder, factory.MaterialBuilder, includeAlpha: false, emissive: false);

		var cubeInstance = factory.ObjectBuilder.CreateModelInstance(cubeMesh, currentMaterialResources.Materials[0], new Location(1f, 0f, 2f));
		var sphereInstance = factory.ObjectBuilder.CreateModelInstance(sphereMesh, currentMaterialResources.Materials[0], new Location(-1f, 0f, 2f));
		scene.Add(cubeInstance);
		scene.Add(sphereInstance);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		void RecreateMaterial() {
			ResourceGroup newMaterialResources;

			switch (curUserOptions.ShaderType) {
				case 1:
					newMaterialResources = CreateSimpleMaterial(
						factory.ResourceAllocator,
						factory.TextureBuilder,
						factory.MaterialBuilder,
						curUserOptions.MapAlphaType != 0,
						curUserOptions.MapEmissive
					);
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
			
			cubeInstance.Material = newMaterialResources.Materials[0];
			sphereInstance.Material = newMaterialResources.Materials[0];

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
					cubeInstance.RotateBy(dt * 30f % Direction.Up);
					sphereInstance.RotateBy(dt * 30f % Direction.Down);
				}

				light.Position = light.Position with { Y = MathF.Sin(tt) };

				renderer.Render();
			}
		}
		finally {
			scene.Remove(cubeInstance);
			scene.Remove(sphereInstance);
			cubeInstance.Dispose();
			sphereInstance.Dispose();
			currentMaterialResources.Dispose();
		}
	}

	ResourceGroup CreateSimpleMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder, bool includeAlpha, bool emissive) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Simple Material Resources"
		);

		Texture colorMap;
		Texture? emissiveMap = null;
		
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

		if (emissive) {
			emissiveMap = texBuilder.CreateEmissiveMap(
				TexturePattern.Rectangles(
					interiorValue: ColorVect.FromStandardColor(StandardColor.LightingCandle),
					borderValue: ColorVect.FromStandardColor(StandardColor.Lime),
					paddingValue: ColorVect.Black,
					repetitions: (3, 3)
				),
				TexturePattern.Rectangles<Real>(
					interiorValue: 0.5f,
					borderValue: 1f,
					paddingValue: 0f,
					repetitions: (3, 3)
				),
				name: "Simple Material Emissive Map"
			);
			result.Add(emissiveMap.Value);
		}

		var matConfig = new SimpleMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
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
					interiorValue: ColorVect.FromStandardColor(StandardColor.LightingCandle),
					borderValue: ColorVect.FromStandardColor(StandardColor.Lime),
					paddingValue: ColorVect.Black,
					repetitions: (5, 5)
				),
				TexturePattern.Rectangles<Real>(
					interiorValue: 0.5f,
					borderValue: 1f,
					paddingValue: 0f,
					repetitions: (5, 5)
				),
				name: "Standard Material Emissive Map"
			);
			result.Add(emissiveMap.Value);
		}

		if (norm) {
			normalMap = texBuilder.CreateNormalMap(
				TexturePattern.Rectangles(
					interiorSize: new XYPair<int>(256, 256),
					borderSize: new XYPair<int>(16, 16),
					paddingSize: new XYPair<int>(64, 64),
					interiorValue: UnitSphericalCoordinate.ZeroZero,
					paddingValue: UnitSphericalCoordinate.ZeroZero,
					borderRightValue: new UnitSphericalCoordinate(0f, 45f),
					borderTopValue: new UnitSphericalCoordinate(90f, 45f),
					borderLeftValue: new UnitSphericalCoordinate(180f, 45f),
					borderBottomValue: new UnitSphericalCoordinate(270f, 45f),
					repetitions: (2, 2)
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
					Angle.From2DPolarAngle(Orientation2D.Left)!.Value,
					Angle.From2DPolarAngle(Orientation2D.Down)!.Value,
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
}