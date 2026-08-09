// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalViewportRayCreationTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }
	
	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "LMB = entire surface, RMB = sub surface (hold LShift to test inverted coord)");
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		var renderers = OrientationUtils.All2DDiagonals.ToArray().ToDictionary(
			o => o.AsGeneralOrientation(),
			o => {
				var cam = factory.CameraBuilder.CreateCamera(Location.Origin, new Direction(o.GetHorizontalComponent() == HorizontalOrientation2D.Left ? 1f : -1f, 0f, o.GetVerticalComponent() == VerticalOrientation2D.Down ? 1f : -1f));
				cam.ProjectionType = CameraProjectionType.Orthographic;
				cam.OrthographicHeight = 10f;
				var result = factory.RendererBuilder.CreateRenderer(scene, cam, window);
				result.SetRenderSubAreaFraction(o.AsGeneralOrientation(), (0f, 0f), (0.5f, 0.5f));
				return result;
			} 
		);
		var compositor = factory.RendererBuilder.CreateCompositor(window);
		foreach (var r in renderers.Values) compositor.Add(r, RenderCompositionType.Standard);

		using var sphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		var spheres = OrientationUtils.All2DDiagonals.ToArray().ToDictionary(
			o => o.AsGeneralOrientation(),
			o => {
				var matColour = o switch {
					DiagonalOrientation2D.UpLeft => new ColorVect(1f, 0f, 0f),
					DiagonalOrientation2D.UpRight => new ColorVect(1f, 1f, 0f),
					DiagonalOrientation2D.DownLeft => new ColorVect(0f, 1f, 0f),
					DiagonalOrientation2D.DownRight => new ColorVect(0f, 0f, 1f),
					_ => throw new ArgumentOutOfRangeException(nameof(o), o, null)
				};
				var sphereMat = factory.MaterialBuilder.CreateStandardMaterial(
					colorMap: factory.TextureBuilder.CreateColorMap(matColour, false)
				);
				var result = factory.ObjectBuilder.CreateModelInstance(sphereMesh, sphereMat);
				scene.Add(result);
				return result;
			});
		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			foreach (var kvp in renderers) {
				var renderer = kvp.Value;
				
				var subAreaOffset = kvp.Key switch {
					Orientation2D.UpRight => new XYPair<int>(window.Size.X / 2, 0),
					Orientation2D.DownLeft => new XYPair<int>(0, window.Size.Y / 2),
					Orientation2D.DownRight => new XYPair<int>(window.Size.X / 2, window.Size.Y / 2),
					_ => XYPair<int>.Zero
				};
				
				foreach (var click in loop.Input.KeyboardAndMouse.NewMouseClicks) {
					if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift)) {
						var ray = click.Key == MouseKey.MouseLeft 
							? renderer.CastRayFromRenderSurface(click.Location with { Y = window.Size.Y - click.Location.Y }, coordOrigin: DiagonalOrientation2D.DownLeft, disableDpiScalingAdjustment: false) 
							: renderer.CastRayFromRenderSubAreaSurface(new XYPair<int>(click.Location.X - subAreaOffset.X, (window.Size.Y / 2) - (click.Location.Y - subAreaOffset.Y)), coordOrigin: DiagonalOrientation2D.DownLeft, disableDpiScalingAdjustment: false);
						spheres[kvp.Key].SetPosition(ray.UnboundedLocationAtDistance(1f));
					}
					else {
						var ray = click.Key == MouseKey.MouseLeft 
							? renderer.CastRayFromRenderSurface(click.Location) 
							: renderer.CastRayFromRenderSubAreaSurface(click.Location - subAreaOffset);
						spheres[kvp.Key].SetPosition(ray.UnboundedLocationAtDistance(1f));
					}
				}
			}

			compositor.RenderAll();
		}
		
		foreach (var s in spheres.Values) {
			scene.Remove(s);
			var mat = s.Material;
			s.Dispose();
			mat.Dispose();
		} 
		compositor.Dispose(); // Must precede child disposal: the group depends on its member renderers
		foreach (var r in renderers.Values) {
			var c = r.TargetCamera;
			r.Dispose();
			c.Dispose();
		} 
	}
}