// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly unsafe struct MeshHandle : IResourceHandle<MeshHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Mesh).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public MeshHandle(nuint integer) => AsInteger = integer;
	public MeshHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(MeshHandle handle) => handle.AsInteger;
	public static implicit operator MeshHandle(nuint integer) => new(integer);
	public static implicit operator void*(MeshHandle handle) => handle.AsPointer;
	public static implicit operator MeshHandle(void* pointer) => new(pointer);

	static MeshHandle IResourceHandle<MeshHandle>.CreateFromInteger(nuint integer) => new(integer);
	static MeshHandle IResourceHandle<MeshHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(MeshHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is MeshHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(MeshHandle left, MeshHandle right) => left.Equals(right);
	public static bool operator !=(MeshHandle left, MeshHandle right) => !left.Equals(right);

	public override string ToString() => $"Mesh Handle 0x{AsInteger:X16}";
}