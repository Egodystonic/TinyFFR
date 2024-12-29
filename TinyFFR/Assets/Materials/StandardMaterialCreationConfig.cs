// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct StandardMaterialCreationConfig {
	public Texture? Albedo { get; init; } = null;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public StandardMaterialCreationConfig() { }
	public StandardMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}
}