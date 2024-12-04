// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopBuilder {
	ApplicationLoop CreateLoop(int? frameRateCapHz = null, ReadOnlySpan<char> name = default) => CreateLoop(new ApplicationLoopConfig { FrameRateCapHz = frameRateCapHz, Name = name });
	ApplicationLoop CreateLoop(in ApplicationLoopConfig config);
}