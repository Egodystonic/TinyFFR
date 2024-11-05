// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Scene;

public interface ISceneImplProvider : IDisposableResourceImplProvider<SceneHandle> {
	void Add(SceneHandle handle, ModelInstance modelInstance);
	void Remove(SceneHandle handle, ModelInstance modelInstance);

	void Render<TRenderTarget>(SceneHandle handle, Camera camera, TRenderTarget renderTarget) where TRenderTarget : IRenderTarget;
}