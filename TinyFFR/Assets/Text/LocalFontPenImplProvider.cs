// Created on 2026-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Text;

sealed class LocalFontPenImplProvider : IFontPenImplProvider, ILocalResourceImplProvider {
	readonly LocalFontLoader _owner;
	public LocalFontPenImplProvider(LocalFontLoader owner) => _owner = owner;

	public string GetNameAsNewStringObject(ResourceHandle<FontPen> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<FontPen> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<FontPen> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<FontPen> handle) => _owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<FontPen> handle) => _owner.Dispose(handle);
	public Font GetFont(ResourceHandle<FontPen> handle) => _owner.GetFont(handle);
	public Material GetMaterial(ResourceHandle<FontPen> handle) => _owner.GetMaterial(handle);
}
