// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

public readonly record struct TextureReadMetadata(XYPair<int> Dimensions);
public readonly record struct MeshReadMetadata(int VertexCount, int TriangleCount);
public readonly record struct MeshReadCountData(int NumVerticesWritten, int NumTrianglesWritten);

public enum TextureCombinationSourceTexture {
	TextureA,
	TextureB,
	TextureC,
	TextureD
}
public readonly record struct TextureCombinationSource(TextureCombinationSourceTexture SourceTexture, ColorChannel SourceChannel) {
	internal byte SelectTexelChannel(ReadOnlySpan<TexelRgba32> samples) => samples[(int) SourceTexture][SourceChannel];

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		if (!Enum.IsDefined(SourceChannel)) {
			throw new InvalidOperationException($"{nameof(SourceChannel)} was not a recognised {nameof(ColorChannel)}.");
		}
		if ((int) SourceTexture < 0 || (int) SourceTexture >= numTexturesBeingCombined) {
			throw new InvalidOperationException($"Non-defined value or references a texture that was not provided (i.e. 'TextureC' when only textures A & B exist).");
		}
	}
}
public readonly record struct TextureCombinationConfig(TextureCombinationSource OutputTextureXRedChannelSource, TextureCombinationSource OutputTextureYGreenChannelSource, TextureCombinationSource OutputTextureZBlueChannelSource, TextureCombinationSource? OutputTextureWAlphaChannelSource = null) {
	internal TexelRgba32 SelectTexel(ReadOnlySpan<TexelRgba32> samples) {
		return new TexelRgba32(
			OutputTextureXRedChannelSource.SelectTexelChannel(samples),
			OutputTextureYGreenChannelSource.SelectTexelChannel(samples),
			OutputTextureZBlueChannelSource.SelectTexelChannel(samples),
			OutputTextureWAlphaChannelSource?.SelectTexelChannel(samples) ?? Byte.MaxValue
		);
	}

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		OutputTextureXRedChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureYGreenChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureZBlueChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureWAlphaChannelSource?.ThrowIfInvalid(numTexturesBeingCombined);
	}
}

public interface IAssetLoader {
	IMeshBuilder MeshBuilder { get; }
	IMaterialBuilder MaterialBuilder { get; }
	ITextureBuilder TextureBuilder { get; }
	IBuiltInTexturePathLibrary BuiltInTexturePaths { get; }

	#region Load / Read Texture
	Texture LoadColorMapTexture(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) => LoadTexture(filePath, isLinearColorspace: false, name);
	Texture LoadDataMapTexture(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) => LoadTexture(filePath, isLinearColorspace: true, name);
	Texture LoadTexture(ReadOnlySpan<char> filePath, bool isLinearColorspace, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config);

	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(filePath, TextureProcessingConfig.None, destinationBuffer);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, in TextureProcessingConfig processingConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Load / Read Combined Texture
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
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
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;
	#endregion

	#region Load Environment Cubemap
	EnvironmentCubemap LoadEnvironmentCubemap(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadEnvironmentCubemap(
			new EnvironmentCubemapReadConfig { IblKtxFilePath = iblKtxFilePath, SkyboxKtxFilePath = skyboxKtxFilePath },
			new EnvironmentCubemapCreationConfig { Name = name }
		);
	}
	EnvironmentCubemap LoadEnvironmentCubemap(in EnvironmentCubemapReadConfig readConfig, in EnvironmentCubemapCreationConfig config);
	#endregion

	#region Load / Read Mesh
	Mesh LoadMesh(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMesh(
			new MeshReadConfig {
				FilePath = filePath
			},
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMesh(new MeshReadConfig { FilePath = filePath }, config);
	Mesh LoadMesh(in MeshReadConfig readConfig, in MeshCreationConfig config);
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath) => ReadMeshMetadata(new MeshReadConfig { FilePath = filePath });
	MeshReadMetadata ReadMeshMetadata(in MeshReadConfig readConfig);
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(new MeshReadConfig { FilePath = filePath }, vertexBuffer, triangleBuffer);
	MeshReadCountData ReadMesh(in MeshReadConfig readConfig, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer);
	#endregion
}