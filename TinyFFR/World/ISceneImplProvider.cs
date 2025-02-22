// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ISceneImplProvider : IDisposableResourceImplProvider<Scene> {
	void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance);

	void Add(ResourceHandle<Scene> handle, Light light);
	void Remove(ResourceHandle<Scene> handle, Light light);
}