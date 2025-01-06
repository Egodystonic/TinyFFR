// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct OpaqueMaterialCreationConfig {
	public required Texture ColorMap { get; init; }
	public required Texture NormalMap { get; init; }
	public required Texture OrmMap { get; init; }

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public OpaqueMaterialCreationConfig() { }
	public OpaqueMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (NormalMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(NormalMap));
		if (OrmMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(OrmMap));
	}
}