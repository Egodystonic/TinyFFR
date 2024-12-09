// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly unsafe struct MaterialHandle : IResourceHandle<MaterialHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Material).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public MaterialHandle(nuint val) => AsInteger = val;
	public MaterialHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(MaterialHandle handle) => handle.AsInteger;
	public static implicit operator MaterialHandle(nuint val) => new(val);
	public static implicit operator void*(MaterialHandle handle) => handle.AsPointer;
	public static implicit operator MaterialHandle(void* val) => new(val);

	static MaterialHandle IResourceHandle<MaterialHandle>.CreateFromInteger(nuint val) => new(val);
	static MaterialHandle IResourceHandle<MaterialHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(MaterialHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is MaterialHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(MaterialHandle left, MaterialHandle right) => left.Equals(right);
	public static bool operator !=(MaterialHandle left, MaterialHandle right) => !left.Equals(right);

	public override string ToString() => $"Material Handle 0x{AsInteger:X16}";
}