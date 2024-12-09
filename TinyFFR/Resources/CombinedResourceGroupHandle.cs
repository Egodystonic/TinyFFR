// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct CombinedResourceGroupHandle : IResourceHandle<CombinedResourceGroupHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(CombinedResourceGroup).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public CombinedResourceGroupHandle(nuint val) => AsInteger = val;
	public CombinedResourceGroupHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(CombinedResourceGroupHandle handle) => handle.AsInteger;
	public static implicit operator CombinedResourceGroupHandle(nuint val) => new(val);
	public static implicit operator void*(CombinedResourceGroupHandle handle) => handle.AsPointer;
	public static implicit operator CombinedResourceGroupHandle(void* val) => new(val);

	static CombinedResourceGroupHandle IResourceHandle<CombinedResourceGroupHandle>.CreateFromInteger(nuint val) => new(val);
	static CombinedResourceGroupHandle IResourceHandle<CombinedResourceGroupHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(CombinedResourceGroupHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is CombinedResourceGroupHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(CombinedResourceGroupHandle left, CombinedResourceGroupHandle right) => left.Equals(right);
	public static bool operator !=(CombinedResourceGroupHandle left, CombinedResourceGroupHandle right) => !left.Equals(right);

	public override string ToString() => $"CombinedResourceGroup Handle 0x{AsInteger:X16}";
}