// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly unsafe struct IndexBufferAsset : IEquatable<IndexBufferAsset>, IDisposable {
	internal readonly AssetHandle Handle;

	internal UIntPtr HandleAsPtr {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (UIntPtr) Handle;
	}

	internal IndexBufferAsset(AssetHandle handle) => Handle = handle;

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);
}