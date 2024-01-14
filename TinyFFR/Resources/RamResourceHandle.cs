// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly struct RamResourceHandle : IEquatable<RamResourceHandle> {
	internal RamResourceMapRef ResourceMapRef { get; }

	internal RamResourceHandle(RamResourceMapRef resourceMapRef) => ResourceMapRef = resourceMapRef;

	public bool Equals(RamResourceHandle other) => ResourceMapRef.Equals(other.ResourceMapRef);
	public override bool Equals(object? obj) => obj is RamResourceHandle other && Equals(other);
	public override int GetHashCode() => ResourceMapRef.GetHashCode();

	public static bool operator ==(RamResourceHandle left, RamResourceHandle right) => left.Equals(right);
	public static bool operator !=(RamResourceHandle left, RamResourceHandle right) => !left.Equals(right);
}