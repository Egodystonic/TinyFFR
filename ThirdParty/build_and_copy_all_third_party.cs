using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// ===================================================================================================================================
// Script constants
// ===================================================================================================================================

// Directory structures
const string ScriptDirName = "ThirdParty";
const string BuildOutputDirName = "build_output";
const string InterimInstallDirName = "install";
const string ThirdPartyNativeDirPath = "TinyFFR.Native/third_party";
const string ThirdPartyNativeBinariesDirName = "binaries";
const string ThirdPartyNativeHeadersDirName = "headers";
var interimInstallBinaryFolderNames = new[] { "bin", "lib" };
var interimInstallHeaderFolderNames = new[] { "include" };

// Commandline args of each third-party dependency
const string LibAssimp = "assimp";
const string LibFilament = "filament";
const string LibSdl = "sdl";
var libs = new[] { LibAssimp, LibFilament, LibSdl };

// Build incantations for each lib
const string RepoRootDirToken = "%REPODIR%";
const string BuildOutputDirToken = "%BUILDOUTDIR%";
const string ConfigurationToken = "%CONFIG%";
const string ConfigurationToLowerToken = "%CONFIG_LOWER%";
var commandLists = new Dictionary<string, List<string>> { [LibAssimp] = new(), [LibFilament] = new(), [LibSdl] = new() };

//		Use clang on Linux
if (OperatingSystem.IsLinux()) {
	Environment.SetEnvironmentVariable("CC", "/usr/bin/clang");
	Environment.SetEnvironmentVariable("CXX", "/usr/bin/clang++");
	Environment.SetEnvironmentVariable("CXXFLAGS", "-stdlib=libc++");
}

//		Assimp
if (OperatingSystem.IsMacOS()) {
	commandLists[LibAssimp].AddRange(
		$"cmake -DCMAKE_INSTALL_PREFIX={InterimInstallDirName} -DASSIMP_BUILD_ZLIB=OFF -DASSIMP_INSTALL=ON -DASSIMP_BUILD_TESTS=OFF \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"cmake --build . --config {ConfigurationToken}",
		$"cmake --build . --target install --config {ConfigurationToken}"
	);
}
else {
	commandLists[LibAssimp].AddRange(
		$"cmake -DCMAKE_INSTALL_PREFIX={InterimInstallDirName} -DASSIMP_INSTALL=ON -DASSIMP_BUILD_TESTS=OFF \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"cmake --build . --config {ConfigurationToken}",
		$"cmake --build . --target install --config {ConfigurationToken}"
	);
}

//		SDL
commandLists[LibSdl].AddRange(
	$"cmake -DCMAKE_INSTALL_PREFIX={InterimInstallDirName} \"{RepoRootDirToken}/CMakeLists.txt\"",
	$"cmake --build . --config {ConfigurationToken}",
	$"cmake --build . --target install --config {ConfigurationToken}"
);

//		Filament
if (OperatingSystem.IsMacOS()) {
	commandLists[LibFilament].AddRange(
		$"/bin/bash \"{RepoRootDirToken}/build.sh\" -c -i {ConfigurationToken}",
		$"/bin/bash -c \"rm -rf \\\"{InterimInstallDirName}\"\\\"",
		$"/bin/bash -c \"mkdir -p \\\"{InterimInstallDirName}\"\\\"",
		$"/bin/bash -c \"cp -R \\\"{RepoRootDirToken}/out/{ConfigurationToLowerToken}/filament/.\\\" \\\"{InterimInstallDirName}\\\"\""
	);
}
else {
	if (OperatingSystem.IsWindows()) {
		commandLists[LibFilament].AddRange(
			$"cmake -DCMAKE_INSTALL_PREFIX={InterimInstallDirName} " +
				$"-DFILAMENT_SUPPORTS_OPENGL=ON -DFILAMENT_INSTALL_BACKEND_TEST=OFF -DFILAMENT_SKIP_SAMPLES=ON -DFILAMENT_SUPPORTS_METAL=OFF -DFILAMENT_SUPPORTS_VULKAN=ON " +
				$"\"{RepoRootDirToken}/CMakeLists.txt\"",
			$"cmake --build . --config {ConfigurationToken}"
		);
	}
	else if (OperatingSystem.IsLinux()) {
		commandLists[LibFilament].AddRange(
			$"cmake -G Ninja -DCMAKE_INSTALL_PREFIX={InterimInstallDirName} " +
				$"-DFILAMENT_SUPPORTS_OPENGL=ON -DFILAMENT_INSTALL_BACKEND_TEST=OFF -DFILAMENT_SKIP_SAMPLES=ON -DFILAMENT_SUPPORTS_METAL=OFF -DFILAMENT_SUPPORTS_VULKAN=ON -DFILAMENT_SUPPORTS_WAYLAND=ON " +
				$"\"{RepoRootDirToken}/CMakeLists.txt\"",
			$"ninja"
		);
	}
	else {
		throw new InvalidOperationException($"Unsupported OS '{Environment.OSVersion}'.");
	}
	commandLists[LibFilament].AddRange(
		$"cmake --build . --target install --config {ConfigurationToken}"
	);
}



// Commandline args + ordering of build configurations in this script
const string DebugConfiguration = "Debug";
const string ReleaseConfiguration = "Release";
var configurations = new[] { DebugConfiguration, ReleaseConfiguration };

// Commandline args + ordering of steps in this script
const string BuildStep = "build";
const string CopyStep = "copy";
var stepNames = new[] { BuildStep, CopyStep };

// Translates source file destination locations
// Returning null indicates "skip this file"
// Otherwise function is expected to return new relative path (fileData.RelativeDestination if untouched)
// All path slashes are forward-slash, never backslash
var installDirTranslations = new Dictionary<string, Func<TargetFileData, string?>> {
	[LibAssimp] = fileData => {
		if (fileData.RelativeSource.Contains("cmake/")) return null;

		return fileData.RelativeDestination.Replace("assimp/", "");
	},
	[LibFilament] = fileData => {
		if (fileData.RelativeSource.Contains("bin/")) return null;

		return fileData.RelativeDestination
			.Replace("pkgconfig/", "")
			.Replace("x86_64/", "")
			.Replace("arm64/", "");
	},
	[LibSdl] = fileData => {
		return fileData.RelativeDestination.Replace("SDL2/", "");
	}
};

// ===================================================================================================================================
// Script starts here
// ===================================================================================================================================

// 1 -- Set up directories
Console.WriteLine("Starting Third Party Build & Copy");

var scriptDir = Directory.GetCurrentDirectory();

if (!Path.GetFileName(scriptDir)?.Equals(ScriptDirName, StringComparison.OrdinalIgnoreCase) ?? false) {
	throw new InvalidOperationException($"You must execute this script from within the '{ScriptDirName}' directory (current dir = '{Path.GetFileName(scriptDir)}').");
}

var interimBuildOutputDir = Directory
	.GetDirectories(scriptDir)
	.SingleOrDefault(d => Path.GetFileName(d)?.Equals(BuildOutputDirName, StringComparison.OrdinalIgnoreCase) ?? false);

if (interimBuildOutputDir == null) {
	interimBuildOutputDir = Directory.CreateDirectory(BuildOutputDirName).FullName;
}

var repositoryRootDir = Directory.GetParent(scriptDir)?.FullName;

if (repositoryRootDir == null) {
	throw new InvalidOperationException($"Can not find repository root (should be parent of {scriptDir} but this dir has no parent).");
}
var thirdPartyNativeDir = Path.Combine(repositoryRootDir, ThirdPartyNativeDirPath);

if (!Directory.Exists(thirdPartyNativeDir)) {
	_ = Directory.CreateDirectory(thirdPartyNativeDir);
}

string GetBuildOutputPath(string lib, string configuration) {
	return Path.Combine(interimBuildOutputDir, lib, configuration);
}
string GetThirdPartyRepoRootPath(string lib) {
	return Path.Combine(scriptDir, lib);
}
string GetInterimInstallDirPath(string lib, string configuration) {
	return Path.Combine(interimBuildOutputDir, lib, configuration, InterimInstallDirName);
}
string GetDestinationBinariesPath(string lib, string configuration) {
	return Path.Combine(thirdPartyNativeDir, ThirdPartyNativeBinariesDirName, lib.ToLowerInvariant(), configuration.ToLowerInvariant());
}
string GetDestinationHeadersPath(string lib) {
	return Path.Combine(thirdPartyNativeDir, ThirdPartyNativeHeadersDirName, lib.ToLowerInvariant());
}


// 2 -- Parse command line args
var commandLineLibArg = libs.FirstOrDefault(l => Environment.GetCommandLineArgs().Any(a => a.Equals(l, StringComparison.OrdinalIgnoreCase)));
var commandLineConfigArg = configurations.FirstOrDefault(c => Environment.GetCommandLineArgs().Any(a => a.Equals(c, StringComparison.OrdinalIgnoreCase)));
var commandLineStepArg = stepNames.FirstOrDefault(s => Environment.GetCommandLineArgs().Any(a => a.Equals(s, StringComparison.OrdinalIgnoreCase)));

Console.WriteLine($"Script Dir: {scriptDir}");
Console.WriteLine($"Interim Build Output Dir: {interimBuildOutputDir}");
Console.WriteLine($"Third Party Native Dir: {thirdPartyNativeDir}");
if (commandLineLibArg == null) {
	Console.WriteLine("Executing for all third-party dependencies.");
}
else {
	Console.WriteLine($"Executing for dependency '{commandLineLibArg}' only.");
}
if (commandLineConfigArg == null) {
	Console.WriteLine("Executing for all configurations.");
}
else {
	Console.WriteLine($"Executing for configuration '{commandLineConfigArg}' only.");
}
if (commandLineStepArg == null) {
	Console.WriteLine("Executing all steps.");
}
else {
	Console.WriteLine($"Executing step '{commandLineStepArg}' only.");
}

// 3 -- Execute according to commandline
if (commandLineStepArg == null || commandLineStepArg == BuildStep) {
	foreach (var configuration in configurations) {
		if (commandLineConfigArg != null && configuration != commandLineConfigArg) continue;

		if (commandLineLibArg != null) {
			BuildLib(commandLineLibArg, configuration);
		}
		else {
			BuildLib(LibAssimp, configuration);
			BuildLib(LibFilament, configuration);
			BuildLib(LibSdl, configuration);
		}
	}
}
if (commandLineStepArg == null || commandLineStepArg == CopyStep) {
	foreach (var configuration in configurations) {
		if (commandLineConfigArg != null && configuration != commandLineConfigArg) continue;

		if (commandLineLibArg != null) {
			CopyLib(commandLineLibArg, configuration);
		}
		else {
			CopyLib(LibAssimp, configuration);
			CopyLib(LibFilament, configuration);
			CopyLib(LibSdl, configuration);
		}
	}
}

Console.WriteLine("Script completed successfully.");

// 4 -- Local functions for each step
void BuildLib(string lib, string configuration) {
	try {
		Console.WriteLine();
		Console.WriteLine($"===============================================");
		Console.WriteLine($"BUILD | {lib.ToUpperInvariant()} | {configuration}");
		Console.WriteLine($"===============================================");
		Console.WriteLine($"Beginning build for {lib}...");
		var buildOutputDir = GetBuildOutputPath(lib, configuration);
		var thirdPartyRepoRootDir = GetThirdPartyRepoRootPath(lib);
		if (Directory.Exists(buildOutputDir)) Directory.Delete(buildOutputDir, true);
		Directory.CreateDirectory(buildOutputDir);
		Directory.SetCurrentDirectory(buildOutputDir);
		Console.WriteLine($"Build output directory: {buildOutputDir}");
		Console.WriteLine($"Repository root directory: {thirdPartyRepoRootDir}");
		Console.WriteLine();

		foreach (var invocation in commandLists[lib]) {
			var parsedInvocation = invocation
				.Replace(RepoRootDirToken, thirdPartyRepoRootDir)
				.Replace(BuildOutputDirToken, buildOutputDir)
				.Replace(ConfigurationToken, configuration)
				.Replace(ConfigurationToLowerToken, configuration.ToLowerInvariant());

			var invocationSplit = parsedInvocation.Split(' ');
			var commandletName = invocationSplit[0];
			var args = String.Join(' ', invocationSplit[1..]);

			Console.WriteLine();
			Console.WriteLine($"> {commandletName} {args}");
			var process = Process.Start(commandletName, args);
			process.WaitForExit();
			if (process.ExitCode != 0) throw new InvalidOperationException($"Process exit code was 0x{process.ExitCode:X}!");
			Console.WriteLine();
		}

		Console.WriteLine($"Finished {BuildStep} for {lib}:{configuration}.");
		Console.WriteLine();
		Console.WriteLine();
	}
	catch {
		Console.WriteLine($"Error occurred during {BuildStep} for {lib}:{configuration}!");
		throw;
	}
	finally {
		Directory.SetCurrentDirectory(scriptDir);
	}
}

void CopyLib(string lib, string configuration) {
	try {
		Console.WriteLine();
		Console.WriteLine($"===============================================");
		Console.WriteLine($"COPY | {lib.ToUpperInvariant()} | {configuration}");
		Console.WriteLine($"===============================================");
		var interimInstallDir = GetInterimInstallDirPath(lib, configuration);
		if (commandLineConfigArg != null || configuration == ReleaseConfiguration) {
			var destinationHeadersDir = GetDestinationHeadersPath(lib);
			Console.WriteLine($"Writing to '{destinationHeadersDir.Replace('\\', '/')}'...");
			if (Directory.Exists(destinationHeadersDir)) Directory.Delete(destinationHeadersDir, true);
			Directory.CreateDirectory(destinationHeadersDir);
			foreach (var h in interimInstallHeaderFolderNames) {
				var interimHeaderDir = Path.Combine(interimInstallDir, h);
				if (!Directory.Exists(interimHeaderDir)) {
					continue;
				}

				CopyDirectory(interimInstallDir, destinationHeadersDir, h, installDirTranslations[lib]);
			}
		}

		var destinationBinariesDir = GetDestinationBinariesPath(lib, configuration);
		Console.WriteLine($"Writing to '{destinationBinariesDir.Replace('\\', '/')}'...");
		if (Directory.Exists(destinationBinariesDir)) Directory.Delete(destinationBinariesDir, true);
		Directory.CreateDirectory(destinationBinariesDir);
		foreach (var b in interimInstallBinaryFolderNames) {
			var interimBinaryDir = Path.Combine(interimInstallDir, b);
			if (!Directory.Exists(interimBinaryDir)) {
				continue;
			}

			CopyDirectory(interimInstallDir, destinationBinariesDir, b, installDirTranslations[lib]);
		}

		Console.WriteLine($"Finished {CopyStep} for {lib}:{configuration}.");
		Console.WriteLine();
		Console.WriteLine();
	}
	catch {
		Console.WriteLine($"Error occurred during {CopyStep} for {lib}:{configuration}!");
		throw;
	}
}

void CopyDirectory(string interimInstallRootDir, string destinationRootDir, string curSubDir, Func<TargetFileData, string?> relativeFilePathMutator) {
	var sourceDirInfo = new DirectoryInfo(Path.Combine(interimInstallRootDir, curSubDir));

	foreach (var file in sourceDirInfo.GetFiles()) {
		var relativeDestSplit = curSubDir.TrimStart(Path.DirectorySeparatorChar, '/', '\\')
			.Split([Path.DirectorySeparatorChar, '/', '\\'], StringSplitOptions.RemoveEmptyEntries);
		var relativeDest = Path.Combine(
			String.Join(Path.DirectorySeparatorChar, relativeDestSplit[1..]),
			file.Name
		);

		var fileData = new TargetFileData(
			FullSource: file.FullName.Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
			RelativeSource: Path.GetRelativePath(interimInstallRootDir, file.FullName).Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
			FullDestination: Path.Combine(destinationRootDir, relativeDest).Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
			RelativeDestination: relativeDest.Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/')
		);

		var mutatedRelativeDest = relativeFilePathMutator(fileData);
		if (mutatedRelativeDest == null) {
			Console.WriteLine($"\t{fileData.RelativeSource}   ->   [skip]");
		}
		else {
			Console.WriteLine($"\t{fileData.RelativeSource}   ->   {mutatedRelativeDest}");
			var finalizedDest = Path.Combine(destinationRootDir, mutatedRelativeDest.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
			var destDir = Path.GetDirectoryName(finalizedDest);
			if (destDir != null && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
			file.CopyTo(finalizedDest);
		}
	}

	foreach (var subDir in sourceDirInfo.GetDirectories()) {
		CopyDirectory(interimInstallRootDir, destinationRootDir, Path.GetRelativePath(interimInstallRootDir, subDir.FullName), relativeFilePathMutator);
	}
}

sealed record TargetFileData(string FullSource, string RelativeSource, string FullDestination, string RelativeDestination);
