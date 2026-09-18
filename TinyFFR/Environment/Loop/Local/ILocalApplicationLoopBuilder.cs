// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

/// <inheritdoc/>
/// <remarks>
/// Specialization of <see cref="IApplicationLoopBuilder"/> for local factories.
/// </remarks>
public interface ILocalApplicationLoopBuilder : IApplicationLoopBuilder {
	ApplicationLoop IApplicationLoopBuilder.CreateLoop(in ApplicationLoopCreationConfig config) => CreateLoop(new LocalApplicationLoopCreationConfig(config));

	/// <summary>
	/// Creates a new <see cref="ApplicationLoop"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new application loop, including its name, frame rate cap, and local-only settings.</param>
	ApplicationLoop CreateLoop(in LocalApplicationLoopCreationConfig config);
}