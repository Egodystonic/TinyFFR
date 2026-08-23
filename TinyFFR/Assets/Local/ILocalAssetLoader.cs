// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Threading;

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
	TinyFfrAsyncOperation PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard);

	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectory(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectoryAsync(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);

	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTexture(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	// TODO xmldoc that this is really slow and generates a lot of garbage
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard);
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureAsync(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard);
}