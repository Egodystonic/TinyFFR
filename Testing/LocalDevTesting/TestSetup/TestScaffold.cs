using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Egodystonic.TinyFFR.Testing.Local.TestSetup;

static class TestScaffold {
	static TestBuilder? _builder;
	static TestContext? _materializedContext;
	static bool _defaultLoopExitRequested = false;
	
	public static void Execute() {
		_builder = new TestBuilder();
		try {
			TestMain.ConfigureTest(_builder);
		}
		catch (Exception e) {
			Console.WriteLine($"{e.GetType().Name} occurred when configuring test ('{e.Message}').");
			throw;
		}

		_materializedContext = ((TestContextBuilder) _builder.Context).Materialize();

		try {
			TestMain.StartTest(_materializedContext);
		}
		catch (Exception e) {
			Console.WriteLine($"{e.GetType().Name} occurred when running test ('{e.Message}').");
			throw;
		}

		if (_builder.AutoDisposeContextObjectsOnTestEnd) _materializedContext.DisposeObjects();
	}
	
	public static void BeginDefaultLoop(Func<float, bool> loopAction, ApplicationLoop loop, Camera? autoCameraControlTarget) {
		if (_builder == null || _materializedContext == null) throw new InvalidOperationException($"Must complete {nameof(TestMain.ConfigureTest)} first.");

		var periodicalFpsTimer = Stopwatch.StartNew();
		var periodicalFrameCount = 0;
		while (!loop.Input.UserQuitRequested && !_defaultLoopExitRequested) {
			var sw = Stopwatch.StartNew();
			var deltaTime = loop.IterateOnce();
			var dtSecs = (float) deltaTime.TotalSeconds;

			if (autoCameraControlTarget is { } camera) {
				DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, dtSecs, _builder.Context.Window);
				DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, dtSecs);
			}

			try {
				_defaultLoopExitRequested = loopAction(dtSecs);
			}
			catch (Exception e) {
				Console.WriteLine($"{e.GetType().Name} occurred when running one iteration of default loop ('{e.Message}').");
				throw;
			}

			if (_builder.DefaultLoopSlowFrameReportingEnable && sw.Elapsed > _builder.DefaultLoopSlowFrameTime) {
				Console.WriteLine("Slow frame! Measured: " + sw.ElapsedMilliseconds + "ms / DeltaTime: " + deltaTime.TotalMilliseconds + "ms");
			}

			++periodicalFrameCount;
			if (_builder.DefaultLoopFpsReportingEnable && periodicalFpsTimer.Elapsed >= _builder.DefaultLoopFpsReportingPeriod) {
				Console.WriteLine($"Framecount over last {periodicalFpsTimer.Elapsed.TotalSeconds:N1} seconds = {periodicalFrameCount} ({(periodicalFrameCount / periodicalFpsTimer.Elapsed.TotalSeconds):N0} FPS)");
				periodicalFrameCount = 0;
				periodicalFpsTimer.Restart();
			}
		}

		_defaultLoopExitRequested = false;
		Console.WriteLine($"Framecount over final {periodicalFpsTimer.Elapsed.TotalSeconds:N1} seconds = {periodicalFrameCount} ({(periodicalFrameCount / periodicalFpsTimer.Elapsed.TotalSeconds):N0} FPS)");
	}
}