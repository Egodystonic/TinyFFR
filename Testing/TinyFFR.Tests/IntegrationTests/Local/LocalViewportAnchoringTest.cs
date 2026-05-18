// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
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
class LocalViewportAnchoringTest {
	Orientation2D _anchoring;
	int _lastAnchoringIndex;
	XYPair<int> _offsetPx;
	XYPair<float> _offsetFrac;
	XYPair<int> _sizePx;
	XYPair<float> _sizeFrac;
	bool _useFractional;
	
	[SetUp]
	public void SetUpTest() {
		_anchoring = Orientation2D.None;
		_lastAnchoringIndex = -1;
		_offsetPx = XYPair<int>.Zero;
		_offsetFrac = XYPair<float>.Zero;
		_sizePx = XYPair<int>.One * 400;
		_sizeFrac = XYPair<float>.One * 0.5f;
		_useFractional = false;
	}

	[TearDown]
	public void TearDownTest() { }
	
	string BuildWindowString() {
		if (_useFractional) return $"Anchoring: {_anchoring} // Offset: {(_offsetFrac * 100f):N0} % // Size: {(_sizeFrac * 100f):N0} %";
		else return $"Anchoring: {_anchoring} // Offset: {_offsetPx} px // Size: {_sizePx} px";
	}
	
	void SetRendererSubArea(Renderer r) {
		if (_useFractional) {
			r.SetRenderSubAreaFraction(_anchoring, _offsetFrac, _sizeFrac);
		}
		else {
			r.SetRenderSubAreaPixels(_anchoring, _offsetPx, _sizePx);
		}
	}
	
	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display);
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Starfield);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		
		void AdjustSizeX(KeyboardOrMouseKey keyToTest, float dt) {
			if (!loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(keyToTest)) return;
			
			if (_useFractional) _sizeFrac = _sizeFrac with { X = _sizeFrac.X + dt * 0.2f };
			else _sizePx = _sizePx with { X = _sizePx.X + (int) (dt * 400f) };
			
			window.SetTitle(BuildWindowString());
			SetRendererSubArea(renderer);
		}
		void AdjustSizeY(KeyboardOrMouseKey keyToTest, float dt) {
			if (!loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(keyToTest)) return;
			
			if (_useFractional) _sizeFrac = _sizeFrac with { Y = _sizeFrac.Y + dt * 0.2f };
			else _sizePx = _sizePx with { Y = _sizePx.Y + (int) (dt * 400f) };
			
			window.SetTitle(BuildWindowString());
			SetRendererSubArea(renderer);
		}
		void AdjustOffsetX(KeyboardOrMouseKey keyToTest, float dt) {
			if (!loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(keyToTest)) return;
			
			if (_useFractional) _offsetFrac = _offsetFrac with { X = _offsetFrac.X + dt * 0.2f };
			else _offsetPx = _offsetPx with { X = _offsetPx.X + (int) (dt * 400f) };
			
			window.SetTitle(BuildWindowString());
			SetRendererSubArea(renderer);
		}
		void AdjustOffsetY(KeyboardOrMouseKey keyToTest, float dt) {
			if (!loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(keyToTest)) return;
			
			if (_useFractional) _offsetFrac = _offsetFrac with { Y = _offsetFrac.Y + dt * 0.2f };
			else _offsetPx = _offsetPx with { Y = _offsetPx.Y + (int) (dt * 400f) };
			
			window.SetTitle(BuildWindowString());
			SetRendererSubArea(renderer);
		}
		
		window.SetTitle(BuildWindowString());
		SetRendererSubArea(renderer);

		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.A)) {
				++_lastAnchoringIndex;
				if (_lastAnchoringIndex == OrientationUtils.All2DOrientations.Length) _lastAnchoringIndex = -1;
				_anchoring = _lastAnchoringIndex < 0 ? Orientation2D.None : OrientationUtils.All2DOrientations[_lastAnchoringIndex];
				
				_offsetPx = XYPair<int>.Zero;
				_offsetFrac = XYPair<float>.Zero;
				_sizePx = XYPair<int>.One * 400;
				_sizeFrac = XYPair<float>.One * 0.5f;
				
				window.SetTitle(BuildWindowString());
				SetRendererSubArea(renderer);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.F)) {
				_useFractional = !_useFractional;
				window.SetTitle(BuildWindowString());
				SetRendererSubArea(renderer);
			}
			
			AdjustOffsetY(KeyboardOrMouseKey.ArrowUp, dt);
			AdjustOffsetY(KeyboardOrMouseKey.ArrowDown, -dt);
			AdjustOffsetX(KeyboardOrMouseKey.ArrowRight, dt);
			AdjustOffsetX(KeyboardOrMouseKey.ArrowLeft, -dt);
			
			AdjustSizeY(KeyboardOrMouseKey.PageUp, dt);
			AdjustSizeY(KeyboardOrMouseKey.PageDown, -dt);
			AdjustSizeX(KeyboardOrMouseKey.Home, dt);
			AdjustSizeX(KeyboardOrMouseKey.End, -dt);
			
			camera.RotateBy(360f % Direction.Down * dt);
			renderer.Render();
		}
	}
}