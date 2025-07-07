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
const string ThirdPartyBinariesDirPath = "third_party/binaries/";
const string DebugConfiguration = "Debug";
const string ReleaseConfiguration = "Release";
var configurations = new[] { DebugConfiguration, ReleaseConfiguration };

// Script actions
void ExecuteLinux(string nativeProjDir, string config, List<string> thirdPartyBinaryFiles, string ultimateOutputDir) {
	const string ExpectedBuiltLibraryFileName = "libTinyFFR.Native.so";
	Environment.SetEnvironmentVariable("CC", "/usr/bin/clang");
	Environment.SetEnvironmentVariable("CXX", "/usr/bin/clang++");
	Environment.SetEnvironmentVariable("CXXFLAGS", "-stdlib=libc++");

	var createFilesArgs = $"\"{Path.Combine(nativeProjDir, "CMakeLists.txt")}\" -DCMAKE_C_COMPILER=clang -DCMAKE_CXX_COMPILER=clang++";
	Console.WriteLine($"> cmake {createFilesArgs}");
	var cmakeCreateFiles = Process.Start("cmake", createFilesArgs);
	cmakeCreateFiles.WaitForExit();
	if (cmakeCreateFiles.ExitCode != 0) throw new InvalidOperationException($"CMAKE files creation process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");

	var buildArgs = $"--build . --config {config}";
	Console.WriteLine($"> cmake {buildArgs}");
	var cmakeBuild = Process.Start("cmake", buildArgs);
	cmakeBuild.WaitForExit();
	if (cmakeBuild.ExitCode != 0) throw new InvalidOperationException($"CMAKE build process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");

	if (!File.Exists(ExpectedBuiltLibraryFileName)) {
		throw new InvalidOperationException($"Expected build script to create output file '{ExpectedBuiltLibraryFileName}' in {Environment.CurrentDirectory}; but no such file exists.");
	}

	Console.WriteLine($"Copying {ExpectedBuiltLibraryFileName} to {ultimateOutputDir}");

	File.Copy(ExpectedBuiltLibraryFileName, Path.Combine(ultimateOutputDir, ExpectedBuiltLibraryFileName), overwrite: true);

	foreach (var file in thirdPartyBinaryFiles) {
		var fileName = Path.GetFileName(file);
		Console.WriteLine($"Copying {fileName} to {ultimateOutputDir}");
		File.Copy(file, Path.Combine(ultimateOutputDir, fileName), overwrite: true);
	}
}
void ExecuteMacOS(string nativeProjDir, string config, List<string> thirdPartyBinaryFiles, string ultimateOutputDir) {
	const string ExpectedBuiltLibraryFileName = "libTinyFFR.Native.dylib";

	var createFilesArgs = $"\"{Path.Combine(nativeProjDir, "CMakeLists.txt")}\"";
	Console.WriteLine($"> cmake {createFilesArgs}");
	var cmakeCreateFiles = Process.Start("cmake", createFilesArgs);
	cmakeCreateFiles.WaitForExit();
	if (cmakeCreateFiles.ExitCode != 0) throw new InvalidOperationException($"CMAKE files creation process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");

	var buildArgs = $"--build . --config {config}";
	Console.WriteLine($"> cmake {buildArgs}");
	var cmakeBuild = Process.Start("cmake", buildArgs);
	cmakeBuild.WaitForExit();
	if (cmakeBuild.ExitCode != 0) throw new InvalidOperationException($"CMAKE build process exit code was 0x{cmakeCreateFiles.ExitCode:X}!");

	if (!File.Exists(ExpectedBuiltLibraryFileName)) {
		throw new InvalidOperationException($"Expected build script to create output file '{ExpectedBuiltLibraryFileName}' in {Environment.CurrentDirectory}; but no such file exists.");
	}

	Console.WriteLine($"Copying {ExpectedBuiltLibraryFileName} to {ultimateOutputDir}");

	File.Copy(ExpectedBuiltLibraryFileName, Path.Combine(ultimateOutputDir, ExpectedBuiltLibraryFileName), overwrite: true);

	foreach (var file in thirdPartyBinaryFiles) {
		var fileName = Path.GetFileName(file);
		Console.WriteLine($"Copying {fileName} to {ultimateOutputDir}");
		File.Copy(file, Path.Combine(ultimateOutputDir, fileName), overwrite: true);
	}
}
void ExecuteWindows(string nativeProjDir, string config, List<string> thirdPartyBinaryFiles, string ultimateOutputDir) {
	const string MsBuildLocation = @"%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";
	var buildArgs = $"\"{nativeProjDir}TinyFFR.Native.vcxproj\" /property:Configuration={config} /property:Platform=x64";
	Console.WriteLine($"> MSBuild.exe {buildArgs}");
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
	repoRootFullPath,
	BuildOutputDirName,
	BuildSpaceDirName
);
var ultimateOutputStartDir = Path.Combine(
	repoRootFullPath,
	BuildOutputDirName
);

var thirdPartyBinariesFullPath = Path.Combine(
	nativeProjDir.FullName,
	ThirdPartyBinariesDirPath
);

Console.WriteLine($"\tScript Dir: {scriptDir}");
Console.WriteLine($"\tNative Dir: {nativeProjDir.FullName}");
Console.WriteLine($"\tRepo Root Dir: {repoRootFullPath}");
Console.WriteLine($"\tInterim Build Dir: {interimBuildOutputDir}");
Console.WriteLine($"\tOutput Dir: {ultimateOutputStartDir}");
Console.WriteLine($"\tThird Party Binaries Dir: {thirdPartyBinariesFullPath}");

foreach (var configuration in configurations) {
	Environment.CurrentDirectory = scriptDir;
	if (Directory.Exists(interimBuildOutputDir)) Directory.Delete(interimBuildOutputDir, true);
	Directory.CreateDirectory(interimBuildOutputDir);
	Environment.CurrentDirectory = interimBuildOutputDir;
	var ultimateOutputDir = Path.Combine(ultimateOutputStartDir, configuration);
	Directory.CreateDirectory(ultimateOutputDir);

	var thirdPartyFilesList = Directory.GetDirectories(thirdPartyBinariesFullPath)
		.SelectMany(d => {
			var configDir = Path.Combine(d, configuration.ToLowerInvariant());
			if (!Directory.Exists(configDir)) {
				Console.WriteLine($"(Could not find {configDir}, skipping binary discovery for {d})");
				return Array.Empty<string>();
			}
			return Directory.GetFiles(configDir, "*", SearchOption.TopDirectoryOnly);
		})
		.ToList();

	if (OperatingSystem.IsWindows()) {
		Console.WriteLine($"\tExecuting for Windows / {configuration}...");
		ExecuteWindows(nativeProjDir.FullName, configuration, thirdPartyFilesList, ultimateOutputDir);
	}
	else if (OperatingSystem.IsLinux()) {
		Console.WriteLine($"\tExecuting for Linux / {configuration}...");
		ExecuteLinux(nativeProjDir.FullName, configuration, thirdPartyFilesList, ultimateOutputDir);
	}
	else if (OperatingSystem.IsMacOS()) {
		Console.WriteLine($"\tExecuting for MacOS / {configuration}...");
		ExecuteMacOS(nativeProjDir.FullName, configuration, thirdPartyFilesList, ultimateOutputDir);
	}
	else {
		throw new InvalidOperationException($"Unsupported OS '{Environment.OSVersion}'.");
	}
}