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
}