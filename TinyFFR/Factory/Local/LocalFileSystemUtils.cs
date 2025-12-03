// Created on 2025-03-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;

namespace Egodystonic.TinyFFR.Factory.Local;

static class LocalFileSystemUtils {
	public static readonly string ApplicationDataDirectoryPath = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
		"Egodystonic",
		"TinyFFR"
	);

	public static void AttemptToEnsureApplicationDataFolderExists() {
		try {
			if (!Directory.Exists(ApplicationDataDirectoryPath)) Directory.CreateDirectory(ApplicationDataDirectoryPath);
		}
		catch (Exception e) when (ExceptionIndicatesGeneralIoError(e)) {
			Console.WriteLine($"Could not ensure existence of data application folder '{ApplicationDataDirectoryPath}': {e}/{e.Message}");
		}
	}

	public static bool ExceptionIndicatesGeneralIoError(Exception e) {
		return e is IOException or DirectoryNotFoundException or UnauthorizedAccessException or PathTooLongException or NotSupportedException;
	}
}