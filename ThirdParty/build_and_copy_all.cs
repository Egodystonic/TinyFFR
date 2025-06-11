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

const string ThirdPartyRepoRootDirToken = "%TPROOTDIR%";
var cmakeInvocationsDict = new Dictionary<string, List<string>> {
	[LibAssimp] = new() {
		$"{ThirdPartyRepoRootDirToken}/CMakeLists.txt",
		$"--build . --config Debug",
		$"--build . --config Release",
	},
	[LibFilament] = new() {
		$"{ThirdPartyRepoRootDirToken}/CMakeLists.txt",
		$"--build . --config Debug",
		$"--build . --config Release",
	},
	[LibSdl] = new() {
		$"{ThirdPartyRepoRootDirToken}/CMakeLists.txt",
		$"--build . --config Debug",
		$"--build . --config Release",
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

Directory.Delete(interimBuildOutputDir, true);
Directory.CreateDirectory(interimBuildOutputDir);

string GetBuildOutputDirForLib(string lib) {
	return Path.Combine(interimBuildOutputDir, lib);
}
string GetThirdPartyRepoRootDirForLib(string lib) {
	return Path.Combine(scriptDir, lib);
}

void InvokeCmakeForLib(string lib) {
	try {
		Console.WriteLine();
		Console.WriteLine($"===============================================");
		Console.WriteLine($"{lib.ToUpperInvariant()}");
		Console.WriteLine($"===============================================");
		Console.WriteLine($"Beginning cmake for {lib}...");
		var buildOutputDir = GetBuildOutputDirForLib(lib);
		var thirdPartyRepoRootDir = GetThirdPartyRepoRootDirForLib(lib);
		if (!Directory.Exists(buildOutputDir)) Directory.CreateDirectory(buildOutputDir);
		Directory.SetCurrentDirectory(buildOutputDir);
		Console.WriteLine($"Build output directory: {buildOutputDir}");
		Console.WriteLine($"Repository root directory: {thirdPartyRepoRootDir}");
		Console.WriteLine();
		
		foreach (var invocation in cmakeInvocationsDict[lib]) {
			var parsedInvocation = invocation.Replace(ThirdPartyRepoRootDirToken, thirdPartyRepoRootDir);
			Console.WriteLine();
			Console.WriteLine($"> cmake {parsedInvocation}");
			var process = Process.Start("cmake", parsedInvocation);
			process.WaitForExit();
			if (process.ExitCode != 0) throw new InvalidOperationException($"Process exit code was 0x{process.ExitCode:X8}!");
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

InvokeCmakeForLib(LibAssimp);
InvokeCmakeForLib(LibFilament);
InvokeCmakeForLib(LibSdl);