// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly unsafe struct VertexBufferHandle : IResourceHandle<VertexBufferHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(VertexBuffer).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public VertexBufferHandle(nuint val) => AsInteger = val;
	public VertexBufferHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(VertexBufferHandle handle) => handle.AsInteger;
	public static implicit operator VertexBufferHandle(nuint val) => new(val);
	public static implicit operator void*(VertexBufferHandle handle) => handle.AsPointer;
	public static implicit operator VertexBufferHandle(void* val) => new(val);

	static VertexBufferHandle IResourceHandle<VertexBufferHandle>.CreateFromInteger(nuint val) => new(val);
	static VertexBufferHandle IResourceHandle<VertexBufferHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(VertexBufferHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is VertexBufferHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(VertexBufferHandle left, VertexBufferHandle right) => left.Equals(right);
	public static bool operator !=(VertexBufferHandle left, VertexBufferHandle right) => !left.Equals(right);

	public override string ToString() => $"Vertex Buffer Handle 0x{AsInteger:X16}";
}