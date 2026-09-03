// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Baking;

public sealed record AssetBakeryConfig {
	public const int DefaultMaxResourcesInBakeryMemory = 500;

	public bool Enabled { get; init; } = false;

	public int MaxResourcesInBakeryMemory {
		get;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(MaxResourcesInBakeryMemory), value, $"Must be at least 1.");
			}
			field = value;
		}
	} = DefaultMaxResourcesInBakeryMemory;
	
	public bool RequireStrictAssetBakeSchemaMatch { get; init; } = false;
}