// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialAssetImplProvider {
	bool IsDisposed(MaterialHandle handle);
	void Dispose(MaterialHandle handle);
	string GetName(MaterialHandle handle);
	int GetNameUsingSpan(MaterialHandle handle, Span<char> dest);
	int GetNameSpanMaxLength(MaterialHandle handle);
}