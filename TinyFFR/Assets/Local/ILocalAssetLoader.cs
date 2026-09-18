// Created on 2025-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

/// <summary>
/// The asset loader used when TinyFFR is rendering on this machine, adding the ability to build scene backdrops from high-dynamic-range images.
/// </summary>
/// <remarks>
/// Everything else an asset loader does is on <see cref="IAssetLoader"/>; backdrop creation is here because it depends on this machine's GPU to do the conversion.
/// </remarks>
public interface ILocalAssetLoader : IAssetLoader {
	internal static string HdrExrToKtxWorkspaceDirectoryPath { get; } = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, "HdrExrToKtxWorkspace");

	/// <summary>
	/// Converts a high-dynamic-range image in to the readily-supported <c>.ktx</c> and <c>.ibl</c> format files, and writes them to a directory.
	/// </summary>
	/// <remarks>
	/// This is the slow step of creating a backdrop, done once so that it need not be repeated. Note that this is still a slower result to
	/// load than a baked backdrop texture (i.e. loaded via <see cref="IAssetLoader.LoadBakedBackdropTexture"/>) but is more universally portable.
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="destinationDirectoryPath">The directory to write the preprocessed files in to. It is created if it does not exist.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	void PreprocessHdrOrExrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, Quality backdropTextureResolution = Quality.Standard);
	/// <summary>
	/// Asynchronously converts a high-dynamic-range image in to the readily-supported <c>.ktx</c> and <c>.ibl</c> format files, and writes them to a directory.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is the slow step of creating a backdrop, done once so that it need not be repeated. Note that this is still a slower result to
	/// load than a baked backdrop texture (i.e. loaded via <see cref="IAssetLoader.LoadBakedBackdropTextureAsync"/>) but is more universally portable.
	/// </para>
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="destinationDirectoryPath">The directory to write the preprocessed files in to. It is created if it does not exist.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	TinyFfrAsyncOperation PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, Quality backdropTextureResolution = Quality.Standard);

	/// <summary>
	/// Loads a backdrop from a directory previously written by <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> (or otherwise
	/// containing exactly one <c>.ibl</c> and one <c>.ktx</c> file).
	/// </summary>
	/// <param name="directoryPath">The path of the directory holding the preprocessed backdrop files.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectory(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	/// <summary>
	/// Loads a backdrop from a directory previously written by <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> (or otherwise
	/// containing exactly one <c>.ibl</c> and one <c>.ktx</c> file), using the given config.
	/// </summary>
	/// <param name="directoryPath">The path of the directory holding the preprocessed backdrop files.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);
	/// <summary>
	/// Asynchronously loads a backdrop from a directory previously written by <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> (or otherwise
	/// containing exactly one <c>.ibl</c> and one <c>.ktx</c> file).
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="directoryPath">The path of the directory holding the preprocessed backdrop files.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureFromPreprocessedDirectoryAsync(directoryPath, new BackdropTextureCreationConfig { Name = name });
	}
	/// <summary>
	/// Asynchronously loads a backdrop from a directory previously written by <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> (or otherwise
	/// containing exactly one <c>.ibl</c> and one <c>.ktx</c> file), using the given config.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="directoryPath">The path of the directory holding the preprocessed backdrop files.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config);

	/// <summary>
	/// Loads a backdrop directly from a high-dynamic-range image, preprocessing it as it goes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deriving a backdrop from a high-dynamic-range image is <b>very</b> slow — minutes rather than seconds for a large image —
	/// and allocates heavily whilst it runs. It is not something to do while the application is meant to be responsive.
	/// </para>
	/// <para>
	/// It is much quicker to load a pre-processed <c>.ktx</c>/<c>.ibl</c> file pair. You can pre-create these files using <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> and
	/// load them with <see cref="LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
	/// </para>
	/// <para>
	/// It is even quicker still to use a pre-baked <see cref="BackdropTexture"/>; you can bake the processed file using the <see cref="IAssetBakery"/> and then
	/// load it again with <see cref="IAssetLoader.LoadBakedBackdropTexture"/>. The only reason to prefer <c>.ktx</c>/<c>.ibl</c> format over this is if you've
	/// already been provided that format or you wish to keep your baked data portable. 
	/// </para>
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, Quality backdropTextureResolution = Quality.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTexture(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	/// <summary>
	/// Loads a backdrop directly from a high-dynamic-range image, preprocessing it as it goes, using the given config.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deriving a backdrop from a high-dynamic-range image is <b>very</b> slow — minutes rather than seconds for a large image —
	/// and allocates heavily whilst it runs. It is not something to do while the application is meant to be responsive.
	/// </para>
	/// <para>
	/// It is much quicker to load a pre-processed <c>.ktx</c>/<c>.ibl</c> file pair. You can pre-create these files using <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectory"/> and
	/// load them with <see cref="LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
	/// </para>
	/// <para>
	/// It is even quicker still to use a pre-baked <see cref="BackdropTexture"/>; you can bake the processed file using the <see cref="IAssetBakery"/> and then
	/// load it again with <see cref="IAssetLoader.LoadBakedBackdropTexture"/>. The only reason to prefer <c>.ktx</c>/<c>.ibl</c> format over this is if you've
	/// already been provided that format or you wish to keep your baked data portable. 
	/// </para>
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, Quality backdropTextureResolution = Quality.Standard);
	/// <summary>
	/// Asynchronously loads a backdrop directly from a high-dynamic-range image, preprocessing it as it goes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Deriving a backdrop from a high-dynamic-range image is <b>very</b> slow — minutes rather than seconds for a large image —
	/// and allocates heavily whilst it runs. It is not something to do while the application is meant to be responsive.
	/// </para>
	/// <para>
	/// It is much quicker to load a pre-processed <c>.ktx</c>/<c>.ibl</c> file pair. You can pre-create these files using <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync"/> and
	/// load them with <see cref="LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
	/// </para>
	/// <para>
	/// It is even quicker still to use a pre-baked <see cref="BackdropTexture"/>; you can bake the processed file using the <see cref="IAssetBakery"/> and then
	/// load it again with <see cref="IAssetLoader.LoadBakedBackdropTextureAsync"/>. The only reason to prefer <c>.ktx</c>/<c>.ibl</c> format over this is if you've
	/// already been provided that format or you wish to keep your baked data portable. 
	/// </para>
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, Quality backdropTextureResolution = Quality.Standard, ReadOnlySpan<char> name = default) {
		return LoadBackdropTextureAsync(hdrOrExrFilePath, new BackdropTextureCreationConfig { Name = name }, backdropTextureResolution);
	}
	/// <summary>
	/// Asynchronously loads a backdrop directly from a high-dynamic-range image, using the given config.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to the method of the same name without the <c>Async</c> suffix. The returned operation
	/// must be consumed exactly once; see <see cref="Threading.TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Deriving a backdrop from a high-dynamic-range image is <b>very</b> slow — minutes rather than seconds for a large image —
	/// and allocates heavily whilst it runs. It is not something to do while the application is meant to be responsive.
	/// </para>
	/// <para>
	/// It is much quicker to load a pre-processed <c>.ktx</c>/<c>.ibl</c> file pair. You can pre-create these files using <see cref="PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync"/> and
	/// load them with <see cref="LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
	/// </para>
	/// <para>
	/// It is even quicker still to use a pre-baked <see cref="BackdropTexture"/>; you can bake the processed file using the <see cref="IAssetBakery"/> and then
	/// load it again with <see cref="IAssetLoader.LoadBakedBackdropTextureAsync"/>. The only reason to prefer <c>.ktx</c>/<c>.ibl</c> format over this is if you've
	/// already been provided that format or you wish to keep your baked data portable. 
	/// </para>
	/// </remarks>
	/// <param name="hdrOrExrFilePath">The path of the high-dynamic-range image to use. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	/// <param name="backdropTextureResolution">How detailed the resulting backdrop should be. Higher resolutions take proportionally longer to prepare.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, Quality backdropTextureResolution = Quality.Standard);
}