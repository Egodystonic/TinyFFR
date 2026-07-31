// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Input;

namespace Egodystonic.TinyFFR.WinForms.Input {
	sealed class UiInputSource : IDisposable {
		readonly Control _control;
		readonly UiSourcedInputRetriever _retriever = new();
		readonly UiSourcedKeyboardAndMouseInputRetriever _kbm;
		Form? _subscribedForm;
		bool _isDisposed = false;

		public UiSourcedInputRetriever Retriever => _retriever;

		public UiInputSource(Control control) {
			ArgumentNullException.ThrowIfNull(control);
			_control = control;
			_kbm = _retriever.KeyboardAndMouseState;

			_control.KeyDown += HandleKeyDown;
			_control.KeyUp += HandleKeyUp;
			_control.MouseDown += HandleMouseDown;
			_control.MouseUp += HandleMouseUp;
			_control.MouseMove += HandleMouseMove;
			_control.MouseWheel += HandleMouseWheel;
			_control.MouseLeave += HandleMouseLeave;
			_control.MouseCaptureChanged += HandleMouseCaptureChanged;
			_control.LostFocus += HandleLostFocus;
			_control.ParentChanged += HandleParentChanged;

			TrySubscribeToHostForm();
		}

		public void Iterate() => _retriever.Iterate();

		void HandleKeyDown(object? sender, KeyEventArgs e) => _kbm.RecordKeyDown(Input.KeyboardOrMouseKeyMap.Translate(e.KeyCode));

		void HandleKeyUp(object? sender, KeyEventArgs e) {
			if (Input.KeyboardOrMouseKeyMap.TryGetSidedModifierPair(e.KeyCode, out var left, out var right)) {
				_kbm.RecordKeyUp(left);
				_kbm.RecordKeyUp(right);
			}
			else _kbm.RecordKeyUp(Input.KeyboardOrMouseKeyMap.Translate(e.KeyCode));
		}

		void HandleLostFocus(object? sender, EventArgs e) => _kbm.ReleaseAllHeldKeyboardKeys();

		void HandleMouseDown(object? sender, MouseEventArgs e) {
			var position = ToXyPair(e.Location);
			_kbm.RecordCursorPosition(position);

			var mouseKey = Input.KeyboardOrMouseKeyMap.Translate(e.Button);
			if (mouseKey == MouseKey.Unknown) return;
			_kbm.RecordKeyDown(mouseKey.ToKeyboardOrMouseKey());
			_kbm.RecordClick(mouseKey, position, e.Clicks);

			if (!_control.Capture) _control.Capture = true;
		}

		void HandleMouseUp(object? sender, MouseEventArgs e) {
			_kbm.RecordCursorPosition(ToXyPair(e.Location));

			var mouseKey = Input.KeyboardOrMouseKeyMap.Translate(e.Button);
			if (mouseKey != MouseKey.Unknown) _kbm.RecordKeyUp(mouseKey.ToKeyboardOrMouseKey());

			if (Control.MouseButtons == MouseButtons.None && _control.Capture) _control.Capture = false;
		}

		void HandleMouseMove(object? sender, MouseEventArgs e) => _kbm.RecordCursorPosition(ToXyPair(e.Location));

		void HandleMouseWheel(object? sender, MouseEventArgs e) {
			_kbm.RecordCursorPosition(ToXyPair(e.Location));
			_kbm.RecordScroll(-e.Delta / (double) SystemInformation.MouseWheelScrollDelta);
		}

		void HandleMouseLeave(object? sender, EventArgs e) {
			if (!_control.Capture) _kbm.ResetCursorDeltaOrigin();
		}

		void HandleMouseCaptureChanged(object? sender, EventArgs e) {
			if (_control.Capture) return;
			_kbm.ReleaseAllHeldMouseButtons();
			_kbm.ResetCursorDeltaOrigin();
		}

		static XYPair<int> ToXyPair(Point point) => new(point.X, point.Y);

		void HandleParentChanged(object? sender, EventArgs e) => TrySubscribeToHostForm();

		void TrySubscribeToHostForm() {
			if (_subscribedForm != null) return;
			if (_control.FindForm() is not { } form) return;
			_subscribedForm = form;
			_subscribedForm.FormClosing += HandleFormClosing;
		}

		void HandleFormClosing(object? sender, FormClosingEventArgs e) => _retriever.SetUserQuitRequested();

		public void Dispose() {
			if (_isDisposed) return;
			try {
				_control.KeyDown -= HandleKeyDown;
				_control.KeyUp -= HandleKeyUp;
				_control.MouseDown -= HandleMouseDown;
				_control.MouseUp -= HandleMouseUp;
				_control.MouseMove -= HandleMouseMove;
				_control.MouseWheel -= HandleMouseWheel;
				_control.MouseLeave -= HandleMouseLeave;
				_control.MouseCaptureChanged -= HandleMouseCaptureChanged;
				_control.LostFocus -= HandleLostFocus;
				_control.ParentChanged -= HandleParentChanged;
				if (_subscribedForm != null) {
					_subscribedForm.FormClosing -= HandleFormClosing;
					_subscribedForm = null;
				}
				if (_control.Capture) _control.Capture = false;
				_retriever.Dispose();
			}
			finally {
				_isDisposed = true;
			}
		}
	}
}
