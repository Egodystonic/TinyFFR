// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ISceneBuilder {
	Scene CreateScene(ReadOnlySpan<char> name = default) => CreateScene(new SceneCreationConfig { Name = name });
	Scene CreateScene(in SceneCreationConfig config);
}