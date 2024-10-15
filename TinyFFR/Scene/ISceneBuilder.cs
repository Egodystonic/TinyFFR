// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface ISceneBuilder {
	Scene CreateScene();
	Scene CreateScene(in SceneCreationConfig config);
}