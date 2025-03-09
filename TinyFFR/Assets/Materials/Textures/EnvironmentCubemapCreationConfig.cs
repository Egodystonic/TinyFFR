// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct EnvironmentCubemapCreationConfig {
	public required ReadOnlySpan<char> SkyboxKtxFilePath { get; init; }
	public required ReadOnlySpan<char> IblKtxFilePath { get; init; }
	public ReadOnlySpan<char> Name { get; init; }

	public EnvironmentCubemapCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (SkyboxKtxFilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(EnvironmentCubemapCreationConfig)}.{nameof(SkyboxKtxFilePath)} can not be empty.", nameof(SkyboxKtxFilePath));
		}
		if (IblKtxFilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(EnvironmentCubemapCreationConfig)}.{nameof(IblKtxFilePath)} can not be empty.", nameof(IblKtxFilePath));
		}
	}
}