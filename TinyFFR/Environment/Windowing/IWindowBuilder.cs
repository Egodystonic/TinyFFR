// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

public interface IWindowBuilder {
	IReadOnlyCollection<WindowHandle> ActiveWindows { get; }

	WindowHandle Build() => Build(new());
	WindowHandle Build(in WindowCreationConfig config);
}