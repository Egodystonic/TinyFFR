// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface ITextureAssetImplProvider {
	bool IsDisposed(TextureHandle handle);
	void Dispose(TextureHandle handle);
	string GetName(TextureHandle handle);
	int GetNameUsingSpan(TextureHandle handle, Span<char> dest);
	int GetNameSpanMaxLength(TextureHandle handle);
}