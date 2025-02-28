// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

public interface IAssetLoader {
	IMeshBuilder MeshBuilder { get; }
	IMaterialBuilder MaterialBuilder { get; }

	Texture LoadTexture(ReadOnlySpan<char> filePath, bool includeWAlphaChannel = false, ReadOnlySpan<char> name = default) {
		return LoadTexture(new TextureLoadConfig {
			FilePath = filePath,
			Name = name.IsEmpty ? Path.GetFileName(filePath) : name,
			IncludeWAlphaChannel = includeWAlphaChannel
		});
	}
	Texture LoadTexture(in TextureLoadConfig config);

	Mesh LoadMesh(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMesh(new MeshLoadConfig {
			FilePath = filePath,
			Name = name.IsEmpty ? Path.GetFileName(filePath) : name
		});
	}
	Mesh LoadMesh(in MeshLoadConfig config);
}