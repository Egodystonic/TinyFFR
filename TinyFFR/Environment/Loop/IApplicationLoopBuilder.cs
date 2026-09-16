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
	/// <summary>
	/// Creates a new <see cref="ApplicationLoop"/>, optionally capped to a maximum rate of iteration.
	/// </summary>
	/// <param name="frameRateCapHz">The maximum number of iterations per second the new loop should run at. Must be positive, or <see langword="null"/> (the default) for no cap.</param>
	/// <param name="name">Optional name for this application loop.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="frameRateCapHz"/> is zero or negative.</exception>
	ApplicationLoop CreateLoop(int? frameRateCapHz = null, ReadOnlySpan<char> name = default) => CreateLoop(new ApplicationLoopCreationConfig { FrameRateCapHz = frameRateCapHz, Name = name });
	/// <summary>
	/// Creates a new <see cref="ApplicationLoop"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new application loop, including its name and frame rate cap.</param>
	ApplicationLoop CreateLoop(in ApplicationLoopCreationConfig config);
}