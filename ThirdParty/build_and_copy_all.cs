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
const string ThirdPartyNativeDirPath = "TinyFFR.Native/third_party";

const string LibAssimp = "assimp";
const string LibFilament = "filament";
const string LibSdl = "sdl";

const string RepoRootDirToken = "%REPODIR%";
const string BuildOutputDirToken = "%BUILDOUTDIR%";
const string ConfigurationToken = "%CONFIG%";
var cmakeInvocationsDict = new Dictionary<string, List<string>> {
	[LibAssimp] = new() {
		$"-DCMAKE_INSTALL_PREFIX=install \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	},
	[LibFilament] = new() {
		$"\"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	},
	[LibSdl] = new() {
		$"-DCMAKE_INSTALL_PREFIX=install \"{RepoRootDirToken}/CMakeLists.txt\"",
		$"--build . --config {ConfigurationToken}",
		$"--build . --target install --config {ConfigurationToken}",
	}
};

// ===================================================================================================================================
// Script starts here
// ===================================================================================================================================

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

Console.WriteLine($"Script Dir: {scriptDir}");
Console.WriteLine($"Interim Build Output Dir: {interimBuildOutputDir}");
Console.WriteLine($"Third Party Native Dir: {thirdPartyNativeDir}");

string GetBuildOutputDirForLib(string lib, string configuration) {
	return Path.Combine(interimBuildOutputDir, lib, configuration);
}
string GetThirdPartyRepoRootDirForLib(string lib) {
	return Path.Combine(scriptDir, lib);
}

void InvokeCmakeForLib(string lib, string configuration) {
	try {
		Console.WriteLine();
		Console.WriteLine($"===============================================");
		Console.WriteLine($"{lib.ToUpperInvariant()} | {configuration}");
		Console.WriteLine($"===============================================");
		Console.WriteLine($"Beginning cmake for {lib}...");
		var buildOutputDir = GetBuildOutputDirForLib(lib, configuration);
		var thirdPartyRepoRootDir = GetThirdPartyRepoRootDirForLib(lib);
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

		Console.WriteLine($"Finished cmake for {lib}.");
		Console.WriteLine();
		Console.WriteLine();
	}
	finally {
		Directory.SetCurrentDirectory(scriptDir);
	}
}

var commandLineLibArg = cmakeInvocationsDict.Keys.FirstOrDefault(k => Environment.GetCommandLineArgs().Any(a => a.Equals(k, StringComparison.OrdinalIgnoreCase)));

foreach (var configuration in new[] { "Debug", "Release" }) {
	if (commandLineLibArg != null) {
		InvokeCmakeForLib(commandLineLibArg, configuration);
	}
	else {
		InvokeCmakeForLib(LibAssimp, configuration);
		InvokeCmakeForLib(LibFilament, configuration);
		InvokeCmakeForLib(LibSdl, configuration);
	}
}
