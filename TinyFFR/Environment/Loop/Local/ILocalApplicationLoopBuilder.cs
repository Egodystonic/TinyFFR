// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public interface ILocalApplicationLoopBuilder : IApplicationLoopBuilder {
	ApplicationLoop BuildLoop(in LocalApplicationLoopConfig config);
}