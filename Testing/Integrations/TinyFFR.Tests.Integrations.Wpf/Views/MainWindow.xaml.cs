// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.ComponentModel;
using System.Windows;
using TinyFFR.Tests.Integrations.Wpf.ViewModels;

namespace TinyFFR.Tests.Integrations.Wpf.Views;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
	}

	protected override void OnClosing(CancelEventArgs e) {
		(DataContext as MainViewModel)?.Shutdown();
		base.OnClosing(e);
	}
}
