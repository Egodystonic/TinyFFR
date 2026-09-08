// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Windows;
using Egodystonic.TinyFFR.Testing;
using TinyFFR.Tests.Integrations.Wpf.ViewModels;
using TinyFFR.Tests.Integrations.Wpf.Views;

namespace TinyFFR.Tests.Integrations.Wpf;

public partial class App : Application {
	protected override void OnStartup(StartupEventArgs e) {
		CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();

		base.OnStartup(e);

		new MainWindow { DataContext = new MainViewModel() }.Show();
	}
}
