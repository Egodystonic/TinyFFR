// Created on 2024-01-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Factory;

namespace Egodystonic.TinyFFR;

[SetUpFixture]
sealed class GlobalSetup {
	readonly IReadOnlyCollection<string> _possibleAssemblyExtensions = [".dll", ".lib", ".so"];

	[OneTimeSetUp]
	public void TestSetup() {
		NativeLibrary.SetDllImportResolver( // Yeah this is ugly af but it'll do for v1
			typeof(TffrFactory).Assembly,
			(libName, assy, searchPath) => {
				var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

				while (curDir != null) {
					var containedDirectories = Directory.GetDirectories(curDir);
					if (containedDirectories.Any(d => Path.GetFileName(d)?.Equals("build_output", StringComparison.OrdinalIgnoreCase) ?? false)) {
#if DEBUG
						const string BuildConfig = "Debug";
#else
						const string BuildConfig = "Release";
#endif
						var expectedFilePath = Path.Combine(curDir, "build_output", BuildConfig, libName);
						foreach (var possibleFilePath in _possibleAssemblyExtensions.Concat([""]).Select(ext => expectedFilePath + ext)) {
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