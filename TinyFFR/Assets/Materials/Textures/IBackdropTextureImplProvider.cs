// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Provides the implementation behind <see cref="BackdropTexture"/>.
/// </summary>
public interface IBackdropTextureImplProvider : IDisposableResourceImplProvider<BackdropTexture> {
	/// <summary>
	/// Invoked internally to obtain the native texture that a backdrop's visible sky is drawn from.
	/// </summary>
	UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle);
	/// <summary>
	/// Invoked internally to obtain the native texture that a backdrop's ambient lighting is derived from.
	/// </summary>
	UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle);
}