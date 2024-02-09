// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Desktop;

namespace Egodystonic.TinyFFR.Factory;

public interface ITffrFactory : ITrackedDisposable {
	IDisplayDiscoverer GetDisplayDiscoverer();
	IWindowBuilder GetWindowBuilder();
	IWindowBuilder GetWindowBuilder(WindowBuilderConfig config);
	IApplicationLoopBuilder GetApplicationLoopBuilder();
	IApplicationLoopBuilder GetApplicationLoopBuilder(ApplicationLoopBuilderConfig config);
}