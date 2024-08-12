// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.Assets.Asset;

namespace Egodystonic.TinyFFR.Assets;

public readonly unsafe struct MeshAsset : IEquatable<MeshAsset>, IDisposable {
	internal readonly AssetHandle Handle;

	internal UIntPtr HandleAsPtr {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (UIntPtr) Handle;
	}

	internal MeshAsset(AssetHandle handle) => Handle = handle;

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);
}