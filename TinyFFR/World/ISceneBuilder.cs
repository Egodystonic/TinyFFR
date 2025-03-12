// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ISceneBuilder {
	Scene CreateScene(bool includeBackdrop = true, ColorVect? backdropColor = null, ReadOnlySpan<char> name = default) {
		return CreateScene(new SceneCreationConfig { InitialBackdropColor = includeBackdrop ? (backdropColor ?? SceneCreationConfig.DefaultInitialBackdropColor) : null, Name = name});
	}
	Scene CreateScene(in SceneCreationConfig config);
}