// Created on 2025-11-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
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

		Console.WriteLine();
		Console.WriteLine("Compiling:");
		CompileAll(matcLocation, destinationDir, processedFiles);

		Console.WriteLine();
		Console.WriteLine("Completed.");
	}

	record FileOption(string Name, string[] Variants);
	record ProcessedFileContents(string DestinationFileName, string ProcessedContents);

	static IEnumerable<ProcessedFileContents> ParseSourceFiles(string[] sourceFiles) {
		foreach (var file in sourceFiles) {
			var options = new List<FileOption>();
			var flagLines = new List<(string FlagName, int IfLineIdx, int EndIfLineIdx)>();
			
			Console.WriteLine("\t" + Path.GetFileName(file) + "...");
			var lines = File.ReadAllLines(file);
			
			var openFlags = new List<(string FlagName, int IfLineIdx)>();
			var openOption = (string?) null;
			var variants = new List<string>();
			for (var lineIndex = 0; lineIndex < lines.Length; ++lineIndex) {
				var line = lines[lineIndex];
				var lineStartsFlag = line.StartsWith("#if ");
				var lineEndsFlag = line.StartsWith("#endif ");
				var lineStartsOption = line.StartsWith("#option ");
				var lineEndsOption = line.StartsWith("#endoption");
				var lineDemarcatesVariant = line.StartsWith("#variant ");
				
				if (lineStartsOption) {
					if (openOption != null) {
						throw new ApplicationException($"Can not embed options (line {lineIndex + 1})");
					}
					openOption = String.Join(" ", line.Split(' ')[1..]).ToLowerInvariant();
					continue;
				}
				if (lineEndsOption) {
					if (openOption == null) {
						throw new ApplicationException($"Endoption without one being started (line {lineIndex + 1})");
					}
					options.Add(new(openOption, variants.ToArray()));
					Console.WriteLine("\t\t" + openOption + " [" + String.Join("/", variants) + "] end @ " + (lineIndex + 1));
					openOption = null;
					variants.Clear();
					continue;
				}
				if (lineDemarcatesVariant) {
					if (openOption == null) {
						throw new ApplicationException($"Variant outside of option (line {lineIndex + 1})");
					}
					var variant = String.Join(" ", line.Split(' ')[1..]).ToLowerInvariant();
					if (String.IsNullOrWhiteSpace(variant)) {
						throw new ApplicationException($"Variant tag with no name (line {lineIndex + 1})");
					}
					variants.Add(variant);
					continue;
				}
				
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
			if (openOption != null) {
				throw new ApplicationException($"No endoption for {openOption}");
			}

			var destinationFileNameStart = Path.GetFileNameWithoutExtension(file)["shader_".Length..];
			var optionsArray = options.GroupBy(o => o.Name).Select(g => new FileOption(g.Key, g.SelectMany(o => o.Variants).Distinct().ToArray())).ToArray();
			foreach (var result in ProcessFile(destinationFileNameStart, lines, flagLines.ToArray(), optionsArray)) {
				yield return result;
			}
		}
	}

	static IEnumerable<ProcessedFileContents> ProcessFile(string destinationFileNameStart, string[] fileLinesRaw, (string FlagName, int IfLineIdx, int EndIfLineIdx)[] flags, FileOption[] options) {
		var linesWithActiveFlagStates = new List<(string Text, string[] ActiveFlags, (string Option, string Variant)? OptionVariant)>();
		var currentlyActiveFlags = new List<string>();
		var activeOption = (string?) null;
		var activeVariant = (string?) null;
		for (var i = 0; i < fileLinesRaw.Length; ++i) {
			var flagTuple = flags.SingleOrDefault(tuple => tuple.IfLineIdx == i || tuple.EndIfLineIdx == i);
			if (flagTuple.FlagName != null) {
				if (flagTuple.IfLineIdx == i && !currentlyActiveFlags.Contains(flagTuple.FlagName)) {
					currentlyActiveFlags.Add(flagTuple.FlagName);
				}
				else {
					currentlyActiveFlags.Remove(flagTuple.FlagName);
				}
			}
			else if (fileLinesRaw[i].StartsWith("#option")) {
				activeOption = String.Join(" ", fileLinesRaw[i].Split(' ')[1..]).ToLowerInvariant();
			}
			else if (fileLinesRaw[i].StartsWith("#variant")) {
				activeVariant = String.Join(" ", fileLinesRaw[i].Split(' ')[1..]).ToLowerInvariant();
			}
			else if (fileLinesRaw[i].StartsWith("#endoption")) {
				activeVariant = null;
				activeOption = null;
			}
			else {
				linesWithActiveFlagStates.Add((fileLinesRaw[i], currentlyActiveFlags.ToArray(), activeVariant == null ? null : (activeOption!, activeVariant)));
			}
		}

		IEnumerable<ProcessedFileContents> RecursivelyCreateContents(Dictionary<string, string> activeVariants, string[] enabledFlags, string[] flagsYetToBeDetermined) {
			if (flagsYetToBeDetermined.Length == 0) {
				var nameToken = destinationFileNameStart 
								+ String.Join("", activeVariants.OrderBy(kvp => kvp.Key).Select(kvp => $"_{kvp.Key}={kvp.Value}")) 
								+ String.Join("", enabledFlags.Select(f => "_" + f));
				var destFileName = nameToken + ".filamat";

				var processedContentsBuilder = new StringBuilder();
				foreach (var line in linesWithActiveFlagStates) {
					if (line.ActiveFlags.Length != 0 && !line.ActiveFlags.All(enabledFlags.Contains)) {
						continue;
					}
					if (line.OptionVariant != null && activeVariants[line.OptionVariant.Value.Option] != line.OptionVariant.Value.Variant) {
						continue;
					}

					processedContentsBuilder.AppendLine(line.Text.Replace("%NAME%", "\"" + nameToken + "\""));
				}

				yield return new ProcessedFileContents(destFileName, processedContentsBuilder.ToString());

				yield break;
			}

			foreach (var withFlagOnResult in RecursivelyCreateContents(activeVariants, enabledFlags.Concat([flagsYetToBeDetermined[0]]).ToArray(), flagsYetToBeDetermined[1..])) {
				yield return withFlagOnResult;
			}
			foreach (var withFlagOffResult in RecursivelyCreateContents(activeVariants, enabledFlags, flagsYetToBeDetermined[1..])) {
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
		Console.WriteLine("\t\tVariants:");
		foreach (var o in options) {
			Console.WriteLine("\t\t\t" + o.Name + " [" + String.Join("/", o.Variants) + "]");
		}
		Console.WriteLine();
		Console.WriteLine("\t\tOutput object count: " + Math.Pow(2, distinctFlags.Length) * options.Aggregate(1, (m, o) => m * o.Variants.Length));
		Console.WriteLine();


		if (options.Length == 0) {
			foreach (var result in RecursivelyCreateContents(new(), Array.Empty<string>(), distinctFlags)) {
				yield return result;
			}
		}
		else {
			var variantGroups = options.Aggregate(
				new List<Dictionary<string, string>>(),
				(oldList, option) => {
					var newList = new List<Dictionary<string, string>>();
					foreach (var v in option.Variants) {
						if (oldList.Count == 0) {
							newList.Add(new Dictionary<string, string> { [option.Name] = v });
						}
						else {
							foreach (var d in oldList) {
								var newDict = new Dictionary<string, string>(d);
								newDict[option.Name] = v;
								newList.Add(newDict);
							}
						}
					}
					return newList;
				}
			);
			foreach (var g in variantGroups) {
				foreach (var result in RecursivelyCreateContents(g, Array.Empty<string>(), distinctFlags)) {
					yield return result;
				}
			}
		}
	}

	static void CompileAll(string matcLocation, string destinationDir, ProcessedFileContents[] files) {
		var errorLock = new Lock();
		var errorBuilder = new StringBuilder();
		var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Egodystonic", "TinyFFR", "ShaderPrecompiler");
		if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
		Environment.CurrentDirectory = tempFolder;

		Console.WriteLine("\tUsing temp folder: " + Environment.CurrentDirectory);

		var completedFileCount = 0;
		try {
			Parallel.ForEach(files, file => {
				var tempFileName = file.DestinationFileName + ".txt";
				var destFileName = Path.Combine(destinationDir, file.DestinationFileName);
				File.WriteAllText(tempFileName, file.ProcessedContents);
				var proc = Process.Start(matcLocation, $"-p desktop -a all -o \"{destFileName}\" \"{tempFileName}\"");
				proc.WaitForExit();
				Console.WriteLine("\t\t\t" + file.DestinationFileName);
				if (proc.ExitCode != 0) {
					lock (errorLock) {
						errorBuilder.AppendLine();
						errorBuilder.AppendLine("===========================================");
						errorBuilder.AppendLine($"Error ({file.DestinationFileName})");
						errorBuilder.AppendLine("===========================================");
						errorBuilder.Append(file.ProcessedContents);
						errorBuilder.AppendLine("===========================================");
						errorBuilder.AppendLine();
					}
					throw new ApplicationException("Error when compiling " + tempFileName);
				}

				using (var zf = ZipFile.Open(destFileName + ".zip", ZipArchiveMode.Create)) {
					_ = zf.CreateEntryFromFile(destFileName, "x", CompressionLevel.SmallestSize);
				}

				File.Delete(destFileName);
				var numFilesCompleted = Interlocked.Increment(ref completedFileCount);
				if (numFilesCompleted % 20 == 0) {
					Console.WriteLine(Environment.NewLine + $"\t\t\tProgress: {(numFilesCompleted / (float) files.Length) * 100f:N0}%" + Environment.NewLine);
				}
			});
		}
		finally {
			lock (errorLock) {
				Console.Write(errorBuilder.ToString());
			}
		}
	}
}