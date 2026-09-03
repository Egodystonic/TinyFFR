// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

public interface ILocalAssetLoader : IAssetLoader {
	internal static string HdrExrToKtxWorkspaceDirectoryPath { get; } = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, "HdrExrToKtxWorkspace");

	void PreprocessHdrOrExrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, Quality backdropTextureResolution = Quality.Standard);
	TinyFfrAsyncOperation PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, Quality backdropTextureResolution = Quality.Standard);

	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectory(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectoryAsync(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);

	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, Quality backdropTextureResolution = Quality.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTexture(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	// TODO xmldoc that this is really slow and generates a lot of garbage
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, Quality backdropTextureResolution = Quality.Standard);
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, Quality backdropTextureResolution = Quality.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureAsync(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, Quality backdropTextureResolution = Quality.Standard);
}