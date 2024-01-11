// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.RenderLoop;

public interface IRenderLoopPlugin {
	void Tick(TimeSpan deltaTime);
}