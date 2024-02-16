// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory;
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
		using var loop = loopBuilder.BuildLoop(new() { FrameRateCapHz = 60 });

		_numControllers = 0;
		while (!loop.InputTracker.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(20d)) {
			if (loop.InputTracker.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) window.LockCursor = !window.LockCursor;
			HandleInput(loop.InputTracker);
			loop.IterateOnce();
		}
		HandleInput(loop.InputTracker);
		Console.WriteLine($"Quit requested: {loop.InputTracker.UserQuitRequested}");
		Console.WriteLine("KBM Event Buffer Length: " + ((UnmanagedBuffer<KeyboardOrMouseKeyEvent>) typeof(NativeInputTracker).GetField("_kbmEventBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loop.InputTracker)).Length);
		Console.WriteLine("Controller Event Buffer Length: " + ((UnmanagedBuffer<RawGameControllerButtonEvent>) typeof(NativeInputTracker).GetField("_controllerEventBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loop.InputTracker)).Length);
		Console.WriteLine("Click Event Buffer Length: " + ((UnmanagedBuffer<MouseClickEvent>) typeof(NativeInputTracker).GetField("_clickEventBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loop.InputTracker)).Length);
	}

	int _numControllers;
	void HandleInput(IInputTracker input) {
		if (input.GameControllers.Length != _numControllers) {
			for (var i = input.GameControllers.Length; i > _numControllers; --i) {
				Console.WriteLine($"Controller: {input.GameControllers[i - 1]}");
			}
			_numControllers = input.GameControllers.Length;
		}

		var amalgamatedController = input.GetAmalgamatedGameController();
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

		if (input.NewKeyEvents.Length == 0) return;
		Console.WriteLine("[" + String.Join(", ", input.CurrentlyPressedKeys.ToArray()) + "] " + String.Join(", ", input.NewKeyEvents.ToArray().Select(ke => $"{(ke.KeyDown ? "+" : "-")}{ke.Key}")));
		if (input.NewKeyDownEvents.Length > 0) Console.WriteLine("\t\t\t+" + String.Join(", ", input.NewKeyDownEvents.ToArray()));
		if (input.NewKeyUpEvents.Length > 0) Console.WriteLine("\t\t\t-" + String.Join(", ", input.NewKeyUpEvents.ToArray()));
		Console.WriteLine($"\t\t\tMouse: {input.MouseCursorPosition} (delta {input.MouseCursorDelta}); Wheel: {input.MouseScrollWheelDelta}");
		for (var i = 0; i < input.NewMouseClicks.Length; ++i) {
			Console.WriteLine($"\t\t\t\tClick: {input.NewMouseClicks[i]}");
		}

		foreach (var curKey in input.CurrentlyPressedKeys) {
			Assert.AreEqual(true, input.KeyIsCurrentlyDown(curKey));
		}
		foreach (var newDownKey in input.NewKeyDownEvents) {
			Assert.AreEqual(true, input.KeyWasPressedThisIteration(newDownKey));
		}
		foreach (var newUpKey in input.NewKeyUpEvents) {
			Assert.AreEqual(true, input.KeyWasReleasedThisIteration(newUpKey));
		}
	}
} 