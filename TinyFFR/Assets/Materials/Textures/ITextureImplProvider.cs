// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface ITextureImplProvider : IDisposableResourceImplProvider<TextureHandle> {
	XYPair<uint> GetDimensions(TextureHandle handle);
}