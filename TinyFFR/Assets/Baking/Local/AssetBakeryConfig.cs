// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Baking;

/// <summary>
/// Configures the asset bakery, which writes loaded resources out in the form TinyFFR can reload fastest.
/// </summary>
public sealed record AssetBakeryConfig {
	/// <summary>
	/// The default value for <see cref="MaxResourcesInBakeryMemory"/>: <c>500</c>.
	/// </summary>
	public const int DefaultMaxResourcesInBakeryMemory = 500;

	/// <summary>
	/// Whether the asset bakery is available at all. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Baking is an authoring-time/packaging-time or first-run step rather than something an application normally does while running, so it is off unless asked
	/// for. Enabling it costs some memory and slightly slows resource loading even when nothing is being baked.
	/// </remarks>
	public bool Enabled { get; init; } = false;

	/// <summary>
	/// How many resources the bakery keeps in memory at once. Defaults to <see cref="DefaultMaxResourcesInBakeryMemory"/>: <c>500</c>.
	/// </summary>
	/// <remarks>
	/// The bakery remembers resources it has already written so that one referenced by several assets is not written repeatedly.
	/// If you're loading significantly complex assets with hundreds of sub-assets you may wish to increase this number; at the
	/// cost of memory usage.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to set a non-positive value.</exception>
	public int MaxResourcesInBakeryMemory {
		get;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(MaxResourcesInBakeryMemory), value, $"Must be at least 1.");
			}
			field = value;
		}
	} = DefaultMaxResourcesInBakeryMemory;
	
	/// <summary>
	/// Whether a baked asset file must match the current schema exactly in order to be loaded. Defaults to <see langword="false"/>.
	/// This property has an effect on the <i>load</i> side of baked assets, and thus has an effect even if <see cref="Enabled"/> is <c>false</c>.
	/// </summary>
	/// <remarks>
	/// By default a file written by a slightly older version of TinyFFR is still accepted where it can be understood. Set this
	/// to reject anything that is not an exact match..
	/// </remarks>
	public bool RequireStrictAssetBakeSchemaMatch { get; init; } = false;
}