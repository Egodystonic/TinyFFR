// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ISceneImplProvider : IDisposableResourceImplProvider<SceneHandle> {
	void Add(SceneHandle handle, ModelInstance modelInstance);
	void Remove(SceneHandle handle, ModelInstance modelInstance);

	void Add(SceneHandle handle, Light light);
	void Remove(SceneHandle handle, Light light);
}