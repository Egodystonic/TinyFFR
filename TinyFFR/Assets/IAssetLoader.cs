// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// Describes the metadata (size, etc) of a texture file.
/// </summary>
/// <param name="Dimensions">The texture's width and height in texels.</param>
/// <param name="IncludesAlphaChannel">Whether the file carries a fourth (alpha) channel in addition to red, green and blue.</param>
public readonly record struct TextureReadMetadata(XYPair<int> Dimensions, bool IncludesAlphaChannel);
/// <summary>
/// Describes the metadata (vertex count, etc) of a mesh file. 
/// </summary>
/// <param name="TotalVertexCount">The total number of vertices across every sub-mesh in the file.</param>
/// <param name="TotalTriangleCount">The total number of triangles across every sub-mesh in the file.</param>
/// <param name="SubMeshCount">The number of separate sub-meshes the file contains.</param>
public readonly record struct MeshReadMetadata(int TotalVertexCount, int TotalTriangleCount, int SubMeshCount);
/// <summary>
/// Reports how much of a mesh file was actually written in to the buffers supplied to a read operation.
/// </summary>
/// <param name="NumVerticesWritten">The number of vertices written to the vertex buffer.</param>
/// <param name="NumTrianglesWritten">The number of triangles written to the triangle buffer.</param>
public readonly record struct MeshReadCountData(int NumVerticesWritten, int NumTrianglesWritten);

/// <summary>
/// Describes what span of angles a single channel of angle-formatted anisotropy data is understood to cover.
/// </summary>
/// <remarks>
/// Anisotropy data sometimes encodes a direction as one channel's intensity rather than as a vector. This says what the lowest
/// and highest values in that channel mean, which is what allows them to be converted in to the direction TinyFFR uses
/// internally.
/// </remarks>
public enum AnisotropyRadialAngleRange {
	/// <summary>
	/// The channel's full range of values covers a complete turn, i.e. <c>0° &lt;= n &lt; 360°</c>.
	/// </summary>
	ZeroTo360,
	/// <summary>
	/// The channel's full range of values covers a half turn, i.e. <c>0° &lt;= n &lt; 180°</c>.
	/// </summary>
	/// <remarks>
	/// This is the usual choice where the data describes the orientation of a line rather than a direction, since a line at
	/// <c>10°</c> and a line at <c>190°</c> are the same line.
	/// </remarks>
	ZeroTo180
}

/// <summary>
/// Loads textures, meshes, fonts, models and backdrops from files on disc, and provides the builders used to create such
/// resources from scratch.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>Load...</c> method returns a resource that must be disposed when it is no longer needed, and resources must be
/// disposed in dependency order (a material before the textures it uses, for example).
/// </para>
/// <para>
/// Each method has an <c>...Async</c> counterpart that returns a <see cref="TinyFfrAsyncOperation{T}"/> instead of blocking.
/// The <c>Read...</c> methods are different in kind: they copy a file's raw contents in to a buffer you supply and create no
/// resource at all, which is what you want in order to inspect or alter that data before it reaches the GPU.
/// </para>
/// </remarks>
public partial interface IAssetLoader {
	/// <summary>
	/// The builder used to create meshes from shape descriptions, polygons or raw vertex data.
	/// </summary>
	IMeshBuilder MeshBuilder { get; }
	/// <summary>
	/// The builder used to combine textures in to materials.
	/// </summary>
	IMaterialBuilder MaterialBuilder { get; }
	/// <summary>
	/// The builder used to create textures from patterns or from texel data held in memory.
	/// </summary>
	ITextureBuilder TextureBuilder { get; }
	/// <summary>
	/// File paths for the textures built in to TinyFFR, usable anywhere a real file path is expected.
	/// </summary>
	IBuiltInTexturePathLibrary BuiltInTexturePaths { get; }

	#region Load / Read Texture
	/// <summary>
	/// Loads a texture from an image file.
	/// </summary>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="dataType">What the texture's texels represent, which determines how they are interpreted and compressed.</param>
	/// <param name="name">The name to give the texture. May be left empty, in which case the file's own name is used.</param>
	Texture LoadTexture(ReadOnlySpan<char> filePath, TextureDataType dataType, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			filePath,
			new TextureCreationConfig {
				DataType = dataType,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Loads a texture from an image file, using the given creation config.
	/// </summary>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTexture(filePath, in config, new TextureReadConfig());
	/// <summary>
	/// Loads a texture from an image file, using the given creation and read configs.
	/// </summary>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	/// <param name="readConfig">Controls how the file's data is read, such as whether its alpha channel is kept.</param>
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig);

	/// <summary>
	/// Asynchronously loads a texture from an image file.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="dataType">What the texture's texels represent, which determines how they are interpreted and compressed.</param>
	/// <param name="name">The name to give the texture. May be left empty, in which case the file's own name is used.</param>
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, TextureDataType dataType, ReadOnlySpan<char> name = default) {
		return LoadTextureAsync(
			filePath,
			new TextureCreationConfig {
				DataType = dataType,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Asynchronously loads a texture from an image file, using the given creation config.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTextureAsync(filePath, in config, new TextureReadConfig());
	/// <summary>
	/// Asynchronously loads a texture from an image file, using the given creation and read configs.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="filePath">The path of the image file to load. Must name an existing file in a supported format.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	/// <param name="readConfig">Controls how the file's data is read, such as whether its alpha channel is kept.</param>
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig);

	/// <summary>
	/// Reads an image file's dimensions and channel layout without loading it, so that a buffer for its texels can be sized.
	/// </summary>
	/// <param name="filePath">The path of the image file to inspect. Must name an existing file in a supported format.</param>
	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath);
	/// <summary>
	/// Reads an image file's texels in to the given buffer without creating any texture resource.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this where the data must be inspected or altered in your own code before it reaches the GPU; there is no need for it
	/// if the file can simply be loaded as-is.
	/// </para>
	/// <para>
	/// Data is written row-by-row, from bottom to top.
	/// </para>
	/// </remarks>
	/// <typeparam name="TTexel">The texel type to write. The file's data is converted to this type as it is read.</typeparam>
	/// <param name="filePath">The path of the image file to read. Must name an existing file in a supported format.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row. Must be large enough to contain the read data,
	/// so size it from <see cref="ReadTextureMetadata"/> first.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is not large enough to accomodate the texel data.</exception>
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(filePath, TextureProcessingConfig.None, destinationBuffer);
	/// <summary>
	/// Reads an image file's texels in to the given buffer without creating any texture resource, applying the given processing
	/// as it does so.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this where the data must be inspected or altered in your own code before it reaches the GPU; there is no need for it
	/// if the file can simply be loaded as-is.
	/// </para>
	/// <para>
	/// Data is written row-by-row, from bottom to top.
	/// </para>
	/// </remarks>
	/// <typeparam name="TTexel">The texel type to write. The file's data is converted to this type as it is read.</typeparam>
	/// <param name="filePath">The path of the image file to read. Must name an existing file in a supported format.</param>
	/// <param name="processingConfig">Alterations to make to the data as it is read, such as flipping it or rearranging its
	/// channels.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row. Must be large enough to contain the read data,
	/// so size it from <see cref="ReadTextureMetadata"/> first.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is not large enough to accomodate the texel data.</exception>
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, in TextureProcessingConfig processingConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;

	/// <summary>
	/// Loads a texture from a previously baked asset file (i.e. via the <see cref="IAssetBakery"/>).
	/// </summary>
	/// <remarks>
	/// A baked asset has already been decoded, processed and compressed ahead of time, so loading one is considerably faster
	/// than loading the original image file.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture LoadBakedTexture(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a texture from a previously baked asset file (i.e. via the <see cref="IAssetBakery"/>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <see cref="LoadBakedTexture"/>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// A baked asset has already been decoded, processed and compressed ahead of time, so loading one is considerably faster
	/// than loading the original image file.
	/// </para>
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	TinyFfrAsyncOperation<Texture> LoadBakedTextureAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);

	/// <summary>
	/// Loads a material, and the textures it uses, from a previously baked asset file (i.e. via the <see cref="IAssetBakery"/>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Disposing the returned group disposes the material and every texture in it.
	/// </para>
	/// <para>
	/// A baked asset has already been decoded, processed and compressed ahead of time, so loading one is considerably faster
	/// than loading the original image file.
	/// </para>
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	ResourceGroup LoadBakedMaterial(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a material, and the textures it uses, from a previously baked asset file (i.e. via the <see cref="IAssetBakery"/>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <see cref="LoadBakedMaterial"/>. The returned operation must be consumed exactly once;
	/// see <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes the material and every texture in it.
	/// </para>
	/// <para>
	/// A baked asset has already been decoded, processed and compressed ahead of time, so loading one is considerably faster
	/// than loading the original image file.
	/// </para>
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadBakedMaterialAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	#endregion

	#region Load / Read Combined Texture
	/// <summary>
	/// Loads two image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Loads two image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	/// <summary>
	/// Loads three image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Loads three image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	/// <summary>
	/// Loads four image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Loads four image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="dProcessingConfig">Alterations to make to the fourth source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);

	/// <summary>
	/// Asynchronously loads two image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Asynchronously loads two image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	/// <summary>
	/// Asynchronously loads three image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Asynchronously loads three image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	/// <summary>
	/// Asynchronously loads four image files and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	/// <summary>
	/// Asynchronously loads four image files, processes each one, and combines their channels in to a single texture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadCombinedTexture</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// This is how separate single-channel maps are packed in to one multi-channel map: an occlusion file and a roughness file
	/// become the first two channels of one ORM texture, for example. Sources of differing sizes are reconciled according to the
	/// combination config's scaling strategy.
	/// </para>
	/// <para>
	/// This particular overload also allows for processing the data before final combination.
	/// </para>
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="dProcessingConfig">Alterations to make to the fourth source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputConfig">Controls how the resulting combined texture is created on the GPU.</param>
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);

	/// <summary>
	/// Reports the dimensions and channel layout the texture combining these two files would have, without loading them.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath);
	/// <summary>
	/// Reports the dimensions and channel layout the texture combining these three files would have, without loading them.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath);
	/// <summary>
	/// Reports the dimensions and channel layout the texture combining these four files would have, without loading them.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources.
	/// </remarks>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath);

	/// <summary>
	/// Combines the channels of two image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	/// <summary>
	/// Processes and combines the channels of two image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	/// <summary>
	/// Combines the channels of three image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	/// <summary>
	/// Processes and combines the channels of three image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output. Must not refer to a source
	/// texture beyond those supplied here.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	/// <summary>
	/// Combines the channels of four image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	/// <summary>
	/// Processes and combines the channels of four image files in to the given buffer without creating any texture resource.
	/// </summary>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="aFilePath">The path of the first source image file.</param>
	/// <param name="aProcessingConfig">Alterations to make to the first source before combining it.</param>
	/// <param name="bFilePath">The path of the second source image file.</param>
	/// <param name="bProcessingConfig">Alterations to make to the second source before combining it.</param>
	/// <param name="cFilePath">The path of the third source image file.</param>
	/// <param name="cProcessingConfig">Alterations to make to the third source before combining it.</param>
	/// <param name="dFilePath">The path of the fourth source image file.</param>
	/// <param name="dProcessingConfig">Alterations to make to the fourth source before combining it.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output.</param>
	/// <param name="finalOutputProcessingConfig">Alterations to make to the combined result.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to. Size it from
	/// <c>ReadCombinedTextureMetadata</c> first; anything it is too small to hold is discarded.</param>
	/// <returns>The number of texels actually written, which is never more than the length of
	/// <paramref name="destinationBuffer"/>.</returns>
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;
	#endregion

	#region Load Backdrop Texture
	/// <summary>
	/// Loads a scene backdrop from a pair of files that have already been converted to <c>.ktx</c> and <c>.ibl</c> format.
	/// </summary>
	/// <remarks>
	/// A backdrop supplies both the sky you see behind the scene and the ambient light that sky casts on to everything in it,
	/// which is why two files are involved: one for the visible sky and one for the lighting derived from it.
	/// </remarks>
	/// <param name="skyboxKtxFilePath">The path of the file holding the visible sky.</param>
	/// <param name="iblKtxFilePath">The path of the file holding the lighting derived from that sky.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadPreprocessedBackdropTexture(
			skyboxKtxFilePath, iblKtxFilePath,
			new BackdropTextureCreationConfig { Name = name }
		);
	}
	/// <summary>
	/// Loads a scene backdrop from a pair of files that have already been converted to <c>.ktx</c> and <c>.ibl</c> format, using the given
	/// config.
	/// </summary>
	/// <param name="skyboxKtxFilePath">The path of the file holding the visible sky.</param>
	/// <param name="iblKtxFilePath">The path of the file holding the lighting derived from that sky.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config);

	/// <summary>
	/// Asynchronously loads a scene backdrop from a pair of files that have already been converted to <c>.ktx</c> and <c>.ibl</c> format.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadPreprocessedBackdropTexture</c>. The returned operation must be consumed exactly
	/// once; see <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="skyboxKtxFilePath">The path of the file holding the visible sky.</param>
	/// <param name="iblKtxFilePath">The path of the file holding the lighting derived from that sky.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadPreprocessedBackdropTextureAsync(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadPreprocessedBackdropTextureAsync(
			skyboxKtxFilePath, iblKtxFilePath,
			new BackdropTextureCreationConfig { Name = name }
		);
	}
	/// <summary>
	/// Asynchronously loads a scene backdrop from a pair of files that have already been converted to <c>.ktx</c> and <c>.ibl</c> format, using
	/// the given config.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadPreprocessedBackdropTexture</c>. The returned operation must be consumed exactly
	/// once; see <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="skyboxKtxFilePath">The path of the file holding the visible sky.</param>
	/// <param name="iblKtxFilePath">The path of the file holding the lighting derived from that sky.</param>
	/// <param name="config">Controls how the backdrop texture is created.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadPreprocessedBackdropTextureAsync(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config);

	/// <summary>
	/// Loads a scene backdrop from a previously baked asset file.
	/// </summary>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	BackdropTexture LoadBakedBackdropTexture(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a scene backdrop from a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <see cref="LoadBakedBackdropTexture"/>. The returned operation must be consumed exactly
	/// once; see <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the backdrop texture. May be left empty.</param>
	TinyFfrAsyncOperation<BackdropTexture> LoadBakedBackdropTextureAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	#endregion

	#region Load Font
	/// <summary>
	/// Loads one of the typefaces built in to TinyFFR.
	/// </summary>
	/// <param name="font">Which built-in typeface to load.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	Font LoadFont(BuiltInFont font = BuiltInFont.Default, ReadOnlySpan<char> name = default) {
		return LoadFont(font, new FontCreationConfig { Name = name });
	}
	/// <summary>
	/// Loads a typeface from a font file. File must be in <c>.ttf</c> format.
	/// </summary>
	/// <param name="fontFilePath">The path of the font file to load. Must name an existing file in <c>TTF</c> format.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	Font LoadFont(ReadOnlySpan<char> fontFilePath, ReadOnlySpan<char> name = default) {
		return LoadFont(fontFilePath, new FontCreationConfig { Name = name });
	}
	/// <summary>
	/// Loads one of the typefaces built in to TinyFFR, using the given config.
	/// </summary>
	/// <param name="font">Which built-in typeface to load.</param>
	/// <param name="config">Controls how the font is prepared, such as which characters it covers.</param>
	Font LoadFont(BuiltInFont font, in FontCreationConfig config);
	/// <summary>
	/// Loads a typeface from a font file, using the given config. File must be in <c>.ttf</c> format.
	/// </summary>
	/// <param name="fontFilePath">The path of the font file to load. Must name an existing file in <c>TTF</c> format.</param>
	/// <param name="config">Controls how the font is prepared, such as which characters it covers.</param>
	Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config);

	/// <summary>
	/// Asynchronously loads one of the typefaces built in to TinyFFR.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadFont</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="font">Which built-in typeface to load.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font = BuiltInFont.Default, ReadOnlySpan<char> name = default) {
		return LoadFontAsync(font, new FontCreationConfig { Name = name });
	}
	/// <summary>
	/// Asynchronously loads a typeface from a font file. File must be in <c>.ttf</c> format.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadFont</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="fontFilePath">The path of the font file to load. Must name an existing file in <c>TTF</c> format.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, ReadOnlySpan<char> name = default) {
		return LoadFontAsync(fontFilePath, new FontCreationConfig { Name = name });
	}
	/// <summary>
	/// Asynchronously loads one of the typefaces built in to TinyFFR, using the given config.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadFont</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="font">Which built-in typeface to load.</param>
	/// <param name="config">Controls how the font is prepared, such as which characters it covers.</param>
	TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font, in FontCreationConfig config);
	/// <summary>
	/// Asynchronously loads a typeface from a font file, using the given config. File must be in <c>.ttf</c> format.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <c>LoadFont</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="fontFilePath">The path of the font file to load. Must name an existing file in <c>TTF</c> format.</param>
	/// <param name="config">Controls how the font is prepared, such as which characters it covers.</param>
	TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config);

	/// <summary>
	/// Loads a font from a previously baked asset file.
	/// </summary>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	Font LoadBakedFont(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a font from a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <see cref="LoadBakedFont"/>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the font. May be left empty.</param>
	TinyFfrAsyncOperation<Font> LoadBakedFontAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	#endregion

	#region Load / Read Mesh
	/// <summary>
	/// Loads a mesh from a 3D model file.
	/// </summary>
	/// <remarks>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="name">The name to give the mesh. May be left empty, in which case the file's own name is used.</param>
	Mesh LoadMesh(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMesh(
			filePath,
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Loads a mesh from a 3D model file, using the given creation config.
	/// </summary>
	/// <remarks>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the mesh is created, such as whether its bounding box is overridden.</param>
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMesh(filePath, config, new MeshReadConfig());
	/// <summary>
	/// Loads a mesh from a 3D model file, using the given creation and read configs.
	/// </summary>
	/// <remarks>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the mesh is created, such as whether its bounding box is overridden.</param>
	/// <param name="readConfig">Controls how the file's geometry is interpreted as it is read.</param>
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig);

	/// <summary>
	/// Asynchronously loads a mesh from a 3D model file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadMesh</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="name">The name to give the mesh. May be left empty, in which case the file's own name is used.</param>
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMeshAsync(
			filePath,
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Asynchronously loads a mesh from a 3D model file, using the given creation config.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadMesh</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the mesh is created, such as whether its bounding box is overridden.</param>
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMeshAsync(filePath, config, new MeshReadConfig());
	/// <summary>
	/// Asynchronously loads a mesh from a 3D model file, using the given creation and read configs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadMesh</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Only the geometry is loaded; any materials the file describes are ignored. Use <c>LoadAll</c> to load a composite file's meshes and
	/// materials together.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the mesh is created, such as whether its bounding box is overridden.</param>
	/// <param name="readConfig">Controls how the file's geometry is interpreted as it is read.</param>
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig);

	/// <summary>
	/// Reports how many vertices, triangles and sub-meshes a model file holds without loading it, so that buffers for its
	/// geometry can be sized.
	/// </summary>
	/// <param name="filePath">The path of the model file to inspect. Must name an existing file in a supported format.</param>
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath) => ReadMeshMetadata(filePath, new MeshReadConfig());
	/// <summary>
	/// Reports how many vertices, triangles and sub-meshes a model file holds without loading it, using the given read config.
	/// </summary>
	/// <remarks>
	/// The read config is taken in to account, so the counts reported here match what a read using the same config would
	/// produce.
	/// </remarks>
	/// <param name="filePath">The path of the model file to inspect. Must name an existing file in a supported format.</param>
	/// <param name="readConfig">Controls how the file's geometry is interpreted.</param>
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath, in MeshReadConfig readConfig);
	/// <summary>
	/// Reads a model file's geometry in to the given buffers without creating any mesh resource.
	/// </summary>
	/// <remarks>
	/// Use this where the geometry must be inspected or altered in your own code before it reaches the GPU.
	/// </remarks>
	/// <param name="filePath">The path of the model file to read. Must name an existing file in a supported format.</param>
	/// <param name="vertexBuffer">The buffer to write vertices in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="triangleBuffer">The buffer to write triangles in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <returns>A <see cref="MeshReadCountData"/> telling you how much data was actually read in to each buffer.</returns>
	/// <exception cref="ArgumentException">Thrown if either buffer is too small.</exception>
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	/// <summary>
	/// Reads a model file's geometry in to the given buffers without creating any mesh resource, using the given read config.
	/// </summary>
	/// <param name="filePath">The path of the model file to read. Must name an existing file in a supported format.</param>
	/// <param name="vertexBuffer">The buffer to write vertices in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="triangleBuffer">The buffer to write triangles in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="readConfig">Controls how the file's geometry is interpreted as it is read.</param>
	/// <returns>A <see cref="MeshReadCountData"/> telling you how much data was actually read in to each buffer.</returns>
	/// <exception cref="ArgumentException">Thrown if either buffer is too small.</exception>
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
	/// <summary>
	/// Reads a model file's geometry, including its bone weightings, in to the given buffers without creating any mesh resource.
	/// </summary>
	/// <remarks>
	/// This overload writes <see cref="MeshVertexSkeletal"/> rather than <see cref="MeshVertex"/>, and so preserves the data
	/// needed to animate the mesh with a skeleton.
	/// </remarks>
	/// <param name="filePath">The path of the model file to read. Must name an existing file in a supported format.</param>
	/// <param name="vertexBuffer">The buffer to write vertices in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="triangleBuffer">The buffer to write triangles in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <returns>A <see cref="MeshReadCountData"/> telling you how much data was actually read in to each buffer.</returns>
	/// <exception cref="ArgumentException">Thrown if either buffer is too small.</exception>
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	/// <summary>
	/// Reads a model file's geometry, including its bone weightings, in to the given buffers without creating any mesh resource,
	/// using the given read config.
	/// </summary>
	/// <param name="filePath">The path of the model file to read. Must name an existing file in a supported format.</param>
	/// <param name="vertexBuffer">The buffer to write vertices in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="triangleBuffer">The buffer to write triangles in to. Must be large enough to contain the read data (use
	/// <see cref="ReadMeshMetadata(ReadOnlySpan{char})"/> first if necessary).</param>
	/// <param name="readConfig">Controls how the file's geometry is interpreted as it is read.</param>
	/// <returns>A <see cref="MeshReadCountData"/> telling you how much data was actually read in to each buffer.</returns>
	/// <exception cref="ArgumentException">Thrown if either buffer is too small.</exception>
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);

	/// <summary>
	/// Loads a mesh from a previously baked asset file.
	/// </summary>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	Mesh LoadBakedMesh(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a mesh from a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// The asynchronous counterpart to <see cref="LoadBakedMesh"/>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	TinyFfrAsyncOperation<Mesh> LoadBakedMeshAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	#endregion

	#region Load Generic / Combined
	/// <summary>
	/// Pairs a mesh with a material to form a <see cref="Model"/>, which can then be used to create <see cref="ModelInstance"/>s.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The resulting model does not own the mesh or material; disposing it does not dispose either of them.
	/// </para>
	/// <para>
	/// Note that you don't need to create a <see cref="Model"/> to create a <see cref="ModelInstance"/> if you don't want to;
	/// <see cref="IObjectBuilder"/> has methods that can take your given <paramref name="mesh"/> and <paramref name="material"/>
	/// directly.
	/// </para>
	/// </remarks>
	/// <param name="mesh">The geometry the model uses.</param>
	/// <param name="material">The surface appearance the model uses.</param>
	/// <param name="name">The name to give the model. May be left empty.</param>
	Model CreateModel(Mesh mesh, Material material, ReadOnlySpan<char> name = default);

	/// <summary>
	/// Loads a model, and the mesh, material and textures it uses, from a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// Disposing the returned group disposes every resource within it.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	ResourceGroup LoadBakedModel(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads a model, and the mesh, material and textures it uses, from a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <see cref="LoadBakedModel"/>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource within it.
	/// </para>
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadBakedModelAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);

	/// <summary>
	/// Loads every resource held in a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// Disposing the returned group disposes every resource in it.
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	ResourceGroup LoadBakedResourceGroup(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);
	/// <summary>
	/// Asynchronously loads every resource held in a previously baked asset file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <see cref="LoadBakedModel"/>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource within it.
	/// </para>
	/// </remarks>
	/// <param name="bakedAssetFilePath">The path of the baked asset file to load. Must name an existing file previously
	/// produced by an <see cref="Baking.IAssetBakery"/>.</param>
	/// <param name="name">The name to give the resource group. May be left empty.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadBakedResourceGroupAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default);

	/// <summary>
	/// Loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar).
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the counterpart to loading a mesh and its textures separately, and is the only option for formats that pack
	/// everything in to one binary file. The returned group's models can be handed straight to the object builder to create an
	/// instance of each.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource in it.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="name">The name to give the resource group. May be left empty, in which case the file's own name is
	/// used.</param>
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadAll(
			filePath,
			new ModelCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar), using the given creation config.
	/// </summary>
	/// <remarks>
	/// Disposing the returned group disposes every resource in it.
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the resources are created.</param>
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config) => LoadAll(filePath, in config, new ModelReadConfig());
	/// <summary>
	/// Loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar), using the given creation and read configs.
	/// </summary>
	/// <remarks>
	/// Disposing the returned group disposes every resource in it.
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the resources are created.</param>
	/// <param name="readConfig">Controls how the file's contents are interpreted as they are read.</param>
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig);

	/// <summary>
	/// Asynchronously loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadAll</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Unlike the other asynchronous loads, this one advances roughly one sub-mesh per frame, so that loading a large model
	/// never stalls a frame. That makes it essentially free for files with a few dozen sub-meshes, but considerably slower in
	/// total than the synchronous version for files with many thousands of them.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource in it.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="name">The name to give the resource group. May be left empty, in which case the file's own name is
	/// used.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadAllAsync(
			filePath,
			new ModelCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	/// <summary>
	/// Asynchronously loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar), using the given creation
	/// config.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadAll</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Unlike the other asynchronous loads, this one advances roughly one sub-mesh per frame, so that loading a large model
	/// never stalls a frame. That makes it essentially free for files with a few dozen sub-meshes, but considerably slower in
	/// total than the synchronous version for files with many thousands of them.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource in it.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the resources are created.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, in ModelCreationConfig config) => LoadAllAsync(filePath, in config, new ModelReadConfig());
	/// <summary>
	/// Asynchronously loads every mesh, texture, material and model contained in a composite file (such as <c>glTF</c>/<c>glb</c> and similar), using the given creation and
	/// read configs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The asynchronous counterpart to <c>LoadAll</c>. The returned operation must be consumed exactly once; see
	/// <see cref="TinyFfrAsyncOperation{T}"/>.
	/// </para>
	/// <para>
	/// Unlike the other asynchronous loads, this one advances roughly one sub-mesh per frame, so that loading a large model
	/// never stalls a frame. That makes it essentially free for files with a few dozen sub-meshes, but considerably slower in
	/// total than the synchronous version for files with many thousands of them.
	/// </para>
	/// <para>
	/// Disposing the returned group disposes every resource in it.
	/// </para>
	/// </remarks>
	/// <param name="filePath">The path of the model file to load. Must name an existing file in a supported format, with any
	/// companion data files alongside it.</param>
	/// <param name="config">Controls how the resources are created.</param>
	/// <param name="readConfig">Controls how the file's contents are interpreted as they are read.</param>
	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig);
	#endregion
}
