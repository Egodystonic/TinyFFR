// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Windowing;

namespace Egodystonic.TinyFFR.Factory;

public interface ITffrFactory : ITrackedDisposable {
	IDisplayLoader GetDisplayLoader();
	IWindowBuilder GetWindowBuilder();
	IWindowBuilder GetWindowBuilder(WindowBuilderCreationConfig config);
}