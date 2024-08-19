// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MeshAsset : IEquatable<MeshAsset>, IDisposable {
	internal readonly 

	

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);
}