// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopBuilder {
	ApplicationLoop BuildLoop();
	ApplicationLoop BuildLoop(in ApplicationLoopConfig config);
}