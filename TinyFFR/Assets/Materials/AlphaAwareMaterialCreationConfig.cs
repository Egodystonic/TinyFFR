// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct AlphaAwareMaterialCreationConfig {
	public static readonly AlphaMaterialType DefaultType = AlphaMaterialType.Standard;

	public required Texture ColorMap { get; init; }
	public required Texture NormalMap { get; init; }
	public required Texture OrmMap { get; init; }
	public AlphaMaterialType Type { get; init; } = DefaultType;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public AlphaAwareMaterialCreationConfig() { }
	public AlphaAwareMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (NormalMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(NormalMap));
		if (OrmMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(OrmMap));
		if (!Enum.IsDefined(Type)) throw new InvalidOperationException($"'{nameof(Type)}' is not a valid {nameof(AlphaMaterialType)} value ({Type}).");
	}
}