// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Loop;

public interface IApplicationLoopBuilder {
	ApplicationLoop BuildLoop();
	ApplicationLoop BuildLoop(in ApplicationLoopConfig config);
}