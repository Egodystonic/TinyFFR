// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshAssetImplProvider {
	bool IsDisposed(MeshHandle handle);
	void Dispose(MeshHandle handle);
	string GetName(MeshHandle handle);
	int GetNameUsingSpan(MeshHandle handle, Span<char> dest);
	int GetNameSpanLength(MeshHandle handle);
}