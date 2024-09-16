// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeInputTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalRendererFactory();

		var displayDiscoverer = factory.DisplayDiscoverer;
		var windowBuilder = factory.WindowBuilder;

		using var window = windowBuilder.Build(new() {
			Display = displayDiscoverer.Primary!.Value,
			FullscreenStyle = WindowFullscreenStyle.NotFullscreen,
			Position = displayDiscoverer.Primary!.Value.CurrentResolution / 2 - (200, 200),
			Size = (400, 400)
		});
		window.Title = "Close me to end test";

		var loopBuilder = factory.ApplicationLoopBuilder;
		using var loop = loopBuilder.BuildLoop(new() { FrameRateCapHz = 60 });

		_numControllers = 0;
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(20d)) {
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) window.LockCursor = !window.LockCursor;
			HandleInput(loop.Input);
			loop.IterateOnce();
		}
		HandleInput(loop.Input);
		Console.WriteLine($"Quit requested: {loop.Input.UserQuitRequested}");
	}

	int _numControllers;
	void HandleInput(IInputTracker input) {
		if (input.GameControllers.Length != _numControllers) {
			for (var i = input.GameControllers.Length; i > _numControllers; --i) {
				Console.WriteLine($"Controller: {input.GameControllers[i - 1].ControllerName}");
			}
			_numControllers = input.GameControllers.Length;
		}

		var amalgamatedController = input.GameControllersCombined;
		if (amalgamatedController.NewButtonEvents.Length > 0) {
			Console.WriteLine("[" + String.Join(", ", amalgamatedController.CurrentlyPressedButtons.ToArray()) + "] " + String.Join(", ", amalgamatedController.NewButtonEvents.ToArray().Select(ke => $"{(ke.ButtonDown ? "+" : "-")}{ke.Button}")));
			if (amalgamatedController.NewButtonDownEvents.Length > 0) Console.WriteLine("\t\t\t+" + String.Join(", ", amalgamatedController.NewButtonDownEvents.ToArray()));
			if (amalgamatedController.NewButtonUpEvents.Length > 0) Console.WriteLine("\t\t\t-" + String.Join(", ", amalgamatedController.NewButtonUpEvents.ToArray()));
			Console.WriteLine("\t\t\tLeft/Right Trigger: " + amalgamatedController.LeftTriggerPosition + " / " + amalgamatedController.RightTriggerPosition);
			Console.WriteLine("\t\t\tLeft/Right Stick: " + amalgamatedController.LeftStickPosition + " / " + amalgamatedController.RightStickPosition);

			foreach (var curButton in amalgamatedController.CurrentlyPressedButtons) {
				Assert.AreEqual(true, amalgamatedController.ButtonIsCurrentlyDown(curButton));
			}
			foreach (var newDownButton in amalgamatedController.NewButtonDownEvents) {
				Assert.AreEqual(true, amalgamatedController.ButtonWasPressedThisIteration(newDownButton));
			}
			foreach (var newUpButton in amalgamatedController.NewButtonUpEvents) {
				Assert.AreEqual(true, amalgamatedController.ButtonWasReleasedThisIteration(newUpButton));
			}
		}

		var kbm = input.KeyboardAndMouse;

		if (kbm.NewKeyEvents.Length == 0) return;
		Console.WriteLine("[" + String.Join(", ", kbm.CurrentlyPressedKeys.ToArray()) + "] " + String.Join(", ", kbm.NewKeyEvents.ToArray().Select(ke => $"{(ke.KeyDown ? "+" : "-")}{ke.Key}")));
		if (kbm.NewKeyDownEvents.Length > 0) Console.WriteLine("\t\t\t+" + String.Join(", ", kbm.NewKeyDownEvents.ToArray()));
		if (kbm.NewKeyUpEvents.Length > 0) Console.WriteLine("\t\t\t-" + String.Join(", ", kbm.NewKeyUpEvents.ToArray()));
		Console.WriteLine($"\t\t\tMouse: {kbm.MouseCursorPosition} (delta {kbm.MouseCursorDelta}); Wheel: {kbm.MouseScrollWheelDelta}");
		for (var i = 0; i < kbm.NewMouseClicks.Length; ++i) {
			Console.WriteLine($"\t\t\t\tClick: {kbm.NewMouseClicks[i]}");
		}

		foreach (var curKey in kbm.CurrentlyPressedKeys) {
			Assert.AreEqual(true, kbm.KeyIsCurrentlyDown(curKey));
		}
		foreach (var newDownKey in kbm.NewKeyDownEvents) {
			Assert.AreEqual(true, kbm.KeyWasPressedThisIteration(newDownKey));
		}
		foreach (var newUpKey in kbm.NewKeyUpEvents) {
			Assert.AreEqual(true, kbm.KeyWasReleasedThisIteration(newUpKey));
		}
	}
} 