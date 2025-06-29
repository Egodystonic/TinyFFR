using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// ===================================================================================================================================
// Script constants
// ===================================================================================================================================

// Directory structures + command tokens
const string ScriptDirName = "build";
const string NativeProjDirName = "TinyFFR.Native";
const string BuildOutputDirName = "build_output";
const string BuildSpaceDirName = "tffr_native_build_script_intermediate";
const string DebugConfiguration = "Debug";
const string ReleaseConfiguration = "Release";
var configurations = new[] { DebugConfiguration, ReleaseConfiguration };

// Script actions
void ExecuteLinux(string nativeProjDir, string config, string ultimateOutputDir) {
	Environment.SetEnvironmentVariable("CC", "/usr/bin/clang");
	Environment.SetEnvironmentVariable("CXX", "/usr/bin/clang++");
	Environment.SetEnvironmentVariable("CXXFLAGS", "-stdlib=libc++");

	var createFilesArgs = $"\"{nativeProjDir}CMakeLists.txt\" -DCMAKE_C_COMPILER=clang -DCMAKE_CXX_COMPILER=clang++";
	Console.WriteLine($"\t\t> cmake {createFilesArgs}");
	var cmakeCreateFiles = Process.Start("cmake", createFilesArgs);
	cmakeCreateFiles.WaitForExit();
	if (cmakeCreateFiles.ExitCode != 0) throw new InvalidOperationException($"CMAKE files creation process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");

	var buildArgs = $"--build . --config {config}";
	Console.WriteLine($"\t\t> cmake {buildArgs}");
	var cmakeBuild = Process.Start("cmake", buildArgs);
	cmakeBuild.WaitForExit();
	if (cmakeBuild.ExitCode != 0) throw new InvalidOperationException($"CMAKE build process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");
}
void ExecuteWindows(string nativeProjDir, string config, string ultimateOutputDir) {
	const string MsBuildLocation = @"%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";
	var buildArgs = $"\"{nativeProjDir}TinyFFR.Native.vcxproj\" /property:Configuration={config} /property:Platform=x64";
	Console.WriteLine($"\t\t> MSBuild.exe {buildArgs}");
	var msBuild = Process.Start(MsBuildLocation, buildArgs);
	msBuild.WaitForExit();
	if (msBuild.ExitCode != 0) throw new InvalidOperationException($"MSBUILD process exist code was 0x{msBuild.ExitCode:X}!");
}

// ===================================================================================================================================
// Script starts here
// ===================================================================================================================================

// 1 -- Set up directories
Console.WriteLine("Starting Native Lib Build");

var scriptDir = Directory.GetCurrentDirectory();

if (!Path.GetFileName(scriptDir)?.Equals(ScriptDirName, StringComparison.OrdinalIgnoreCase) ?? false) {
	throw new InvalidOperationException($"You must execute this script from within the '{ScriptDirName}' directory (current dir = '{Path.GetFileName(scriptDir)}').");
}
var nativeProjDir = Directory.GetParent(scriptDir);
if (nativeProjDir?.Name != NativeProjDirName) {
	throw new InvalidOperationException($"You must execute this script in '{NativeProjDirName}/{ScriptDirName}' (actual path = '{nativeProjDir?.Name}/{ScriptDirName}').");
}

var repoRootFullPath = Directory.GetParent(nativeProjDir.FullName)?.FullName;
if (repoRootFullPath == null) {
	throw new InvalidOperationException($"Could not get repository root dir (parent of {NativeProjDirName} did not exist).");
}

var interimBuildOutputDir = Path.Combine(
	nativeProjDir.FullName, 
	BuildOutputDirName,
	BuildSpaceDirName
);
var ultimateOutputStartDir = Path.Combine(
	repoRootFullPath,
	BuildOutputDirName
);

Console.WriteLine($"\tScript Dir: {scriptDir}");
Console.WriteLine($"\tNative Dir: {nativeProjDir.FullName}");
Console.WriteLine($"\tRepo Root Dir: {repoRootFullPath}");
Console.WriteLine($"\tInterim Build Dir: {interimBuildOutputDir}");
Console.WriteLine($"\tOutput Dir: {ultimateOutputStartDir}");

foreach (var configuration in configurations) {
	Environment.CurrentDirectory = scriptDir;
	if (Directory.Exists(interimBuildOutputDir)) Directory.Delete(interimBuildOutputDir, true);
	Directory.CreateDirectory(interimBuildOutputDir);
	Environment.CurrentDirectory = interimBuildOutputDir;
	var ultimateOutputDir = Path.Combine(ultimateOutputStartDir, configuration);
	Directory.CreateDirectory(ultimateOutputDir);

	if (OperatingSystem.IsWindows()) {
		Console.WriteLine($"\tExecuting for Windows / {configuration}...");
		ExecuteWindows(nativeProjDir.FullName, configuration, ultimateOutputDir);
	}
	else if (OperatingSystem.IsLinux()) {
		Console.WriteLine($"\tExecuting for Linux / {configuration}...");
		ExecuteLinux(nativeProjDir.FullName, configuration, ultimateOutputDir);
	}
	else {
		throw new InvalidOperationException($"Unsupported OS '{Environment.OSVersion}'.");
	}
}