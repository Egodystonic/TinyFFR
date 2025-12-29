// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct BackdropTextureReadConfig {
	public required ReadOnlySpan<char> SkyboxKtxFilePath { get; init; }
	public required ReadOnlySpan<char> IblKtxFilePath { get; init; }

	public BackdropTextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (SkyboxKtxFilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(BackdropTextureCreationConfig)}.{nameof(SkyboxKtxFilePath)} can not be empty.", nameof(SkyboxKtxFilePath));
		}
		if (IblKtxFilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(BackdropTextureCreationConfig)}.{nameof(IblKtxFilePath)} can not be empty.", nameof(IblKtxFilePath));
		}
	}
}

public readonly ref struct BackdropTextureCreationConfig {
	public ReadOnlySpan<char> Name { get; init; }

	public BackdropTextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}