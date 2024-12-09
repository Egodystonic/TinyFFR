// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly unsafe struct DisplayHandle : IResourceHandle<DisplayHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Display).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public DisplayHandle(nuint val) => AsInteger = val;
	public DisplayHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(DisplayHandle handle) => handle.AsInteger;
	public static implicit operator DisplayHandle(nuint val) => new(val);
	public static implicit operator void*(DisplayHandle handle) => handle.AsPointer;
	public static implicit operator DisplayHandle(void* val) => new(val);

	static DisplayHandle IResourceHandle<DisplayHandle>.CreateFromInteger(nuint val) => new(val);
	static DisplayHandle IResourceHandle<DisplayHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(DisplayHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is DisplayHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(DisplayHandle left, DisplayHandle right) => left.Equals(right);
	public static bool operator !=(DisplayHandle left, DisplayHandle right) => !left.Equals(right);

	public override string ToString() => $"Display Handle 0x{AsInteger:X16}";
}