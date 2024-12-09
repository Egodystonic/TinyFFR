// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Factory.Local;

public interface ILocalTinyFfrFactory : ITinyFfrFactory {
	IWindowBuilder WindowBuilder { get; }
	new ILocalApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IApplicationLoopBuilder ITinyFfrFactory.ApplicationLoopBuilder => ApplicationLoopBuilder;
}