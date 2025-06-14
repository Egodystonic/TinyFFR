using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// ===================================================================================================================================
// Script constants
// ===================================================================================================================================

const string ScriptDirName = "ThirdParty";
const string BuildOutputDirName = "build_output";
const string InterimInstallDirName = "install";
const string ThirdPartyNativeDirPath = "TinyFFR.Native/third_party";
const string ThirdPartyNativeBinariesDirName = "binaries";
const string ThirdPartyNativeHeadersDirName = "headers";
var interimInstallBinaryFolderNames = new[] { "bin", "lib" };
var interimInstallHeaderFolderNames = new[] { "include" };

const string LibAssimp = "assimp";
const string LibFilament = "filament";
const string LibSdl = "sdl";

const string RepoRootDirToken = "%REPODIR%";
const string BuildOutputDirToken = "%BUILDOUTDIR%";
const string ConfigurationToken = "%CONFIG%";
var cmakeInvocationsDict = new Dictionary<string, List<string>> {
	[LibAssimp] = new() {
		$"-DCMAKE_INSTALL_PREFIX={InterimInstallDirName} -DASSIMP_INSTALL=OFF -DASSIMP_BUILD_TESTS=OFF \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	},
	[LibFilament] = new() {
		$"-DCMAKE_INSTALL_PREFIX={InterimInstallDirName} \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	},
	[LibSdl] = new() {
		$"-DCMAKE_INSTALL_PREFIX={InterimInstallDirName} \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	}
};

const string DebugConfiguration = "Debug";
const string ReleaseConfiguration = "Release";
var configurations = new[] { DebugConfiguration, ReleaseConfiguration };

const string BuildStep = "build";
const string CopyStep = "copy";
var stepNames = new[] { BuildStep, CopyStep };

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
var commandLineLibArg = cmakeInvocationsDict.Keys.FirstOrDefault(k => Environment.GetCommandLineArgs().Any(a => a.Equals(k, StringComparison.OrdinalIgnoreCase)));
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

// 4 -- Local functions for each step
void BuildLib(string lib, string configuration) {
	try {
		Console.WriteLine();
		Console.WriteLine($"===============================================");
		Console.WriteLine($"BUILD | {lib.ToUpperInvariant()} | {configuration}");
		Console.WriteLine($"===============================================");
		Console.WriteLine($"Beginning cmake for {lib}...");
		var buildOutputDir = GetBuildOutputPath(lib, configuration);
		var thirdPartyRepoRootDir = GetThirdPartyRepoRootPath(lib);
		if (Directory.Exists(buildOutputDir)) Directory.Delete(buildOutputDir, true);
		Directory.CreateDirectory(buildOutputDir);
		Directory.SetCurrentDirectory(buildOutputDir);
		Console.WriteLine($"Build output directory: {buildOutputDir}");
		Console.WriteLine($"Repository root directory: {thirdPartyRepoRootDir}");
		Console.WriteLine();

		foreach (var invocation in cmakeInvocationsDict[lib]) {
			var parsedInvocation = invocation
				.Replace(RepoRootDirToken, thirdPartyRepoRootDir)
				.Replace(BuildOutputDirToken, buildOutputDir)
				.Replace(ConfigurationToken, configuration);

			Console.WriteLine();
			Console.WriteLine($"> cmake {parsedInvocation}");
			var process = Process.Start("cmake", parsedInvocation);
			process.WaitForExit();
			if (process.ExitCode != 0) throw new InvalidOperationException($"Process exit code was 0x{process.ExitCode:X}!");
			Console.WriteLine();
		}

		Console.WriteLine($"Finished cmake for {lib}:{configuration}.");
		Console.WriteLine();
		Console.WriteLine();
	}
	finally {
		Directory.SetCurrentDirectory(scriptDir);
	}
}

void CopyLib(string lib, string configuration) {
	Console.WriteLine();
	Console.WriteLine($"===============================================");
	Console.WriteLine($"COPY | {lib.ToUpperInvariant()} | {configuration}");
	Console.WriteLine($"===============================================");
	var interimInstallDir = GetInterimInstallDirPath(lib, configuration);
	if (commandLineConfigArg != null || configuration == ReleaseConfiguration) {
		var destinationHeadersDir = GetDestinationHeadersPath(lib);
		Console.WriteLine($"Clearing contents from '{destinationHeadersDir}'...");
		if (Directory.Exists(destinationHeadersDir)) Directory.Delete(destinationHeadersDir, true);
		Directory.CreateDirectory(destinationHeadersDir);
		Console.WriteLine($"Searching for headers...");
		foreach (var h in interimInstallHeaderFolderNames) {
			var interimHeaderDir = Path.Combine(interimInstallDir, h);
			if (!Directory.Exists(interimHeaderDir)) {
				Console.WriteLine($"\tNone found at '{interimHeaderDir}'.");
				continue;
			}

			Console.WriteLine($"\tCopying everything from '{interimHeaderDir}'...");
			CopyDirectory(interimHeaderDir, interimHeaderDir, destinationHeadersDir);
		}
	}

	var destinationBinariesDir = GetDestinationBinariesPath(lib, configuration);
	Console.WriteLine($"Clearing contents from '{destinationBinariesDir}'...");
	if (Directory.Exists(destinationBinariesDir)) Directory.Delete(destinationBinariesDir, true);
	Directory.CreateDirectory(destinationBinariesDir);
	Console.WriteLine($"Searching for binaries...");
	foreach (var b in interimInstallBinaryFolderNames) {
		var interimBinaryDir = Path.Combine(interimInstallDir, b);
		if (!Directory.Exists(interimBinaryDir)) {
			Console.WriteLine($"\tNone found at '{interimBinaryDir}'.");
			continue;
		}

		Console.WriteLine($"\tCopying everything from '{interimBinaryDir}'...");
		CopyDirectory(interimBinaryDir, interimBinaryDir, destinationBinariesDir);
	}

	Console.WriteLine($"Finished copy for {lib}:{configuration}.");
	Console.WriteLine();
	Console.WriteLine();
}

static void CopyDirectory(string sourceDirOriginal, string sourceDir, string destinationDir) {
	var sourceDirInfo = new DirectoryInfo(sourceDir);
	Directory.CreateDirectory(destinationDir);

	foreach (var file in sourceDirInfo.GetFiles()) {
		file.CopyTo(Path.Combine(destinationDir, file.Name));
		Console.WriteLine(file.FullName[sourceDirOriginal.Length..]);
	}

	foreach (var subDir in sourceDirInfo.GetDirectories()) {
		CopyDirectory(sourceDirOriginal, subDir.FullName, Path.Combine(destinationDir, subDir.Name));
	}
}