// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshAssetImplProvider {
	bool IsDisposed(MeshAssetHandle handle);
	void Dispose(MeshAssetHandle handle);
	string GetName(MeshAssetHandle handle);
	int GetNameUsingSpan(MeshAssetHandle handle, Span<char> dest);
	int GetNameSpanMaxLength(MeshAssetHandle handle);
}