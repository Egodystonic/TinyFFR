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
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTexture(new TextureReadConfig { FilePath = filePath }, config);
	Texture LoadTexture(in TextureReadConfig readConfig, in TextureCreationConfig config);
	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath) => ReadTextureMetadata(new TextureReadConfig { FilePath = filePath });
	TextureReadMetadata ReadTextureMetadata(in TextureReadConfig readConfig);
	void ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(new TextureReadConfig { FilePath = filePath }, destinationBuffer);
	void ReadTexture<TTexel>(in TextureReadConfig readConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;
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
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMesh(new MeshReadConfig { FilePath = filePath }, config);
	Mesh LoadMesh(in MeshReadConfig readConfig, in MeshCreationConfig config);
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath) => ReadMeshMetadata(new MeshReadConfig { FilePath = filePath });
	MeshReadMetadata ReadMeshMetadata(in MeshReadConfig readConfig);
	void ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(new MeshReadConfig { FilePath = filePath }, vertexBuffer, triangleBuffer);
	void ReadMesh(in MeshReadConfig readConfig, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer);
}