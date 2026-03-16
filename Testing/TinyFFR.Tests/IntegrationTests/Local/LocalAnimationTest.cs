// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
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
class LocalAnimationTest {
	const float AnimBlendTime = 0.5f;
	(string Filename, float ScalingFactor)[] _filesToLoad;
	
	[SetUp]
	public void SetUpTest() {
		_filesToLoad = new[] {
			("SimpleSkin.gltf", 1f),
			("RiggedSimple.glb", 0.25f),
			("RiggedFigure.glb", 1f),
			("CesiumMan.glb", 1f),
			("BrainStem.glb", 0.8f),
			("Fox.glb", 0.01f),
			("Mixamo.fbx", 0.01f),
			//"RecursiveSkeletons.glb", // One day we might need to support this; but not today
		};
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Press Space");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f));
		camera.NearPlaneDistance = 0.001f;
		var lightBrightnessStage = 3;
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, coneDirection: camera.ViewDirection, highQuality: true);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		using var backdrop = factory.AssetLoader.LoadPreprocessedBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx), CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx));
		using var nodeHighlightMesh = factory.MeshBuilder.CreateMesh(new Cuboid(0.1f, 0.4f, 0.1f));
		using var nodeHighlightMat = factory.MaterialBuilder.CreateSimpleMaterial(factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false));
		using var nodeHighlighter = factory.ObjectBuilder.CreateModelInstance(nodeHighlightMesh, nodeHighlightMat); 
		using var scene = factory.SceneBuilder.CreateScene(backdrop);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		renderer.SetQuality(new(Quality.VeryHigh));
		

		scene.Add(light);
		scene.Add(sunlight);
		scene.Add(nodeHighlighter);
		
		var prevAnimIndex = 0;
		var prevAnimTimeRemaining = 0f;
		var prevAnimFreezeTime = 0f;
		var curFileIndex = -1;
		var curAnimIndex = 0;
		var curNodeIndex = 0;
		var curAnimCount = 1;
		var playingAnim = false;
		ResourceGroup? loadedResources = null;
		ModelInstanceGroup? modelInstanceGroup = null;
		
		void UpdateTitle() {
			window.SetTitle(
				$"X/Y/Z rotates | " +
				$"A = chg anim | " +
				$"S = chg start/stop | " +
				$"N = chg node | " +
				$"Mousewheel scales | " +
				$"'{_filesToLoad[curFileIndex].Filename}' anim {(curAnimIndex + 1)} / {curAnimCount} | " +
				$"'{modelInstanceGroup?[0].Skeleton.Nodes[curNodeIndex].GetNameAsNewStringObject()}' node | " +
				$"{modelInstanceGroup?.Count ?? 0} models"
			);
		}

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				if (modelInstanceGroup is {} i) {
					scene.Remove(i);
					i.Dispose();
				}
				
				if (loadedResources is {} g) {
					g.Dispose();
				}
				
				curFileIndex++;
				if (curFileIndex >= _filesToLoad.Length) curFileIndex = 0;
				curAnimIndex = 0;
				curNodeIndex = 0;
				prevAnimTimeRemaining = 0f;

				Console.WriteLine("Loading " + _filesToLoad[curFileIndex].Filename + "...");
				loadedResources = factory.AssetLoader.LoadAll(CommonTestAssets.FindAsset("models/" + _filesToLoad[curFileIndex].Filename), new ModelCreationConfig() { MeshConfig = new() { LinearRescalingFactor = _filesToLoad[curFileIndex].ScalingFactor, OriginTranslation = (0f, 0f, curFileIndex) }}, new ModelReadConfig() { HandleUriEscapedStrings = true });
				curAnimCount = loadedResources.Value.Models.Max(m => m.Mesh.Animations.All.Count);
				Assert.GreaterOrEqual(curAnimCount, 1);

				modelInstanceGroup = factory.ObjectBuilder.CreateModelInstanceGroup(loadedResources.Value);
				scene.Add(modelInstanceGroup.Value);
				UpdateTitle();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.A) && modelInstanceGroup.HasValue) {
				prevAnimIndex = curAnimIndex;
				prevAnimTimeRemaining = AnimBlendTime;
				prevAnimFreezeTime = (float) loop.TotalIteratedTime.TotalSeconds;
				++curAnimIndex;
				if (curAnimIndex >= curAnimCount) curAnimIndex = 0;
				UpdateTitle();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.S) && modelInstanceGroup.HasValue) {
				playingAnim = !playingAnim;
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0) && modelInstanceGroup.HasValue) {
				Console.WriteLine("Setting t=0 on anim #" + curAnimIndex);
				foreach (var mi in modelInstanceGroup) {
					if (curAnimIndex >= mi.Mesh.Animations.All.Count) continue;
					mi.GetAnimationPlayer(mi.Animations[curAnimIndex]).SetCompletionFraction(0f);
				}
				playingAnim = false;
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1) && modelInstanceGroup.HasValue) {
				Console.WriteLine("Setting t=max on anim #" + curAnimIndex);
				foreach (var mi in modelInstanceGroup) {
					if (curAnimIndex >= mi.Mesh.Animations.All.Count) continue;
					mi.GetAnimationPlayer(mi.Animations[curAnimIndex]).SetCompletionFraction(1f);
				}
				playingAnim = false;
			}

			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.X)) {
				modelInstanceGroup?.RotateBy((90f * deltaTime) % Direction.Left);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Y)) {
				modelInstanceGroup?.RotateBy((90f * deltaTime) % Direction.Up);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Z)) {
				modelInstanceGroup?.RotateBy((90f * deltaTime) % Direction.Forward);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.L)) {
				lightBrightnessStage++;
				if (lightBrightnessStage > 3) lightBrightnessStage = 0;
				light.SetBrightness(lightBrightnessStage switch {
					0 => 0f,
					1 => 0.33f,
					2 => 0.66f,
					_ => 1f
				});
			}
			modelInstanceGroup?.ScaleBy(1f - (0.05f * loop.Input.KeyboardAndMouse.MouseScrollWheelDelta)); 
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.E)) {
				factory.AssetLoader.LoadAll(CommonTestAssets.FindAsset("models/" + _filesToLoad[0])).Meshes[0].ApplySkeletalBindPose(modelInstanceGroup!.Value.Instances[0]);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.N)) {
				if (modelInstanceGroup is { } mig) {
					++curNodeIndex;
					if (curNodeIndex >= mig[0].Skeleton.Nodes.Count) curNodeIndex = 0;
					mig[0].Skeleton.GetBindPoseNodeTransform(mig[0].Skeleton.Nodes[curNodeIndex], out var bpt);
					nodeHighlighter.SetTransform(bpt * mig[0].Transform.ToMatrix());
					nodeHighlighter.SetScaling(1f);
					UpdateTitle();
				}
			}
			
			if (playingAnim && modelInstanceGroup.HasValue) {
				var isFirst = true;
				foreach (var mi in modelInstanceGroup) {
					if (curAnimIndex >= mi.Mesh.Animations.All.Count) continue;
					
					if (prevAnimTimeRemaining > 0f && prevAnimIndex < mi.Mesh.Animations.All.Count) {
						if (isFirst) {
							mi.GetAnimationPlayer(mi.Animations[curAnimIndex], mi.Animations[prevAnimIndex])
								.SetTimePointAndGetNodeTransform((float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop, (float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop, prevAnimTimeRemaining / AnimBlendTime, mi.Skeleton.Nodes[curNodeIndex], out var transform);
							nodeHighlighter.SetTransform(transform * mi.Transform.ToMatrix());
							nodeHighlighter.SetScaling(1f);
						}
						else {
							mi.GetAnimationPlayer(mi.Animations[curAnimIndex], mi.Animations[prevAnimIndex])
								.SetTimePoint((float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop, (float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop, prevAnimTimeRemaining / AnimBlendTime);
						}
						prevAnimTimeRemaining -= deltaTime;
					}
					else {
						if (isFirst) {
							mi.GetAnimationPlayer(mi.Animations[curAnimIndex]).SetTimePointAndGetNodeTransform((float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop, mi.Skeleton.Nodes[curNodeIndex], out var transform);
							nodeHighlighter.SetTransform(transform * mi.Transform.ToMatrix());
							nodeHighlighter.SetScaling(1f);
						}
						else {
							mi.GetAnimationPlayer(mi.Animations[curAnimIndex]).SetTimePoint((float) loop.TotalIteratedTime.TotalSeconds, MeshAnimationTimestampWrapStyle.Loop);
						}	
					}
				}
			}
			
			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);
			
			light.Position = camera.Position;
			light.ConeDirection = camera.ViewDirection;
			
			renderer.Render();
		}
	}
}