// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

public readonly record struct TextureReadMetadata(int Width, int Height);
public readonly record struct MeshReadMetadata(int VertexCount, int TriangleCount);

public interface IAssetLoader {
	IMeshBuilder MeshBuilder { get; }
	IMaterialBuilder MaterialBuilder { get; }

	Texture LoadTexture(ReadOnlySpan<char> filePath, bool includeWAlphaChannel = false, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			new TextureReadConfig {
				FilePath = filePath,
				IncludeWAlphaChannel = includeWAlphaChannel
			},
			new TextureCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Texture LoadTexture(in TextureReadConfig readConfig, in TextureCreationConfig config);
	TextureReadMetadata ReadTextureMetadata(in TextureReadConfig readConfig);
	void ReadTexture<TTexel>(Span<TTexel> destinationBuffer, in TextureReadConfig readConfig) where TTexel : unmanaged, ITexel<TTexel>;
	Texture LoadAndCombineOrmTextures(ReadOnlySpan<char> occlusionMapFilePath = default, ReadOnlySpan<char> roughnessMapFilePath = default, ReadOnlySpan<char> metallicMapFilePath = default, in TextureCreationConfig config = default) {
		return LoadAndCombineOrmTextures(
			occlusionMapFilePath.IsEmpty ? default : new TextureReadConfig { FilePath = occlusionMapFilePath },
			roughnessMapFilePath.IsEmpty ? default : new TextureReadConfig { FilePath = roughnessMapFilePath },
			metallicMapFilePath.IsEmpty ? default : new TextureReadConfig { FilePath = metallicMapFilePath },
			config
		);
	}
	Texture LoadAndCombineOrmTextures(in TextureReadConfig occlusionMapReadConfig = default, in TextureReadConfig roughnessMapReadConfig = default, in TextureReadConfig metallicMapReadConfig = default, in TextureCreationConfig config = default);

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
	Mesh LoadMesh(in MeshReadConfig readConfig, in MeshCreationConfig config);
	MeshReadMetadata ReadMeshMetadata(in MeshReadConfig readConfig);
	void ReadMesh(Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
}