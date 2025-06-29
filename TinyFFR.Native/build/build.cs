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
const string ParentDirName = "TinyFFR.Native";
const string BuildOutputDirName = "build_output";
const string BuildSpaceDirName = "tffr_native_build_script_intermediate";
const string DebugConfiguration = "Debug";
const string ReleaseConfiguration = "Release";
var configurations = new[] { DebugConfiguration, ReleaseConfiguration };

// Script actions
// void ExecuteLinux(string nativeProjDir, string config) {
// 	Environment.SetEnvironmentVariable("CC", "/usr/bin/clang");
// 	Environment.SetEnvironmentVariable("CXX", "/usr/bin/clang++");
// 	Environment.SetEnvironmentVariable("CXXFLAGS", "-stdlib=libc++");
//
// 	commandList.AddRange([
// 		$"cmake \"{nativeProjDir}CMakeLists.txt\" -DCMAKE_C_COMPILER=clang -DCMAKE_CXX_COMPILER=clang++",
// 		$"cmake --build . --config {config}"
// 	]);
// }
// void ExecuteWindows(string nativeProjDir, string config) {
// 	const string MsBuildLocation = @"%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";
// 	commandList.AddRange([
// 		$"\"{MsBuildLocation}\" \"{nativeProjDir}TinyFFR.Native.vcxproj\" /property:Configuration={config} /property:Platform=x64"
// 	]);
// }

// ===================================================================================================================================
// Script starts here
// ===================================================================================================================================

// 1 -- Set up directories
Console.WriteLine("Starting Native Lib Build");

var scriptDir = Directory.GetCurrentDirectory();

if (!Path.GetFileName(scriptDir)?.Equals(ScriptDirName, StringComparison.OrdinalIgnoreCase) ?? false) {
	throw new InvalidOperationException($"You must execute this script from within the '{ScriptDirName}' directory (current dir = '{Path.GetFileName(scriptDir)}').");
}
var parentFullPath = Directory.GetParent(scriptDir);
if (parentFullPath?.Name != ParentDirName) {
	throw new InvalidOperationException($"You must execute this script in '{ParentDirName}/{ScriptDirName}' (actual path = '{parentFullPath?.Name}/{ScriptDirName}').");
}

var buildOutputDir = Path.Combine(
	parentFullPath.FullName, 
	BuildOutputDirName,
	BuildSpaceDirName
);
Console.WriteLine($"I want to delete {buildOutputDir}");

//
// if (interimBuildOutputDir == null) {
// 	interimBuildOutputDir = Directory.CreateDirectory(BuildOutputDirName).FullName;
// }
//
// var repositoryRootDir = Directory.GetParent(scriptDir)?.FullName;
//
// if (repositoryRootDir == null) {
// 	throw new InvalidOperationException($"Can not find repository root (should be parent of {scriptDir} but this dir has no parent).");
// }
// var thirdPartyNativeDir = Path.Combine(repositoryRootDir, ThirdPartyNativeDirPath);
//
// if (!Directory.Exists(thirdPartyNativeDir)) {
// 	_ = Directory.CreateDirectory(thirdPartyNativeDir);
// }
//
// string GetBuildOutputPath(string lib, string configuration) {
// 	return Path.Combine(interimBuildOutputDir, lib, configuration);
// }
// string GetThirdPartyRepoRootPath(string lib) {
// 	return Path.Combine(scriptDir, lib);
// }
// string GetInterimInstallDirPath(string lib, string configuration) {
// 	return Path.Combine(interimBuildOutputDir, lib, configuration, InterimInstallDirName);
// }
// string GetDestinationBinariesPath(string lib, string configuration) {
// 	return Path.Combine(thirdPartyNativeDir, ThirdPartyNativeBinariesDirName, lib.ToLowerInvariant(), configuration.ToLowerInvariant());
// }
// string GetDestinationHeadersPath(string lib) {
// 	return Path.Combine(thirdPartyNativeDir, ThirdPartyNativeHeadersDirName, lib.ToLowerInvariant());
// }
//
//
// // 2 -- Parse command line args
// var commandLineLibArg = libs.FirstOrDefault(l => Environment.GetCommandLineArgs().Any(a => a.Equals(l, StringComparison.OrdinalIgnoreCase)));
// var commandLineConfigArg = configurations.FirstOrDefault(c => Environment.GetCommandLineArgs().Any(a => a.Equals(c, StringComparison.OrdinalIgnoreCase)));
// var commandLineStepArg = stepNames.FirstOrDefault(s => Environment.GetCommandLineArgs().Any(a => a.Equals(s, StringComparison.OrdinalIgnoreCase)));
//
// Console.WriteLine($"Script Dir: {scriptDir}");
// Console.WriteLine($"Interim Build Output Dir: {interimBuildOutputDir}");
// Console.WriteLine($"Third Party Native Dir: {thirdPartyNativeDir}");
// if (commandLineLibArg == null) {
// 	Console.WriteLine("Executing for all third-party dependencies.");
// }
// else {
// 	Console.WriteLine($"Executing for dependency '{commandLineLibArg}' only.");
// }
// if (commandLineConfigArg == null) {
// 	Console.WriteLine("Executing for all configurations.");
// }
// else {
// 	Console.WriteLine($"Executing for configuration '{commandLineConfigArg}' only.");
// }
// if (commandLineStepArg == null) {
// 	Console.WriteLine("Executing all steps.");
// }
// else {
// 	Console.WriteLine($"Executing step '{commandLineStepArg}' only.");
// }
//
// // 3 -- Execute according to commandline
// if (commandLineStepArg == null || commandLineStepArg == BuildStep) {
// 	foreach (var configuration in configurations) {
// 		if (commandLineConfigArg != null && configuration != commandLineConfigArg) continue;
//
// 		if (commandLineLibArg != null) {
// 			BuildLib(commandLineLibArg, configuration);
// 		}
// 		else {
// 			BuildLib(LibAssimp, configuration);
// 			BuildLib(LibFilament, configuration);
// 			BuildLib(LibSdl, configuration);
// 		}
// 	}
// }
// if (commandLineStepArg == null || commandLineStepArg == CopyStep) {
// 	foreach (var configuration in configurations) {
// 		if (commandLineConfigArg != null && configuration != commandLineConfigArg) continue;
//
// 		if (commandLineLibArg != null) {
// 			CopyLib(commandLineLibArg, configuration);
// 		}
// 		else {
// 			CopyLib(LibAssimp, configuration);
// 			CopyLib(LibFilament, configuration);
// 			CopyLib(LibSdl, configuration);
// 		}
// 	}
// }
//
// Console.WriteLine("Script completed successfully.");
//
// // 4 -- Local functions for each step
// void BuildLib(string lib, string configuration) {
// 	try {
// 		Console.WriteLine();
// 		Console.WriteLine($"===============================================");
// 		Console.WriteLine($"BUILD | {lib.ToUpperInvariant()} | {configuration}");
// 		Console.WriteLine($"===============================================");
// 		Console.WriteLine($"Beginning build for {lib}...");
// 		var buildOutputDir = GetBuildOutputPath(lib, configuration);
// 		var thirdPartyRepoRootDir = GetThirdPartyRepoRootPath(lib);
// 		if (Directory.Exists(buildOutputDir)) Directory.Delete(buildOutputDir, true);
// 		Directory.CreateDirectory(buildOutputDir);
// 		Directory.SetCurrentDirectory(buildOutputDir);
// 		Console.WriteLine($"Build output directory: {buildOutputDir}");
// 		Console.WriteLine($"Repository root directory: {thirdPartyRepoRootDir}");
// 		Console.WriteLine();
//
// 		foreach (var invocation in commandLists[lib]) {
// 			var parsedInvocation = invocation
// 				.Replace(RepoRootDirToken, thirdPartyRepoRootDir)
// 				.Replace(BuildOutputDirToken, buildOutputDir)
// 				.Replace(ConfigurationToken, configuration);
//
// 			var invocationSplit = parsedInvocation.Split(' ');
// 			var commandletName = invocationSplit[0];
// 			var args = String.Join(' ', invocationSplit[1..]);
//
// 			Console.WriteLine();
// 			Console.WriteLine($"> {commandletName} {args}");
// 			var process = Process.Start(commandletName, args);
// 			process.WaitForExit();
// 			if (process.ExitCode != 0) throw new InvalidOperationException($"Process exit code was 0x{process.ExitCode:X}!");
// 			Console.WriteLine();
// 		}
//
// 		Console.WriteLine($"Finished {BuildStep} for {lib}:{configuration}.");
// 		Console.WriteLine();
// 		Console.WriteLine();
// 	}
// 	catch {
// 		Console.WriteLine($"Error occurred during {BuildStep} for {lib}:{configuration}!");
// 		throw;
// 	}
// 	finally {
// 		Directory.SetCurrentDirectory(scriptDir);
// 	}
// }
//
// void CopyLib(string lib, string configuration) {
// 	try {
// 		Console.WriteLine();
// 		Console.WriteLine($"===============================================");
// 		Console.WriteLine($"COPY | {lib.ToUpperInvariant()} | {configuration}");
// 		Console.WriteLine($"===============================================");
// 		var interimInstallDir = GetInterimInstallDirPath(lib, configuration);
// 		if (commandLineConfigArg != null || configuration == ReleaseConfiguration) {
// 			var destinationHeadersDir = GetDestinationHeadersPath(lib);
// 			Console.WriteLine($"Writing to '{destinationHeadersDir.Replace('\\', '/')}'...");
// 			if (Directory.Exists(destinationHeadersDir)) Directory.Delete(destinationHeadersDir, true);
// 			Directory.CreateDirectory(destinationHeadersDir);
// 			foreach (var h in interimInstallHeaderFolderNames) {
// 				var interimHeaderDir = Path.Combine(interimInstallDir, h);
// 				if (!Directory.Exists(interimHeaderDir)) {
// 					continue;
// 				}
//
// 				CopyDirectory(interimInstallDir, destinationHeadersDir, h, installDirTranslations[lib]);
// 			}
// 		}
//
// 		var destinationBinariesDir = GetDestinationBinariesPath(lib, configuration);
// 		Console.WriteLine($"Writing to '{destinationBinariesDir.Replace('\\', '/')}'...");
// 		if (Directory.Exists(destinationBinariesDir)) Directory.Delete(destinationBinariesDir, true);
// 		Directory.CreateDirectory(destinationBinariesDir);
// 		foreach (var b in interimInstallBinaryFolderNames) {
// 			var interimBinaryDir = Path.Combine(interimInstallDir, b);
// 			if (!Directory.Exists(interimBinaryDir)) {
// 				continue;
// 			}
//
// 			CopyDirectory(interimInstallDir, destinationBinariesDir, b, installDirTranslations[lib]);
// 		}
//
// 		Console.WriteLine($"Finished {CopyStep} for {lib}:{configuration}.");
// 		Console.WriteLine();
// 		Console.WriteLine();
// 	}
// 	catch {
// 		Console.WriteLine($"Error occurred during {CopyStep} for {lib}:{configuration}!");
// 		throw;
// 	}
// }
//
// void CopyDirectory(string interimInstallRootDir, string destinationRootDir, string curSubDir, Func<TargetFileData, string?> relativeFilePathMutator) {
// 	var sourceDirInfo = new DirectoryInfo(Path.Combine(interimInstallRootDir, curSubDir));
//
// 	foreach (var file in sourceDirInfo.GetFiles()) {
// 		var relativeDestSplit = curSubDir.TrimStart(Path.DirectorySeparatorChar, '/', '\\')
// 			.Split([Path.DirectorySeparatorChar, '/', '\\'], StringSplitOptions.RemoveEmptyEntries);
// 		var relativeDest = Path.Combine(
// 			String.Join(Path.DirectorySeparatorChar, relativeDestSplit[1..]),
// 			file.Name
// 		);
//
// 		var fileData = new TargetFileData(
// 			FullSource: file.FullName.Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
// 			RelativeSource: Path.GetRelativePath(interimInstallRootDir, file.FullName).Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
// 			FullDestination: Path.Combine(destinationRootDir, relativeDest).Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/'),
// 			RelativeDestination: relativeDest.Replace('\\', '/').Replace(Path.DirectorySeparatorChar, '/')
// 		);
//
// 		var mutatedRelativeDest = relativeFilePathMutator(fileData);
// 		if (mutatedRelativeDest == null) {
// 			Console.WriteLine($"\t{fileData.RelativeSource}   ->   [skip]");
// 		}
// 		else {
// 			Console.WriteLine($"\t{fileData.RelativeSource}   ->   {mutatedRelativeDest}");
// 			var finalizedDest = Path.Combine(destinationRootDir, mutatedRelativeDest.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
// 			var destDir = Path.GetDirectoryName(finalizedDest);
// 			if (destDir != null && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
// 			file.CopyTo(finalizedDest);
// 		}
// 	}
//
// 	foreach (var subDir in sourceDirInfo.GetDirectories()) {
// 		CopyDirectory(interimInstallRootDir, destinationRootDir, Path.GetRelativePath(interimInstallRootDir, subDir.FullName), relativeFilePathMutator);
// 	}
// }
//
// sealed record TargetFileData(string FullSource, string RelativeSource, string FullDestination, string RelativeDestination);