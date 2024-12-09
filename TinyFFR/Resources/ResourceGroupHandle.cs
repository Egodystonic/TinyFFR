// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct ResourceGroupHandle : IResourceHandle<ResourceGroupHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(ResourceGroup).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public ResourceGroupHandle(nuint val) => AsInteger = val;
	public ResourceGroupHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(ResourceGroupHandle handle) => handle.AsInteger;
	public static implicit operator ResourceGroupHandle(nuint val) => new(val);
	public static implicit operator void*(ResourceGroupHandle handle) => handle.AsPointer;
	public static implicit operator ResourceGroupHandle(void* val) => new(val);

	static ResourceGroupHandle IResourceHandle<ResourceGroupHandle>.CreateFromInteger(nuint val) => new(val);
	static ResourceGroupHandle IResourceHandle<ResourceGroupHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(ResourceGroupHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is ResourceGroupHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(ResourceGroupHandle left, ResourceGroupHandle right) => left.Equals(right);
	public static bool operator !=(ResourceGroupHandle left, ResourceGroupHandle right) => !left.Equals(right);

	public override string ToString() => $"ResourceGroup Handle 0x{AsInteger:X16}";
}