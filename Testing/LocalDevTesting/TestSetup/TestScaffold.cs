using System.Reflection;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Testing.Local.TestSetup;

static class TestScaffold {
	const string BuildOutputDir = "build_output";
#if DEBUG
	const string ConfiguredBuildDir = "Debug";
#else
	const string ConfiguredBuildDir = "Release";
#endif
	static readonly string[] PermittedNativeLibFileExtensions = [".dll", ".lib", ".so", ""];

	public static void SetUpNativeAssemblyResolution() {
		NativeLibrary.SetDllImportResolver(
			typeof(LocalTinyFfrFactory).Assembly,
			(libName, assy, searchPath) => {
				var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

				while (curDir != null) {
					var containedDirectories = Directory.GetDirectories(curDir).Select(Path.GetFileName);
					if (containedDirectories.Any(d => d != null && d.Equals(BuildOutputDir, StringComparison.OrdinalIgnoreCase))) {
						var expectedFilePath = Path.Combine(curDir, "build_output", ConfiguredBuildDir, libName);
						foreach (var possibleFilePath in PermittedNativeLibFileExtensions.Select(ext => expectedFilePath + ext)) {
							if (File.Exists(possibleFilePath)) return NativeLibrary.Load(possibleFilePath, assy, searchPath);
						}

						return IntPtr.Zero;
					}

					curDir = Directory.GetParent(curDir)?.FullName;
				}
				return IntPtr.Zero;
			}
		);
	}
}