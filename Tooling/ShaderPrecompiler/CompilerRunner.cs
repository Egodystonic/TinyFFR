// Created on 2025-11-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Text;

namespace Egodystonic.TinyFFR;

static class CompilerRunner {
	public static void Execute(string matcLocation, string destinationDir, string[] sourceFiles, string[] compiledObjectsToDelete) {
		Console.WriteLine();
		Console.WriteLine();
		Console.WriteLine("==================================================");
		Console.WriteLine();
		Console.WriteLine();

		Console.WriteLine("Deleting files:");
		foreach (var file in compiledObjectsToDelete) {
			Console.WriteLine("\t" + file);
			File.Delete(file);
		}

		Console.WriteLine();
		Console.WriteLine("Preprocessing:");
		var processedFiles = ParseSourceFiles(sourceFiles).ToArray();
		Console.WriteLine("\tTotal: " + processedFiles.Length + " target objects");
	}

	record ProcessedFileContents(string DestinationFileName, string[] EnabledFlags, string ProcessedContents);

	static IEnumerable<ProcessedFileContents> ParseSourceFiles(string[] sourceFiles) {
		foreach (var file in sourceFiles) {
			var flagLines = new List<(string FlagName, int IfLineIdx, int EndIfLineIdx)>();
			
			Console.WriteLine("\t" + Path.GetFileName(file) + "...");
			var lines = File.ReadAllLines(file);
			
			var openFlags = new List<(string FlagName, int IfLineIdx)>();
			for (var lineIndex = 0; lineIndex < lines.Length; ++lineIndex) {
				var line = lines[lineIndex];
				var lineStartsFlag = line.StartsWith("#if ");
				var lineEndsFlag = line.StartsWith("#endif ");
				if (!lineStartsFlag && !lineEndsFlag) continue;
				
				var flag = String.Join(" ", line.Split(' ')[1..]).ToLowerInvariant();
				if (String.IsNullOrWhiteSpace(flag)) {
					throw new ApplicationException($"Empty if/endif in {Path.GetFileName(file)} on line {lineIndex + 1}");
				}

				if (lineStartsFlag) {
					var alreadyOpenFlagLine = openFlags.FirstOrDefault(tuple => tuple.FlagName.Equals(flag, StringComparison.OrdinalIgnoreCase));
					if (alreadyOpenFlagLine.FlagName != null) {
						throw new ApplicationException($"#if {flag} found on line {lineIndex + 1} although flag was already enabled on line {alreadyOpenFlagLine.IfLineIdx + 1}");
					}

					openFlags.Add((flag, lineIndex));
				}
				else if (lineEndsFlag) {
					var alreadyOpenFlagLine = openFlags.Index().FirstOrDefault(tuple => tuple.Item.FlagName.Equals(flag, StringComparison.OrdinalIgnoreCase));
					if (alreadyOpenFlagLine.Item.FlagName == null) {
						throw new ApplicationException($"#endif {flag} found on line {lineIndex + 1}, but no preceding #if");
					}

					openFlags.RemoveAt(alreadyOpenFlagLine.Index);
					flagLines.Add((flag, alreadyOpenFlagLine.Item.IfLineIdx, lineIndex));
					Console.WriteLine("\t\t" + flag + " [" + (alreadyOpenFlagLine.Index + 1) + ":" + (lineIndex + 1) + "]");
				}
			}

			if (openFlags.Any()) {
				throw new ApplicationException($"No #endif for {openFlags[0].FlagName} (opened on line {(openFlags[0].IfLineIdx + 1)})");
			}

			var destinationFileNameStart = Path.GetFileNameWithoutExtension(file)["shader_".Length..];
			foreach (var result in ProcessFile(destinationFileNameStart, lines, flagLines.ToArray())) {
				yield return result;
			}
		}
	}

	static IEnumerable<ProcessedFileContents> ProcessFile(string destinationFileNameStart, string[] fileLinesRaw, (string FlagName, int IfLineIdx, int EndIfLineIdx)[] flags) {
		var linesWithActiveFlagStates = new (string Text, string[] ActiveFlags)[fileLinesRaw.Length];
		var currentlyActiveFlags = new List<string>();
		for (var i = 0; i < fileLinesRaw.Length; ++i) {
			var flagTuple = flags.SingleOrDefault(tuple => tuple.IfLineIdx == i || tuple.EndIfLineIdx == i);
			if (flagTuple.FlagName != null) {
				if (flagTuple.IfLineIdx == i && !currentlyActiveFlags.Contains(flagTuple.FlagName)) {
					currentlyActiveFlags.Add(flagTuple.FlagName);
				}
				else {
					currentlyActiveFlags.Remove(flagTuple.FlagName);
				}
				linesWithActiveFlagStates[i] = ("", currentlyActiveFlags.ToArray());
			}
			else {
				linesWithActiveFlagStates[i] = (fileLinesRaw[i], currentlyActiveFlags.ToArray());
			}
		}

		IEnumerable<ProcessedFileContents> RecursivelyCreateContents(string[] enabledFlags, string[] flagsYetToBeDetermined) {
			if (flagsYetToBeDetermined.Length == 0) {
				var destFileName = destinationFileNameStart + String.Join("", enabledFlags.Select(f => "_" + f)) + ".filamat";
				Console.WriteLine("\t\t\t" + destFileName);

				var processedContentsBuilder = new StringBuilder();
				foreach (var line in linesWithActiveFlagStates) {
					if (line.ActiveFlags.Length == 0 || line.ActiveFlags.All(enabledFlags.Contains)) processedContentsBuilder.AppendLine(line.Text);
				}

				yield return new ProcessedFileContents(
					destFileName,
					enabledFlags,
					processedContentsBuilder.ToString()
				);

				yield break;
			}

			foreach (var withFlagOnResult in RecursivelyCreateContents(enabledFlags.Concat([flagsYetToBeDetermined[0]]).ToArray(), flagsYetToBeDetermined[1..])) {
				yield return withFlagOnResult;
			}
			foreach (var withFlagOffResult in RecursivelyCreateContents(enabledFlags, flagsYetToBeDetermined[1..])) {
				yield return withFlagOffResult;
			}
		}
		
		Console.WriteLine();
		var distinctFlags = flags.Select(tuple => tuple.FlagName).Distinct().Order().ToArray();
		Console.WriteLine("\t\tFlags:");
		foreach (var distinctFlag in distinctFlags) {
			Console.WriteLine("\t\t\t" + distinctFlag);
		}
		Console.WriteLine();
		Console.WriteLine("\t\tOutput object count: " + Math.Pow(2, distinctFlags.Length));

		foreach (var result in RecursivelyCreateContents(Array.Empty<string>(), distinctFlags)) {
			yield return result;
		}
	}
}