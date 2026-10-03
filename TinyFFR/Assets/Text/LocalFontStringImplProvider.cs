// Created on 2026-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Text;

sealed class LocalFontStringImplProvider : IFontStringImplProvider, ILocalResourceImplProvider {
	readonly LocalFontLoader _owner;
	public LocalFontStringImplProvider(LocalFontLoader owner) => _owner = owner;

	public string GetNameAsNewStringObject(ResourceHandle<FontString> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<FontString> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<FontString> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<FontString> handle) => _owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<FontString> handle) => _owner.Dispose(handle);
	public Font GetFont(ResourceHandle<FontString> handle) => _owner.GetFont(handle);
	public Mesh GetMesh(ResourceHandle<FontString> handle) => _owner.GetMesh(handle);
	public XYPair<float> GetSize(ResourceHandle<FontString> handle) => _owner.GetSize(handle);
}
