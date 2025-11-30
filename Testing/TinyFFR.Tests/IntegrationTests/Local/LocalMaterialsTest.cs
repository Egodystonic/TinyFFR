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

	sealed record UserOptions {
		public int BackdropIntensity { get; set; } = 2;
		public bool Rotate { get; set; } = true;

		public bool MapAlpha { get; set; } = false;
		public bool MapEmissive { get; set; } = false;

		public int ShaderType { get; set; } = 1;
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
			title: $"settings: {BackdropToggleKey},{RotationToggleKey} " +
				   $"shaders: 1,2,3 " +
				   $"maps: {MapAlphaToggleKey},{MapEmissiveToggleKey}"
		);

		using var backdrop = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere, subdivisionLevel: 1);

		using var camera = factory.CameraBuilder.CreateCamera();
		using var light = factory.LightBuilder.CreateSpotLight(highQuality: true);
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
						curUserOptions.MapAlpha,
						curUserOptions.MapEmissive
					);
					break;
				default:
					newMaterialResources = CreateStandardMaterial(
						factory.ResourceAllocator,
						factory.TextureBuilder,
						factory.MaterialBuilder,
						curUserOptions.MapAlpha,
						curUserOptions.MapEmissive
					);
					break;
			}
			

			cubeInstance.Material = newMaterialResources.Materials[0];
			sphereInstance.Material = newMaterialResources.Materials[0];

			currentMaterialResources.Dispose();
			currentMaterialResources = newMaterialResources;
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
				curUserOptions.MapAlpha = !curUserOptions.MapAlpha;
				recreationNecessary = true;
			}
			if (kbm.KeyWasPressedThisIteration(MapEmissiveToggleKey)) {
				curUserOptions.MapEmissive = !curUserOptions.MapEmissive;
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
					cubeInstance.RotateBy(dt * 90f % Direction.Up);
					sphereInstance.RotateBy(dt * 90f % Direction.Down);
				}

				light.Position = new Location(MathF.Sin(tt) * 2f, 0f, 0f);
				light.ConeDirection = light.Position.DirectionTo(new Location(0f, 0f, 1f));

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
			includeAlpha,
			"Simple Material Color Map"
		);
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

	ResourceGroup CreateStandardMaterial(IResourceAllocator resAllocator, ITextureBuilder texBuilder, IMaterialBuilder matBuilder, bool includeAlpha, bool emissive) {
		var result = resAllocator.CreateResourceGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: "Standard Material Resources"
		);

		Texture colorMap;
		Texture? emissiveMap = null;

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
			includeAlpha,
			" Material Color Map"
		);
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

		var matConfig = new StandardMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
			Name = "Standard Material"
		};
		var mat = matBuilder.CreateStandardMaterial(matConfig);
		result.Add(mat);

		return result;
	}
}