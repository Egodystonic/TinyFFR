// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Assets.Local;

public interface ILocalAssetLoader : IAssetLoader {
	internal static string HdrToKtxWorkspaceDirectoryPath { get; } = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, "HdrToKtxWorkspace");

	void PreprocessHdrTextureToEnvironmentCubemapDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath);
	EnvironmentCubemap LoadEnvironmentCubemapFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadEnvironmentCubemapFromPreprocessedHdrDirectory(directoryPath, new EnvironmentCubemapCreationConfig { Name = name });
	}
	EnvironmentCubemap LoadEnvironmentCubemapFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, in EnvironmentCubemapCreationConfig config);
	EnvironmentCubemap LoadEnvironmentCubemap(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> name = default) {
		return LoadEnvironmentCubemap(hdrFilePath, new EnvironmentCubemapCreationConfig { Name = name });
	}
	// TODO xmldoc that this is really slow and generates a lot of garbage
	EnvironmentCubemap LoadEnvironmentCubemap(ReadOnlySpan<char> hdrFilePath, in EnvironmentCubemapCreationConfig config) {
		var targetDir = HdrToKtxWorkspaceDirectoryPath;
		try {
			Directory.CreateDirectory(targetDir);
			foreach (var file in Directory.GetFiles(targetDir, "*.ktx")) File.Delete(file);
			
			PreprocessHdrTextureToEnvironmentCubemapDirectory(hdrFilePath, targetDir);
			return LoadEnvironmentCubemapFromPreprocessedHdrDirectory(targetDir, config);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not generate and load HDR file: When attempting to preprocess " +
												$"and generate KTX files in directory '{targetDir}', an error was encountered.", e);
		}
	}
}