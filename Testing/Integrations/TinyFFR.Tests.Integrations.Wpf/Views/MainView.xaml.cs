// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Windows.Controls;
using TinyFFR.Tests.Integrations.Wpf.ViewModels;

namespace TinyFFR.Tests.Integrations.Wpf.Views;

public partial class MainView : UserControl {
	public MainView() {
		InitializeComponent();

		// The scene view is the element whose input events feed TinyFFR's ILatestInputRetriever
		DataContextChanged += (_, _) => (DataContext as MainViewModel)?.SetInputSource(SceneView);
	}
}
