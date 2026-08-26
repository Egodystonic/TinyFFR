// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets;

public readonly record struct TextureReadMetadata(XYPair<int> Dimensions, bool IncludesAlphaChannel);
public readonly record struct MeshReadMetadata(int TotalVertexCount, int TotalTriangleCount, int SubMeshCount);
public readonly record struct MeshReadCountData(int NumVerticesWritten, int NumTrianglesWritten);

public enum AnisotropyRadialAngleRange {
	ZeroTo360,
	ZeroTo180
}

public partial interface IAssetLoader {
	IMeshBuilder MeshBuilder { get; }
	IMaterialBuilder MaterialBuilder { get; }
	ITextureBuilder TextureBuilder { get; }
	IBuiltInTexturePathLibrary BuiltInTexturePaths { get; }

	#region Load / Read Texture
	Texture LoadTexture(ReadOnlySpan<char> filePath, bool isLinearColorspace, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTexture(filePath, in config, new TextureReadConfig());
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig);
	
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, bool isLinearColorspace, ReadOnlySpan<char> name = default) {
		return LoadTextureAsync(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTextureAsync(filePath, in config, new TextureReadConfig());
	TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig);

	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(filePath, TextureProcessingConfig.None, destinationBuffer);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, in TextureProcessingConfig processingConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Load / Read Combined Texture
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTextureAsync(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);

	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath);

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;
	#endregion

	#region Load Backdrop Texture
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadPreprocessedBackdropTexture(
			skyboxKtxFilePath, iblKtxFilePath,
			new BackdropTextureCreationConfig { Name = name }
		);
	}
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config);
	
	TinyFfrAsyncOperation<BackdropTexture> LoadPreprocessedBackdropTextureAsync(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadPreprocessedBackdropTextureAsync(
			skyboxKtxFilePath, iblKtxFilePath,
			new BackdropTextureCreationConfig { Name = name }
		);
	}
	TinyFfrAsyncOperation<BackdropTexture> LoadPreprocessedBackdropTextureAsync(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config);
	#endregion
	
	#region Load Font
	Font LoadFont(BuiltInFont font = BuiltInFont.Default, ReadOnlySpan<char> name = default) {
		return LoadFont(font, new FontCreationConfig { Name = name });
	}
	Font LoadFont(ReadOnlySpan<char> fontFilePath, ReadOnlySpan<char> name = default) {
		return LoadFont(fontFilePath, new FontCreationConfig { Name = name });
	}
	Font LoadFont(BuiltInFont font, in FontCreationConfig config);
	Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config);
	
	TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font = BuiltInFont.Default, ReadOnlySpan<char> name = default) {
		return LoadFontAsync(font, new FontCreationConfig { Name = name });
	}
	TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, ReadOnlySpan<char> name = default) {
		return LoadFontAsync(fontFilePath, new FontCreationConfig { Name = name });
	}
	TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font, in FontCreationConfig config);
	TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config);
	#endregion

	#region Load / Read Mesh
	Mesh LoadMesh(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMesh(
			filePath,
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMesh(filePath, config, new MeshReadConfig());
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig);
	
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMeshAsync(
			filePath,
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMeshAsync(filePath, config, new MeshReadConfig());
	TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig);
	
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath) => ReadMeshMetadata(filePath, new MeshReadConfig());
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath, in MeshReadConfig readConfig);
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
	#endregion

	#region Load Generic / Combined
	Model CreateModel(Mesh mesh, Material material, ReadOnlySpan<char> name = default);
	
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadAll(
			filePath,
			new ModelCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config) => LoadAll(filePath, in config, new ModelReadConfig());
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig);

	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadAllAsync(
			filePath,
			new ModelCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, in ModelCreationConfig config) => LoadAllAsync(filePath, in config, new ModelReadConfig());
	TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig);
	#endregion
}