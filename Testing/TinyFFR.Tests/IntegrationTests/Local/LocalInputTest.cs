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
class LocalInputTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		var displayDiscoverer = factory.DisplayDiscoverer;
		var windowBuilder = factory.WindowBuilder;

		using var window = windowBuilder.CreateWindow(new() {
			Display = displayDiscoverer.Primary!.Value,
			FullscreenStyle = WindowFullscreenStyle.NotFullscreen,
			Position = displayDiscoverer.Primary!.Value.CurrentResolution / 2 - (200, 200),
			Size = (400, 400)
		});
		window.SetTitle("Close me to end test early");

		var loopBuilder = factory.ApplicationLoopBuilder;
		using var beforeLoop = loopBuilder.CreateLoop(new() { IterationShouldRefreshGlobalInputStates = false });
		using var loop = loopBuilder.CreateLoop(new() { FrameRateCapHz = 60 });
		using var afterLoop = loopBuilder.CreateLoop(new() { IterationShouldRefreshGlobalInputStates = false });

		_numControllers = 0;
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) window.LockCursor = !window.LockCursor;
			HandleInput(loop.Input);
			AssertInputStatesAreEqual(loop.Input, beforeLoop.Input);
			AssertInputStatesAreEqual(loop.Input, afterLoop.Input);
			beforeLoop.IterateOnce();
			loop.IterateOnce();
			afterLoop.IterateOnce();
		}
		HandleInput(loop.Input);
		Console.WriteLine($"Quit requested: {loop.Input.UserQuitRequested}");
	}

	int _numControllers;
	void HandleInput(ILatestInputRetriever input) {
		if (input.GameControllers.Count != _numControllers) {
			for (var i = input.GameControllers.Count; i > _numControllers; --i) {
				Console.WriteLine($"Controller: {input.GameControllers[i - 1].GetNameAsNewStringObject()}");
			}
			_numControllers = input.GameControllers.Count;
		}

		var amalgamatedController = input.GameControllersCombined;
		if (amalgamatedController.NewButtonEvents.Count > 0) {
			Console.WriteLine("[" + String.Join(", ", amalgamatedController.CurrentlyPressedButtons.ToArray()) + "] " + String.Join(", ", amalgamatedController.NewButtonEvents.ToArray().Select(ke => $"{(ke.ButtonDown ? "+" : "-")}{ke.Button}")));
			if (amalgamatedController.NewButtonDownEvents.Count > 0) Console.WriteLine("\t\t\t+" + String.Join(", ", amalgamatedController.NewButtonDownEvents.ToArray()));
			if (amalgamatedController.NewButtonUpEvents.Count > 0) Console.WriteLine("\t\t\t-" + String.Join(", ", amalgamatedController.NewButtonUpEvents.ToArray()));
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

			foreach (var controller in input.GameControllers) {
				Console.WriteLine("\t\t\t" + controller.GetNameAsNewStringObject() + " -> " + controller.CurrentlyPressedButtons.Count + " buttons pressed");
			}
		}

		var kbm = input.KeyboardAndMouse;

		if (kbm.NewKeyEvents.Count == 0) return;
		Console.WriteLine("[" + String.Join(", ", kbm.CurrentlyPressedKeys.ToArray()) + "] " + String.Join(", ", kbm.NewKeyEvents.ToArray().Select(ke => $"{(ke.KeyDown ? "+" : "-")}{ke.Key}")));
		if (kbm.NewKeyDownEvents.Count > 0) Console.WriteLine("\t\t\t+" + String.Join(", ", kbm.NewKeyDownEvents.ToArray()));
		if (kbm.NewKeyUpEvents.Count > 0) Console.WriteLine("\t\t\t-" + String.Join(", ", kbm.NewKeyUpEvents.ToArray()));
		Console.WriteLine($"\t\t\tMouse: {kbm.MouseCursorPosition} (delta {kbm.MouseCursorDelta}); Wheel: {kbm.MouseScrollWheelDelta}");
		for (var i = 0; i < kbm.NewMouseClicks.Count; ++i) {
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

	void AssertInputStatesAreEqual(ILatestInputRetriever expected, ILatestInputRetriever actual) {
		void AssertControllerStates(ILatestGameControllerInputStateRetriever e, ILatestGameControllerInputStateRetriever a) {
			Assert.IsTrue(e.GetNameAsNewStringObject().SequenceEqual(a.GetNameAsNewStringObject()));
			Assert.IsTrue(e.CurrentlyPressedButtons.SequenceEqual(a.CurrentlyPressedButtons));
			Assert.AreEqual(e.LeftStickPosition, a.LeftStickPosition);
			Assert.AreEqual(e.LeftTriggerPosition, a.LeftTriggerPosition);
			Assert.AreEqual(e.RightStickPosition, a.RightStickPosition);
			Assert.AreEqual(e.RightTriggerPosition, a.RightTriggerPosition);
		}

		Assert.AreEqual(expected.UserQuitRequested, actual.UserQuitRequested);

		var expectedKbm = expected.KeyboardAndMouse;
		var actualKbm = actual.KeyboardAndMouse;
		Assert.AreEqual(expectedKbm.MouseCursorDelta, actualKbm.MouseCursorDelta);
		Assert.AreEqual(expectedKbm.MouseCursorPosition, actualKbm.MouseCursorPosition);
		Assert.AreEqual(expectedKbm.MouseScrollWheelDelta, actualKbm.MouseScrollWheelDelta);
		Assert.IsTrue(expectedKbm.NewKeyDownEvents.SequenceEqual(actualKbm.NewKeyDownEvents));
		Assert.IsTrue(expectedKbm.NewKeyEvents.SequenceEqual(actualKbm.NewKeyEvents));
		Assert.IsTrue(expectedKbm.NewKeyUpEvents.SequenceEqual(actualKbm.NewKeyUpEvents));
		Assert.IsTrue(expectedKbm.NewMouseClicks.SequenceEqual(actualKbm.NewMouseClicks));

		AssertControllerStates(expected.GameControllersCombined, actual.GameControllersCombined);
		Assert.AreEqual(expected.GameControllers.Count, actual.GameControllers.Count);
		for (var i = 0; i < expected.GameControllers.Count; ++i) {
			AssertControllerStates(expected.GameControllers[i], actual.GameControllers[i]);
		}
	}
} 