// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public interface IWindowBuilder {
	Window Build(Display display, WindowFullscreenStyle fullscreenStyle);
	Window Build(in WindowCreationConfig config);
}