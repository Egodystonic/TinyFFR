// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public interface ILocalApplicationLoopBuilder : IApplicationLoopBuilder {
	ApplicationLoop CreateLoop(int? frameRateCapHz = null, bool? waitForVsync = null, ReadOnlySpan<char> name = default);
	ApplicationLoop CreateLoop(in LocalApplicationLoopConfig config);
}