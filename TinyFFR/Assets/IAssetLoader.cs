// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

public readonly record struct TextureReadMetadata(XYPair<int> Dimensions);
public readonly record struct MeshReadMetadata(int VertexCount, int TriangleCount);

public enum TextureCombinationSourceTexture {
	TextureA,
	TextureB,
	TextureC,
	TextureD
}
public readonly record struct TextureCombinationSource(TextureCombinationSourceTexture SourceTexture, ColorChannel SourceChannel) {
	internal byte SelectTexelChannel(ReadOnlySpan<TexelRgba32> samples) => samples[(int) SourceTexture][SourceChannel];

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		if (!Enum.IsDefined(SourceChannel)) throw new ArgumentOutOfRangeException(nameof(SourceChannel), SourceChannel, null);
		if ((int) SourceTexture < 0 || (int) SourceTexture >= numTexturesBeingCombined) {
			throw new ArgumentOutOfRangeException(nameof(SourceTexture), SourceTexture, $"Invalid texture value or references a texture that was not provided (i.e. 'TextureC' when only textures A & B exist).");
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

	Texture LoadTexture(ReadOnlySpan<char> filePath, bool includeWAlphaChannel = false, bool isLinearColorspace = true, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			new TextureReadConfig {
				FilePath = filePath,
				IncludeWAlphaChannel = includeWAlphaChannel
			},
			new TextureCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name,
				IsLinearColorspace = isLinearColorspace
			}
		);
	}
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTexture(new TextureReadConfig { FilePath = filePath }, config);
	Texture LoadTexture(in TextureReadConfig readConfig, in TextureCreationConfig config);
	
	Texture LoadCombinedTexture(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig
	);
	Texture LoadCombinedTexture(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		in TextureReadConfig cReadConfig, in TextureCreationConfig cConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig
	);
	Texture LoadCombinedTexture(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		in TextureReadConfig cReadConfig, in TextureCreationConfig cConfig,
		in TextureReadConfig dReadConfig, in TextureCreationConfig dConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig
	);

	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath) => ReadTextureMetadata(new TextureReadConfig { FilePath = filePath });
	TextureReadMetadata ReadTextureMetadata(in TextureReadConfig readConfig);
	void ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(new TextureReadConfig { FilePath = filePath }, destinationBuffer);
	void ReadTexture<TTexel>(in TextureReadConfig readConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;
	
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath) {
		return ReadCombinedTextureMetadata(new TextureReadConfig { FilePath = aFilePath }, new TextureReadConfig { FilePath = bFilePath });
	}
	TextureReadMetadata ReadCombinedTextureMetadata(in TextureReadConfig aReadConfig, in TextureReadConfig bReadConfig);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath) {
		return ReadCombinedTextureMetadata(new TextureReadConfig { FilePath = aFilePath }, new TextureReadConfig { FilePath = bFilePath }, new TextureReadConfig { FilePath = cFilePath });
	}
	TextureReadMetadata ReadCombinedTextureMetadata(in TextureReadConfig aReadConfig, in TextureReadConfig bReadConfig, in TextureReadConfig cReadConfig);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath) {
		return ReadCombinedTextureMetadata(new TextureReadConfig { FilePath = aFilePath }, new TextureReadConfig { FilePath = bFilePath }, new TextureReadConfig { FilePath = cFilePath }, new TextureReadConfig { FilePath = dFilePath });
	}
	TextureReadMetadata ReadCombinedTextureMetadata(in TextureReadConfig aReadConfig, in TextureReadConfig bReadConfig, in TextureReadConfig cReadConfig, in TextureReadConfig dReadConfig);
	void ReadCombinedTexture<TTexel>(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig,
		Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, ITexel<TTexel>;
	void ReadCombinedTexture<TTexel>(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		in TextureReadConfig cReadConfig, in TextureCreationConfig cConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig,
		Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, ITexel<TTexel>;
	void ReadCombinedTexture<TTexel>(
		in TextureReadConfig aReadConfig, in TextureCreationConfig aConfig,
		in TextureReadConfig bReadConfig, in TextureCreationConfig bConfig,
		in TextureReadConfig cReadConfig, in TextureCreationConfig cConfig,
		in TextureReadConfig dReadConfig, in TextureCreationConfig dConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig combinedTextureConfig,
		Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, ITexel<TTexel>;

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
	void ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(new MeshReadConfig { FilePath = filePath }, vertexBuffer, triangleBuffer);
	void ReadMesh(in MeshReadConfig readConfig, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer);

	EnvironmentCubemap LoadEnvironmentCubemap(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadEnvironmentCubemap(
			new EnvironmentCubemapReadConfig { IblKtxFilePath = iblKtxFilePath, SkyboxKtxFilePath = skyboxKtxFilePath }, 
			new EnvironmentCubemapCreationConfig { Name = name }
		);
	}
	EnvironmentCubemap LoadEnvironmentCubemap(in EnvironmentCubemapReadConfig readConfig, in EnvironmentCubemapCreationConfig config);
}