// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct ResourceHandle : IEquatable<ResourceHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;

	public ResourceHandle(void* val) : this((nuint) val) { }
	public ResourceHandle(nuint val) => AsInteger = val;

	public static implicit operator nuint(ResourceHandle handle) => handle.AsInteger;
	public static implicit operator ResourceHandle(nuint val) => new(val);
	public static implicit operator void*(ResourceHandle handle) => handle.AsPointer;
	public static implicit operator ResourceHandle(void* val) => new(val);

	public bool Equals(ResourceHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is ResourceHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(ResourceHandle left, ResourceHandle right) => left.Equals(right);
	public static bool operator !=(ResourceHandle left, ResourceHandle right) => !left.Equals(right);
	public override string ToString() => $"Untyped Handle 0x{AsInteger:X16}";
}

public readonly unsafe struct ResourceHandle<TResource> : IEquatable<ResourceHandle<TResource>> where TResource : IResource<TResource> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	public static nint TypeHandle => typeof(TResource).TypeHandle.Value;

	public ResourceHandle(void* val) : this((nuint) val) { }
	public ResourceHandle(nuint val) => AsInteger = val;

	public static implicit operator nuint(ResourceHandle<TResource> handle) => handle.AsInteger;
	public static implicit operator ResourceHandle<TResource>(nuint val) => new(val);
	public static implicit operator void*(ResourceHandle<TResource> handle) => handle.AsPointer;
	public static implicit operator ResourceHandle<TResource>(void* val) => new(val);
	public static implicit operator ResourceHandle(ResourceHandle<TResource> typedHandle) => new(typedHandle.AsInteger);
	public static explicit operator ResourceHandle<TResource>(ResourceHandle untypedHandle) => new(untypedHandle.AsInteger);

	public bool Equals(ResourceHandle<TResource> other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is ResourceHandle<TResource> other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(ResourceHandle<TResource> left, ResourceHandle<TResource> right) => left.Equals(right);
	public static bool operator !=(ResourceHandle<TResource> left, ResourceHandle<TResource> right) => !left.Equals(right);

	public override string ToString() => $"{typeof(TResource).Name} Handle 0x{AsInteger:X16}";
}