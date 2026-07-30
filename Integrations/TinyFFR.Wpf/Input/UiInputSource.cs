// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Ui;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Egodystonic.TinyFFR.Wpf {
	// Translates one WPF element's input events in to a UiSourcedInputRetriever, making TinyFFR's input abstraction
	// available to code hosted inside a WPF application. Keyboard events are only observed whilst the element has
	// focus and mouse events only whilst it is hovered or has captured the mouse.
	sealed class UiInputSource : IDisposable {
		readonly UIElement _element;
		readonly UiSourcedInputRetriever _retriever = new();
		readonly UiSourcedKeyboardAndMouseRetriever _kbm;
		Window? _subscribedWindow;
		bool _isDisposed = false;

		public UiSourcedInputRetriever Retriever => _retriever;

		public UiInputSource(UIElement element) {
			ArgumentNullException.ThrowIfNull(element);
			_element = element;
			_kbm = _retriever.KeyboardAndMouseState;

			_element.PreviewKeyDown += HandleKeyDown;
			_element.PreviewKeyUp += HandleKeyUp;
			_element.PreviewMouseDown += HandleMouseDown;
			_element.PreviewMouseUp += HandleMouseUp;
			_element.PreviewMouseWheel += HandleMouseWheel;
			_element.MouseMove += HandleMouseMove;
			_element.MouseLeave += HandleMouseLeave;
			_element.LostMouseCapture += HandleLostMouseCapture;
			_element.LostKeyboardFocus += HandleLostKeyboardFocus;
			if (_element is FrameworkElement frameworkElement) frameworkElement.Loaded += HandleLoaded;

			TrySubscribeToHostWindow();
		}

		public void Iterate() => _retriever.Iterate();

		void HandleKeyDown(object sender, KeyEventArgs e) {
			if (e.IsRepeat) return;
			_kbm.RecordKeyDown(KeyboardOrMouseKeyMap.Translate(EffectiveKey(e)));
		}
		void HandleKeyUp(object sender, KeyEventArgs e) => _kbm.RecordKeyUp(KeyboardOrMouseKeyMap.Translate(EffectiveKey(e)));

		// WPF reports Alt-modified keypresses as Key.System with the 'real' key in SystemKey
		static Key EffectiveKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;

		void HandleLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => _kbm.ReleaseAllHeldKeyboardKeys();

		void HandleMouseDown(object sender, MouseButtonEventArgs e) {
			var position = ToXyPair(e.GetPosition(_element));
			_kbm.RecordCursorPosition(position);

			var mouseKey = KeyboardOrMouseKeyMap.Translate(e.ChangedButton);
			if (mouseKey == MouseKey.Unknown) return;
			_kbm.RecordKeyDown(mouseKey.ToKeyboardOrMouseKey());
			_kbm.RecordClick(mouseKey, position, e.ClickCount);

			// Capturing whilst a button is held keeps cursor deltas flowing even when the mouse leaves the element
			if (!_element.IsMouseCaptured) _element.CaptureMouse();
		}

		void HandleMouseUp(object sender, MouseButtonEventArgs e) {
			_kbm.RecordCursorPosition(ToXyPair(e.GetPosition(_element)));

			var mouseKey = KeyboardOrMouseKeyMap.Translate(e.ChangedButton);
			if (mouseKey != MouseKey.Unknown) _kbm.RecordKeyUp(mouseKey.ToKeyboardOrMouseKey());

			if (!AnyButtonIsHeld(e) && _element.IsMouseCaptured) _element.ReleaseMouseCapture();
		}

		void HandleMouseMove(object sender, MouseEventArgs e) => _kbm.RecordCursorPosition(ToXyPair(e.GetPosition(_element)));

		void HandleMouseWheel(object sender, MouseWheelEventArgs e) {
			_kbm.RecordCursorPosition(ToXyPair(e.GetPosition(_element)));
			_kbm.RecordScroll(-e.Delta / (double) Mouse.MouseWheelDeltaForOneLine); // WPF reports a positive delta for scrolling up, TinyFFR treats down as positive
		}

		// Whilst captured we keep receiving moves (with out-of-bounds positions), so the delta remains meaningful
		void HandleMouseLeave(object sender, MouseEventArgs e) {
			if (!_element.IsMouseCaptured) _kbm.ResetCursorDeltaOrigin();
		}

		void HandleLostMouseCapture(object sender, MouseEventArgs e) {
			_kbm.ReleaseAllHeldMouseButtons();
			_kbm.ResetCursorDeltaOrigin();
		}

		static bool AnyButtonIsHeld(MouseEventArgs e) {
			return e.LeftButton == MouseButtonState.Pressed
				|| e.MiddleButton == MouseButtonState.Pressed
				|| e.RightButton == MouseButtonState.Pressed
				|| e.XButton1 == MouseButtonState.Pressed
				|| e.XButton2 == MouseButtonState.Pressed;
		}

		static XYPair<int> ToXyPair(Point point) => new((int) point.X, (int) point.Y);

		void HandleLoaded(object sender, RoutedEventArgs e) => TrySubscribeToHostWindow();

		void TrySubscribeToHostWindow() {
			if (_subscribedWindow != null) return;
			if (Window.GetWindow(_element) is not { } window) return;
			_subscribedWindow = window;
			_subscribedWindow.Closing += HandleWindowClosing;
		}

		void HandleWindowClosing(object? sender, CancelEventArgs e) => _retriever.SetUserQuitRequested();

		public void Dispose() {
			if (_isDisposed) return;
			try {
				_element.PreviewKeyDown -= HandleKeyDown;
				_element.PreviewKeyUp -= HandleKeyUp;
				_element.PreviewMouseDown -= HandleMouseDown;
				_element.PreviewMouseUp -= HandleMouseUp;
				_element.PreviewMouseWheel -= HandleMouseWheel;
				_element.MouseMove -= HandleMouseMove;
				_element.MouseLeave -= HandleMouseLeave;
				_element.LostMouseCapture -= HandleLostMouseCapture;
				_element.LostKeyboardFocus -= HandleLostKeyboardFocus;
				if (_element is FrameworkElement frameworkElement) frameworkElement.Loaded -= HandleLoaded;
				if (_subscribedWindow != null) {
					_subscribedWindow.Closing -= HandleWindowClosing;
					_subscribedWindow = null;
				}
				if (_element.IsMouseCaptured) _element.ReleaseMouseCapture();
				_retriever.Dispose();
			}
			finally {
				_isDisposed = true;
			}
		}
	}
}
