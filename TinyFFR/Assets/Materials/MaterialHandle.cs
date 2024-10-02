// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly unsafe struct MaterialHandle : IResourceHandle<MaterialHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal ResourceIdent Ident => new(typeof(Material).TypeHandle.Value, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public MaterialHandle(nuint integer) => AsInteger = integer;
	public MaterialHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(MaterialHandle handle) => handle.AsInteger;
	public static implicit operator MaterialHandle(nuint integer) => new(integer);
	public static implicit operator void*(MaterialHandle handle) => handle.AsPointer;
	public static implicit operator MaterialHandle(void* pointer) => new(pointer);

	static MaterialHandle IResourceHandle<MaterialHandle>.CreateFromInteger(nuint integer) => new(integer);
	static MaterialHandle IResourceHandle<MaterialHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(MaterialHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is MaterialHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(MaterialHandle left, MaterialHandle right) => left.Equals(right);
	public static bool operator !=(MaterialHandle left, MaterialHandle right) => !left.Equals(right);

	public override string ToString() => $"Material Handle 0x{AsInteger:X16}";
}