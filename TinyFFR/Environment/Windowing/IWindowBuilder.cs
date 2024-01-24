// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Desktop;

namespace Egodystonic.TinyFFR.Environment.Windowing;

public interface IWindowBuilder {
	Window Build(Monitor monitor, WindowFullscreenStyle fullscreenStyle);
	Window Build(in WindowCreationConfig config);
}