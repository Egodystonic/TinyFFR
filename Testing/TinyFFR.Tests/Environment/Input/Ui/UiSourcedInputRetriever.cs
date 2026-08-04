// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using NUnit.Framework;
using System;
using System.Linq;
using Egodystonic.TinyFFR.Input;

namespace Egodystonic.TinyFFR.Environment.Input.Ui;

[TestFixture]
class UiSourcedInputRetrieverTest {
	UiSourcedInputRetriever _retriever = null!;
	UiSourcedKeyboardAndMouseInputRetriever _recorder = null!;
	ILatestKeyboardAndMouseInputRetriever Kbm => _retriever.KeyboardAndMouse;

	[SetUp]
	public void SetUpTest() {
		_retriever = new UiSourcedInputRetriever();
		_recorder = _retriever.KeyboardAndMouseState;
	}

	[TearDown]
	public void TearDownTest() {
		_retriever.Dispose();
	}

	#region Key events
	[Test]
	public void ShouldOnlyReportKeyEventsForTheIterationTheyWereRecordedIn() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.W);
		_retriever.Iterate();

		Assert.AreEqual(1, Kbm.NewKeyEvents.Count);
		Assert.AreEqual(new KeyboardOrMouseKeyEvent(KeyboardOrMouseKey.W, true), Kbm.NewKeyEvents[0]);
		Assert.IsTrue(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.W));
		Assert.IsTrue(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.W));

		_retriever.Iterate();

		Assert.AreEqual(0, Kbm.NewKeyEvents.Count);
		Assert.IsFalse(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.W));
		Assert.IsTrue(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.W)); // Held keys persist until released

		_recorder.RecordKeyUp(KeyboardOrMouseKey.W);
		_retriever.Iterate();

		Assert.IsTrue(Kbm.KeyWasReleasedThisIteration(KeyboardOrMouseKey.W));
		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.W));
		Assert.AreEqual(0, Kbm.CurrentlyPressedKeys.Count);
	}

	[Test]
	public void ShouldFilterOsKeyRepeat() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.A);
		_recorder.RecordKeyDown(KeyboardOrMouseKey.A);
		_recorder.RecordKeyDown(KeyboardOrMouseKey.A);
		_retriever.Iterate();

		Assert.AreEqual(1, Kbm.NewKeyDownEvents.Count);
		Assert.AreEqual(1, Kbm.CurrentlyPressedKeys.Count);

		// A repeat spanning an iteration boundary must also be filtered
		_recorder.RecordKeyDown(KeyboardOrMouseKey.A);
		_retriever.Iterate();

		Assert.AreEqual(0, Kbm.NewKeyDownEvents.Count);
		Assert.IsTrue(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.A));
	}

	[Test]
	public void ShouldIgnoreReleasesOfKeysThatWereNeverPressed() {
		_recorder.RecordKeyUp(KeyboardOrMouseKey.Space);
		_retriever.Iterate();

		Assert.AreEqual(0, Kbm.NewKeyEvents.Count);
		Assert.IsFalse(Kbm.KeyWasReleasedThisIteration(KeyboardOrMouseKey.Space));
	}

	[Test]
	public void ShouldIgnoreUnknownKeys() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.Unknown);
		_recorder.RecordKeyUp(KeyboardOrMouseKey.Unknown);
		_retriever.Iterate();

		Assert.AreEqual(0, Kbm.NewKeyEvents.Count);
	}

	[Test]
	public void ShouldNotExposeEventsRecordedAfterTheCurrentIterationBegan() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.Q);
		_retriever.Iterate();
		_recorder.RecordKeyDown(KeyboardOrMouseKey.E); // Arrives 'between' ticks

		Assert.AreEqual(1, Kbm.NewKeyDownEvents.Count);
		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.E));
		Assert.IsFalse(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.E));

		_retriever.Iterate();

		Assert.IsTrue(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.E));
		Assert.IsTrue(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.E));
	}

	[Test]
	public void ShouldReleaseHeldKeysByCategory() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.W);
		_recorder.RecordKeyDown(KeyboardOrMouseKey.LeftShift);
		_recorder.RecordKeyDown(KeyboardOrMouseKey.MouseLeft);
		_retriever.Iterate();
		Assert.AreEqual(3, Kbm.CurrentlyPressedKeys.Count);

		_recorder.ReleaseAllHeldKeyboardKeys();
		_retriever.Iterate();

		Assert.AreEqual(2, Kbm.NewKeyUpEvents.Count);
		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.W));
		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift));
		Assert.IsTrue(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft));

		_recorder.ReleaseAllHeldMouseButtons();
		_retriever.Iterate();

		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft));
		Assert.AreEqual(0, Kbm.CurrentlyPressedKeys.Count);
	}
	#endregion

	#region Mouse wheel
	[Test]
	public void ShouldRecordScrollNotchesAsPairedWheelEvents() {
		_recorder.RecordScroll(2d);
		_retriever.Iterate();

		Assert.AreEqual(2, Kbm.MouseScrollWheelDelta); // Down is positive
		Assert.AreEqual(4, Kbm.NewKeyEvents.Count);
		Assert.AreEqual(2, Kbm.NewKeyDownEvents.Count);
		Assert.AreEqual(2, Kbm.NewKeyUpEvents.Count);
		Assert.IsTrue(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseWheelDown));
		Assert.IsFalse(Kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseWheelDown)); // Must not linger as a held key

		_recorder.RecordScroll(-1d);
		_retriever.Iterate();

		Assert.AreEqual(-1, Kbm.MouseScrollWheelDelta);
		Assert.IsTrue(Kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseWheelUp));

		_retriever.Iterate();
		Assert.AreEqual(0, Kbm.MouseScrollWheelDelta);
	}

	[Test]
	public void ShouldAccumulateFractionalScrollNotches() {
		_recorder.RecordScroll(0.5d);
		_retriever.Iterate();
		Assert.AreEqual(0, Kbm.MouseScrollWheelDelta);

		_recorder.RecordScroll(0.5d);
		_retriever.Iterate();
		Assert.AreEqual(1, Kbm.MouseScrollWheelDelta);
	}
	#endregion

	#region Cursor position and delta
	[Test]
	public void ShouldNotReportADeltaForTheFirstCursorObservation() {
		_recorder.RecordCursorPosition(new XYPair<int>(100, 200));
		_retriever.Iterate();

		Assert.AreEqual(new XYPair<int>(100, 200), Kbm.MouseCursorPosition);
		Assert.AreEqual(XYPair<int>.Zero, Kbm.MouseCursorDelta);
	}

	[Test]
	public void ShouldAccumulateCursorDeltaWithinAnIterationAndZeroItAfterwards() {
		_recorder.RecordCursorPosition(new XYPair<int>(10, 10));
		_retriever.Iterate();

		_recorder.RecordCursorPosition(new XYPair<int>(15, 12));
		_recorder.RecordCursorPosition(new XYPair<int>(20, 20));
		_retriever.Iterate();

		Assert.AreEqual(new XYPair<int>(20, 20), Kbm.MouseCursorPosition);
		Assert.AreEqual(new XYPair<int>(10, 10), Kbm.MouseCursorDelta);

		_retriever.Iterate();

		Assert.AreEqual(new XYPair<int>(20, 20), Kbm.MouseCursorPosition); // Position persists when nothing was recorded
		Assert.AreEqual(XYPair<int>.Zero, Kbm.MouseCursorDelta); // Delta does not
	}

	[Test]
	public void ShouldNotReportADeltaAcrossAResetCursorDeltaOrigin() {
		_recorder.RecordCursorPosition(new XYPair<int>(799, 5));
		_retriever.Iterate();

		_recorder.ResetCursorDeltaOrigin();
		_recorder.RecordCursorPosition(new XYPair<int>(2, 400));
		_retriever.Iterate();

		Assert.AreEqual(new XYPair<int>(2, 400), Kbm.MouseCursorPosition);
		Assert.AreEqual(XYPair<int>.Zero, Kbm.MouseCursorDelta);
	}
	#endregion

	#region Clicks
	[Test]
	public void ShouldRecordClickEvents() {
		_recorder.RecordClick(MouseKey.MouseRight, new XYPair<int>(30, 40), 2);
		_retriever.Iterate();

		Assert.AreEqual(1, Kbm.NewMouseClicks.Count);
		Assert.AreEqual(new MouseClickEvent(new XYPair<int>(30, 40), MouseKey.MouseRight, 2), Kbm.NewMouseClicks[0]);

		_retriever.Iterate();
		Assert.AreEqual(0, Kbm.NewMouseClicks.Count);
	}
	#endregion

	#region Enumerable invalidation
	[Test]
	public void ShouldInvalidateOutstandingEnumerablesWhenIterated() {
		_recorder.RecordKeyDown(KeyboardOrMouseKey.W);
		_retriever.Iterate();

		var keyDownEvents = Kbm.NewKeyDownEvents;
		Assert.AreEqual(1, keyDownEvents.Count);

		_retriever.Iterate();

		Assert.Catch<InvalidOperationException>(() => _ = keyDownEvents.Count);
	}
	#endregion

	#region Game controllers
	[Test]
	public void ShouldReportNoGameControllers() {
		Assert.AreEqual(0, _retriever.GameControllers.Count);

		var combined = _retriever.GameControllersCombined;
		Assert.AreEqual(0, combined.NewButtonEvents.Count);
		Assert.AreEqual(0, combined.NewButtonDownEvents.Count);
		Assert.AreEqual(0, combined.NewButtonUpEvents.Count);
		Assert.AreEqual(0, combined.CurrentlyPressedButtons.Count);
		Assert.IsFalse(combined.ButtonIsCurrentlyDown(GameControllerButton.A));
		Assert.IsFalse(combined.ButtonWasPressedThisIteration(GameControllerButton.A));
		Assert.IsFalse(combined.ButtonWasReleasedThisIteration(GameControllerButton.A));
		Assert.AreEqual(default(GameControllerStickPosition), combined.LeftStickPosition);
		Assert.AreEqual(default(GameControllerTriggerPosition), combined.RightTriggerPosition);
	}
	#endregion

	#region Quit request and disposal
	[Test]
	public void ShouldTrackUserQuitRequest() {
		Assert.IsFalse(_retriever.UserQuitRequested);
		_retriever.SetUserQuitRequested();
		Assert.IsTrue(_retriever.UserQuitRequested);
	}

	[Test]
	public void ShouldThrowWhenUsedAfterDisposal() {
		var kbm = Kbm;
		_retriever.Dispose();

		Assert.Catch<ObjectDisposedException>(() => _ = _retriever.KeyboardAndMouse);
		Assert.Catch<ObjectDisposedException>(() => _ = _retriever.GameControllers);
		Assert.Catch<ObjectDisposedException>(() => _ = kbm.MouseScrollWheelDelta);
		Assert.Catch<ObjectDisposedException>(() => _ = kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.W));

		_retriever.Dispose(); // Disposal must be idempotent
	}
	#endregion

	#region Text Input
	[Test]
	public void ShouldNotRecordTextInputUntilEnabled() {
		_recorder.RecordTextInput("hello");
		_recorder.Iterate();
		Assert.AreEqual(0, Kbm.TranscribedText.Length);

		Kbm.TextInputEnabled = true;
		_recorder.RecordTextInput("hello");
		_recorder.Iterate();
		Assert.AreEqual("hello", Kbm.TranscribedText.ToString());
	}

	[Test]
	public void ShouldOnlyReportTextInputForTheIterationItWasRecordedIn() {
		Kbm.TextInputEnabled = true;

		_recorder.RecordTextInput("ab");
		Assert.AreEqual(0, Kbm.TranscribedText.Length); // Not yet iterated

		_recorder.Iterate();
		Assert.AreEqual("ab", Kbm.TranscribedText.ToString());

		_recorder.Iterate();
		Assert.AreEqual(0, Kbm.TranscribedText.Length);
	}

	[Test]
	public void ShouldAccumulateMultipleTextInputRecordingsWithinAnIteration() {
		Kbm.TextInputEnabled = true;

		_recorder.RecordTextInput("a");
		_recorder.RecordTextInput("b");
		_recorder.RecordTextInput("cd");
		_recorder.Iterate();
		Assert.AreEqual("abcd", Kbm.TranscribedText.ToString());
	}

	[Test]
	public void ShouldSupportNonAsciiTextInput() {
		Kbm.TextInputEnabled = true;

		_recorder.RecordTextInput("é€");
		_recorder.Iterate();
		Assert.AreEqual("é€", Kbm.TranscribedText.ToString());
	}

	[Test]
	public void ShouldDiscardPendingTextInputWhenDisabled() {
		Kbm.TextInputEnabled = true;
		_recorder.RecordTextInput("abc");
		Kbm.TextInputEnabled = false;
		_recorder.Iterate();
		Assert.AreEqual(0, Kbm.TranscribedText.Length);
	}
	#endregion
}
