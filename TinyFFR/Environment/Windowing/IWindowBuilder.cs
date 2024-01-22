// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

public interface IWindowBuilder {
	IReadOnlyCollection<Window> ActiveWindows { get; }

	Window Build() => Build(new());
	Window Build(in WindowCreationConfig config);
}