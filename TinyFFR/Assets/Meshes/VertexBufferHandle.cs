// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly unsafe struct VertexBufferHandle : IResourceHandle<VertexBufferHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal ResourceIdent Ident => new(typeof(VertexBufferHandle).TypeHandle.Value, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public VertexBufferHandle(nuint integer) => AsInteger = integer;
	public VertexBufferHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(VertexBufferHandle handle) => handle.AsInteger;
	public static implicit operator VertexBufferHandle(nuint integer) => new(integer);
	public static implicit operator void*(VertexBufferHandle handle) => handle.AsPointer;
	public static implicit operator VertexBufferHandle(void* pointer) => new(pointer);

	static VertexBufferHandle IResourceHandle<VertexBufferHandle>.CreateFromInteger(nuint integer) => new(integer);
	static VertexBufferHandle IResourceHandle<VertexBufferHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(VertexBufferHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is VertexBufferHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(VertexBufferHandle left, VertexBufferHandle right) => left.Equals(right);
	public static bool operator !=(VertexBufferHandle left, VertexBufferHandle right) => !left.Equals(right);

	public override string ToString() => $"Vertex Buffer Handle 0x{AsInteger:X16}";
}