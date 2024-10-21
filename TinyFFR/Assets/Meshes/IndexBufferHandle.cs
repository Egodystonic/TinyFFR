// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly unsafe struct IndexBufferHandle : IResourceHandle<IndexBufferHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(IndexBufferHandle).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public IndexBufferHandle(nuint integer) => AsInteger = integer;
	public IndexBufferHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(IndexBufferHandle handle) => handle.AsInteger;
	public static implicit operator IndexBufferHandle(nuint integer) => new(integer);
	public static implicit operator void*(IndexBufferHandle handle) => handle.AsPointer;
	public static implicit operator IndexBufferHandle(void* pointer) => new(pointer);

	static IndexBufferHandle IResourceHandle<IndexBufferHandle>.CreateFromInteger(nuint integer) => new(integer);
	static IndexBufferHandle IResourceHandle<IndexBufferHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(IndexBufferHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is IndexBufferHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(IndexBufferHandle left, IndexBufferHandle right) => left.Equals(right);
	public static bool operator !=(IndexBufferHandle left, IndexBufferHandle right) => !left.Equals(right);

	public override string ToString() => $"Index Buffer Handle 0x{AsInteger:X16}";
}