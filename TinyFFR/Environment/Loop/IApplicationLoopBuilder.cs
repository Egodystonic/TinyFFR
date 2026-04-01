// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public interface IApplicationLoopBuilder {
	ApplicationLoop CreateLoop(int? frameRateCapHz = null, ReadOnlySpan<char> name = default) => CreateLoop(new ApplicationLoopCreationConfig { FrameRateCapHz = frameRateCapHz, Name = name });
	ApplicationLoop CreateLoop(in ApplicationLoopCreationConfig config);
	
	ApplicationLoop? FindLoopByName(ReadOnlySpan<char> name, bool allowPartialMatch = IResourceFinder.DefaultAllowPartialMatch, StringComparison comparisonType = IResourceFinder.DefaultComparisonType);
	IndirectEnumerable<object, ApplicationLoop> AllCreatedLoops { get; }
}