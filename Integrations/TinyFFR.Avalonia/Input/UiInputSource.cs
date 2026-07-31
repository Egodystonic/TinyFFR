// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Input;

namespace Egodystonic.TinyFFR.Avalonia.Input;

sealed class UiInputSource : IDisposable {
	readonly InputElement _element;
	readonly UiSourcedInputRetriever _retriever = new();
	readonly UiSourcedKeyboardAndMouseInputRetriever _kbm;
	Window? _subscribedWindow;
	IPointer? _capturedPointer;
	bool _isDisposed = false;

	public UiSourcedInputRetriever Retriever => _retriever;

	public UiInputSource(InputElement element) {
		ArgumentNullException.ThrowIfNull(element);
		_element = element;
		_kbm = _retriever.KeyboardAndMouseState;

		_element.KeyDown += HandleKeyDown;
		_element.KeyUp += HandleKeyUp;
		_element.PointerPressed += HandlePointerPressed;
		_element.PointerReleased += HandlePointerReleased;
		_element.PointerMoved += HandlePointerMoved;
		_element.PointerWheelChanged += HandlePointerWheelChanged;
		_element.PointerExited += HandlePointerExited;
		_element.PointerCaptureLost += HandlePointerCaptureLost;
		_element.LostFocus += HandleLostFocus;
		_element.AttachedToVisualTree += HandleAttachedToVisualTree;

		TrySubscribeToHostWindow();
	}

	public void Iterate() => _retriever.Iterate();

	void HandleKeyDown(object? sender, KeyEventArgs e) => _kbm.RecordKeyDown(KeyboardOrMouseKeyMap.Translate(e.Key, e.KeySymbol));
	void HandleKeyUp(object? sender, KeyEventArgs e) => _kbm.RecordKeyUp(KeyboardOrMouseKeyMap.Translate(e.Key, e.KeySymbol));
	void HandleLostFocus(object? sender, RoutedEventArgs e) => _kbm.ReleaseAllHeldKeyboardKeys();

	void HandlePointerPressed(object? sender, PointerPressedEventArgs e) {
		var point = e.GetCurrentPoint(_element);
		var position = ToXyPair(point.Position);
		_kbm.RecordCursorPosition(position);

		var mouseKey = KeyboardOrMouseKeyMap.Translate(point.Properties.PointerUpdateKind);
		if (mouseKey == MouseKey.Unknown) return;
		_kbm.RecordKeyDown(mouseKey.ToKeyboardOrMouseKey());
		_kbm.RecordClick(mouseKey, position, e.ClickCount);

		if (_capturedPointer == null) {
			_capturedPointer = e.Pointer;
			e.Pointer.Capture(_element);
		}
	}

	void HandlePointerReleased(object? sender, PointerReleasedEventArgs e) {
		var point = e.GetCurrentPoint(_element);
		_kbm.RecordCursorPosition(ToXyPair(point.Position));

		var mouseKey = KeyboardOrMouseKeyMap.Translate(point.Properties.PointerUpdateKind);
		if (mouseKey != MouseKey.Unknown) _kbm.RecordKeyUp(mouseKey.ToKeyboardOrMouseKey());

		if (!AnyButtonIsHeld(point.Properties)) _capturedPointer?.Capture(null);
	}

	void HandlePointerMoved(object? sender, PointerEventArgs e) => _kbm.RecordCursorPosition(ToXyPair(e.GetPosition(_element)));

	void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e) {
		_kbm.RecordCursorPosition(ToXyPair(e.GetPosition(_element)));
		_kbm.RecordScroll(-e.Delta.Y);
	}

	void HandlePointerExited(object? sender, PointerEventArgs e) {
		if (_capturedPointer == null) _kbm.ResetCursorDeltaOrigin();
	}

	void HandlePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) {
		_capturedPointer = null;
		_kbm.ReleaseAllHeldMouseButtons();
		_kbm.ResetCursorDeltaOrigin();
	}

	static bool AnyButtonIsHeld(PointerPointProperties properties) {
		return properties.IsLeftButtonPressed
			|| properties.IsMiddleButtonPressed
			|| properties.IsRightButtonPressed
			|| properties.IsXButton1Pressed
			|| properties.IsXButton2Pressed;
	}

	static XYPair<int> ToXyPair(Point point) => new((int) point.X, (int) point.Y);

	void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => TrySubscribeToHostWindow();

	void TrySubscribeToHostWindow() {
		if (_subscribedWindow != null) return;
		if (TopLevel.GetTopLevel(_element) is not Window window) return;
		_subscribedWindow = window;
		_subscribedWindow.Closing += HandleWindowClosing;
	}

	void HandleWindowClosing(object? sender, WindowClosingEventArgs e) => _retriever.SetUserQuitRequested();

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_element.KeyDown -= HandleKeyDown;
			_element.KeyUp -= HandleKeyUp;
			_element.PointerPressed -= HandlePointerPressed;
			_element.PointerReleased -= HandlePointerReleased;
			_element.PointerMoved -= HandlePointerMoved;
			_element.PointerWheelChanged -= HandlePointerWheelChanged;
			_element.PointerExited -= HandlePointerExited;
			_element.PointerCaptureLost -= HandlePointerCaptureLost;
			_element.LostFocus -= HandleLostFocus;
			_element.AttachedToVisualTree -= HandleAttachedToVisualTree;
			if (_subscribedWindow != null) {
				_subscribedWindow.Closing -= HandleWindowClosing;
				_subscribedWindow = null;
			}
			_capturedPointer?.Capture(null);
			_capturedPointer = null;
			_retriever.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}
