using System.Configuration;
using System.Data;
using System.Windows;
using Egodystonic.TinyFFR.Testing;

namespace TinyFFR.Tests.Integrations.Wpf {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();

			base.OnStartup(e);
		}
	}

}
