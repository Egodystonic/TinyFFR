using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace SceneProgrammer;

sealed partial class MainWindow : Window {
	readonly MainViewModel _viewModel = new();

	public MainWindow() {
		InitializeComponent();
		DataContext = _viewModel;
		_viewModel.AttachInputSource(SceneView);
	}

	async void OnImportClicked(object? sender, RoutedEventArgs e) {
		var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = "Import Models",
			AllowMultiple = true,
			FileTypeFilter = [
				new FilePickerFileType("3D models") { Patterns = ["*.glb", "*.gltf", "*.fbx", "*.obj", "*.dae", "*.blend", "*.3ds", "*.ply", "*.stl"] },
				new FilePickerFileType("All files") { Patterns = ["*"] }
			]
		});

		var paths = new List<string>();
		foreach (var file in files) {
			if (file.TryGetLocalPath() is { } path) paths.Add(path);
		}
		await _viewModel.ImportAsync(paths);
	}

	async void OnLoadBackdropClicked(object? sender, RoutedEventArgs e) {
		var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = "Load Backdrop",
			FileTypeFilter = [new FilePickerFileType("HDR images") { Patterns = ["*.hdr", "*.exr"] }]
		});
		if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) await _viewModel.LoadCustomBackdropAsync(path);
	}

	protected override void OnClosing(WindowClosingEventArgs e) {
		_viewModel.Dispose();
		base.OnClosing(e);
	}
}
