using Egodystonic.TinyFFR.Testing;

namespace TinyFFR.Tests.Integrations.WinForms {
	internal static class Program {
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();

			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();
			Application.Run(new MainForm());
		}
	}
}