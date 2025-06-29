using System.Reflection;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Factory;

namespace Egodystonic.TinyFFR.Testing;

public static class CommonTestSupportFunctions {
	const string BuildOutputDir = "build_output";
#if DEBUG
	const string ConfiguredBuildDir = "Debug";
#else
	const string ConfiguredBuildDir = "Release";
#endif
	static readonly Func<string, string>[] PossibleFileNameMutations = [
		f => $"{f}.dll",
		f => $"{f}.so",
		f => $"lib{f}.so",
		f => $"lib{f}",
	];

	public static void ResolveNativeAssembliesFromBuildOutputDir() {
		NativeLibrary.SetDllImportResolver(
			typeof(ITinyFfrFactory).Assembly,
			(libName, assy, searchPath) => {
				var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

				while (curDir != null) {
					var containedDirectories = Directory.GetDirectories(curDir).Select(Path.GetFileName);
					if (containedDirectories.Any(d => d != null && d.Equals(BuildOutputDir, StringComparison.OrdinalIgnoreCase))) {
						var expectedFilePath = Path.Combine(curDir, "build_output", ConfiguredBuildDir);
						foreach (var possibleFilePath in PossibleFileNameMutations.Select(f => Path.Combine(expectedFilePath, f(libName)))) {
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