using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing.ModelViewer;
using Egodystonic.TinyFFR.WinForms;

namespace TinyFFR.Tests.Integrations.WinForms {
	public partial class MainForm : Form {
		ModelViewerScene? _viewer;
		IDisposable? _loop;
		ModelListEntry? _displayedEntry;
		bool _uiReady;
		bool _selectionUpdateInFlight;
		bool _startInFlight;
		bool _compositorSwitchInFlight;
		bool _hasDisplayedAModel;
		int _loadedModelCount;
		int _failedModelCount;
		int _totalModelCount;

		public MainForm() {
			InitializeComponent();

			shadingStyleComboBox.DataSource = Enum.GetValues<ViewerShadingStyle>();
			qualityComboBox.DataSource = Enum.GetValues<BuiltInQualityConfiguration>();
			qualityComboBox.SelectedItem = BuiltInQualityConfiguration.Ultra;

			modelListBox.DisplayMember = nameof(ModelListEntry.DisplayText);

			_uiReady = true;
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			_ = StartRenderingAsync();
		}

		protected override void OnFormClosing(FormClosingEventArgs e) {
			Shutdown();
			base.OnFormClosing(e);
		}

		void HandleStartStopClicked(object? sender, EventArgs e) {
			if (_viewer == null) _ = StartRenderingAsync();
			else Shutdown();
		}

		void HandleRenderOnceClicked(object? sender, EventArgs e) => _viewer?.Render();

		void HandleRandomizeColorClicked(object? sender, EventArgs e) {
			if (shadingStyleComboBox.SelectedItem is ViewerShadingStyle.Textured) return;
			_viewer?.RandomizeColor(randomColorOpaqueCheckBox.Checked);
		}

		void HandleShiftLightHueClicked(object? sender, EventArgs e) => _viewer?.ShiftLightHue();

		void HandleSpinChanged(object? sender, EventArgs e) => _viewer?.SetSpin(spinXCheckBox.Checked, spinYCheckBox.Checked, spinZCheckBox.Checked);

		void HandleShadingStyleChanged(object? sender, EventArgs e) {
			if (!_uiReady || shadingStyleComboBox.SelectedItem is not ViewerShadingStyle style) return;
			_viewer?.SetShadingStyle(style);
		}

		void HandleQualityChanged(object? sender, EventArgs e) {
			if (!_uiReady || qualityComboBox.SelectedItem is not BuiltInQualityConfiguration quality) return;
			_viewer?.SetQuality(quality);
		}

		void HandleOverlayEnabledChanged(object? sender, EventArgs e) => _viewer?.SetOverlayEnabled(overlayEnabledCheckBox.Checked);

		void HandleCompositeModeChanged(object? sender, EventArgs e) {
			overlayEnabledCheckBox.Enabled = compositeModeCheckBox.Checked;
			_ = ApplyCompositorAsync(compositeModeCheckBox.Checked);
		}

		void HandleModelSelectionChanged(object? sender, EventArgs e) {
			if (!_uiReady || _selectionUpdateInFlight) return;

			// A WinForms ListBox cannot disable individual rows, so category headers and still-loading models bounce the selection back
			if (modelListBox.SelectedItem is not ModelListEntry entry || !entry.IsSelectable) {
				_selectionUpdateInFlight = true;
				try {
					if (_displayedEntry is { } displayed) modelListBox.SelectedItem = displayed;
					else modelListBox.SelectedIndex = -1;
				}
				finally { _selectionUpdateInFlight = false; }
				return;
			}

			if (ReferenceEquals(entry, _displayedEntry)) return;
			if (_viewer is not { } viewer || !viewer.IsModelLoaded(entry.FileName!)) return;

			_displayedEntry = entry;
			statusLabel.Text = viewer.DisplayModel(entry.FileName!);
			viewer.Render();
		}

		async Task StartRenderingAsync() {
			if (_startInFlight || _viewer != null) return;

			_startInFlight = true;
			try {
				var entries = ModelCatalog.Build();
				var selectableCount = 0;
				foreach (var entry in entries) {
					if (entry.FileName != null) ++selectableCount;
				}

				_selectionUpdateInFlight = true;
				try {
					modelListBox.BeginUpdate();
					modelListBox.Items.Clear();
					modelListBox.Items.AddRange(entries);
					modelListBox.SelectedIndex = -1;
					modelListBox.EndUpdate();
				}
				finally {
					_selectionUpdateInFlight = false;
				}

				_displayedEntry = null;
				_loadedModelCount = 0;
				_failedModelCount = 0;
				_totalModelCount = selectableCount;
				_hasDisplayedAModel = false;
				UpdateLoadProgress();

				var viewer = new ModelViewerScene();
				_viewer = viewer;

				PushAllSettings();

				sceneView.Renderer = viewer.ActiveRenderer;
				sceneView.Compositor = viewer.ActiveCompositor;
				statusLabel.Text = "Rendering. Models are streaming in asynchronously.";

				_loop = viewer.ApplicationLoopBuilder.StartWinFormsUiLoop(sceneView, TickWithInput);

				await viewer.LoadBackdropAsync();
				if (!ReferenceEquals(_viewer, viewer)) return;

				if (compositeModeCheckBox.Checked) {
					await ApplyCompositorAsync(true);
					if (!ReferenceEquals(_viewer, viewer)) return;
				}

				foreach (var entry in entries) {
					if (entry.FileName == null) continue;
					_ = LoadEntryAsync(viewer, entry);
				}
			}
			catch (Exception e) {
				statusLabel.Text = $"Failed to start rendering: {e.GetBaseException().Message}";
			}
			finally {
				_startInFlight = false;
			}
		}

		async Task LoadEntryAsync(ModelViewerScene viewer, ModelListEntry entry) {
			try {
				await viewer.LoadModelAsync(entry.FileName!);
				if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

				entry.IsLoaded = true;
				++_loadedModelCount;
				RefreshEntryRow(entry);

				if (!_hasDisplayedAModel) {
					_hasDisplayedAModel = true;
					modelListBox.SelectedItem = entry;
				}

				UpdateLoadProgress();
			}
			catch (Exception e) {
				if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

				entry.LoadFailed = true;
				++_failedModelCount;
				RefreshEntryRow(entry);
				UpdateLoadProgress();
				statusLabel.Text = $"Failed to load {entry.FileName}: {e.GetBaseException().Message}";
			}
		}

		// Reassigning the item is what makes the ListBox re-read DisplayText; it re-selects the row itself if it was the selected one
		void RefreshEntryRow(ModelListEntry entry) {
			var index = modelListBox.Items.IndexOf(entry);
			if (index < 0) return;

			_selectionUpdateInFlight = true;
			try { modelListBox.Items[index] = entry; }
			finally { _selectionUpdateInFlight = false; }
		}

		void UpdateLoadProgress() {
			var pending = _totalModelCount - _loadedModelCount - _failedModelCount;
			if (pending <= 0 && _failedModelCount == 0) loadProgressLabel.Text = $"All {_totalModelCount} models loaded.";
			else if (_failedModelCount > 0) loadProgressLabel.Text = $"Loaded {_loadedModelCount} of {_totalModelCount} ({_failedModelCount} failed, {pending} pending)";
			else loadProgressLabel.Text = $"Loaded {_loadedModelCount} of {_totalModelCount} ({pending} pending)";

			loadProgressBar.Maximum = Int32.Max(_totalModelCount, 1);
			loadProgressBar.Value = Int32.Min(_loadedModelCount, loadProgressBar.Maximum);
		}

		void Shutdown() {
			if (_viewer == null && _loop == null) return;

			var viewer = _viewer;
			_viewer = null;

			_loop?.Dispose();
			_loop = null;

			sceneView.Renderer = null;
			sceneView.Compositor = null;

			viewer?.Dispose();

			_selectionUpdateInFlight = true;
			try { modelListBox.SelectedIndex = -1; }
			finally { _selectionUpdateInFlight = false; }

			_displayedEntry = null;
			_loadedModelCount = 0;
			_failedModelCount = 0;
			_hasDisplayedAModel = false;
			loadProgressLabel.Text = "";
			loadProgressBar.Value = 0;
			statusLabel.Text = "Not rendering.";
		}

		void PushAllSettings() {
			if (_viewer is not { } viewer) return;

			viewer.SetSpin(spinXCheckBox.Checked, spinYCheckBox.Checked, spinZCheckBox.Checked);
			if (shadingStyleComboBox.SelectedItem is ViewerShadingStyle style) viewer.SetShadingStyle(style);
			if (qualityComboBox.SelectedItem is BuiltInQualityConfiguration quality) viewer.SetQuality(quality);
			viewer.SetOverlayEnabled(overlayEnabledCheckBox.Checked);
		}

		async Task ApplyCompositorAsync(bool value) {
			if (_viewer is not { } viewer || _compositorSwitchInFlight) return;

			_compositorSwitchInFlight = true;
			try {
				// Both must be unbound before the switch: the scene view rejects having Renderer and Compositor set simultaneously,
				// and a renderer that is still supplying frames cannot be attached to a compositor
				sceneView.Renderer = null;
				sceneView.Compositor = null;

				await viewer.SetCompositeModeAsync(value);
				if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

				sceneView.Renderer = viewer.ActiveRenderer;
				sceneView.Compositor = viewer.ActiveCompositor;
			}
			catch (Exception e) {
				statusLabel.Text = $"Failed to change compositor mode: {e.GetBaseException().Message}";
			}
			finally {
				_compositorSwitchInFlight = false;
			}
		}

		void TickWithInput(TimeSpan deltaTime, ILatestInputRetriever input) {
			if (_viewer is not { } viewer) return;
			viewer.Tick(deltaTime.AsDeltaTime(), input.KeyboardAndMouse);
			if (animateCheckBox.Checked) viewer.Render();
		}
	}
}
