// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.Assets.Local;

public interface ILocalAssetLoader : IAssetLoader {
	void PreprocessHdrTextureToEnvironmentCubemapDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath);
	EnvironmentCubemap LoadEnvironmentCubemapFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default);
	// TODO xmldoc that this is really slow and generates a lot of garbage
	EnvironmentCubemap LoadEnvironmentCubemap(ReadOnlySpan<char> hdrFilePath) {
		var targetDir = Path.Combine(
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
			"Egodystonic",
			"TinyFFR",
			"HdrToKtxWorkspace"
		);
		try {
			Directory.CreateDirectory(targetDir);
			foreach (var file in Directory.GetFiles(targetDir, "*.ktx")) File.Delete(file);
			
			PreprocessHdrTextureToEnvironmentCubemapDirectory(hdrFilePath, targetDir);
			return LoadEnvironmentCubemapFromPreprocessedHdrDirectory(targetDir);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not generate and load HDR file: When attempting to preprocess " +
												$"and generate KTX files in directory '{targetDir}', an error was encountered.", e);
		}
	}
}