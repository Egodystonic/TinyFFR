// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.Assets.Local;

public enum BackdropTextureResolution {
	Standard,
	RoughDraft,
	Higher,
	VeryHigh,
	Production
}

public interface ILocalAssetLoader : IAssetLoader {
	internal static string HdrExrToKtxWorkspaceDirectoryPath { get; } = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, "HdrExrToKtxWorkspace");

	void PreprocessHdrOrExrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard);
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectory(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTexture(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name });
	}
	// TODO xmldoc that this is really slow and generates a lot of garbage
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard) {
		var targetDir = HdrExrToKtxWorkspaceDirectoryPath;
		try {
			Directory.CreateDirectory(targetDir);
			foreach (var file in Directory.GetFiles(targetDir, "*.ktx")) File.Delete(file);
			
			PreprocessHdrOrExrTextureToBackdropTextureDirectory(hdrOrExrFilePath, targetDir, backdropTextureResolution);
			return LoadBackdropTextureFromPreprocessedDirectory(targetDir, config);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not generate and load HDR file: When attempting to preprocess " +
												$"and generate KTX files in directory '{targetDir}', an error was encountered.", e);
		}
	}
}