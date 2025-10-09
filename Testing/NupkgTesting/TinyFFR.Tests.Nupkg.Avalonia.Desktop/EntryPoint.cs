using System;

using Avalonia;
using Egodystonic.TinyFFR.Testing;

namespace TinyFFR.Tests.Integrations.Avalonia.Desktop;

static class EntryPoint {
    [STAThread]
    public static void Main(string[] args) {
		CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() {
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
	}
}
