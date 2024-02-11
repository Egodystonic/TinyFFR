// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeInputTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	// No assertions, but check the console while executing this
	public void Execute() {
		using var factory = new TffrFactory();

		var displayDiscoverer = factory.GetDisplayDiscoverer();
		var windowBuilder = factory.GetWindowBuilder();

		using var window = windowBuilder.Build(new() {
			Display = displayDiscoverer.GetPrimary(),
			FullscreenStyle = WindowFullscreenStyle.NotFullscreen,
			Position = displayDiscoverer.GetPrimary().CurrentResolution / 2 - (200, 200),
			Size = (400, 400)
		});

		var loopBuilder = factory.GetApplicationLoopBuilder(new() { InputTrackerConfig = new() { MaxControllerNameLength = 20 } });
		using var loop = loopBuilder.BuildLoop(new() { FrameRateCapHz = 30 });

		while (!loop.InputTracker.IsKeyDown(KeyboardOrMouseKey.Q) && loop.TotalIteratedTime < TimeSpan.FromSeconds(3d)) {
			Console.WriteLine(loop.InputTracker.CurrentlyPressedKeys.Length);
			loop.IterateOnce();
		}
	}
} 