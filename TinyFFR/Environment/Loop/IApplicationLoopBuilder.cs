// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

/// <summary>
/// Builder interface that helps create <see cref="ApplicationLoop"/>s.
/// </summary>
public interface IApplicationLoopBuilder {
	ApplicationLoop CreateLoop(int? frameRateCapHz = null, ReadOnlySpan<char> name = default) => CreateLoop(new ApplicationLoopCreationConfig { FrameRateCapHz = frameRateCapHz, Name = name });
	ApplicationLoop CreateLoop(in ApplicationLoopCreationConfig config);
}