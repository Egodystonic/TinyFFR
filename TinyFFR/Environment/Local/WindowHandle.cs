// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly unsafe struct WindowHandle : IResourceHandle<WindowHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(WindowHandle).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public WindowHandle(nuint integer) => AsInteger = integer;
	public WindowHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(WindowHandle handle) => handle.AsInteger;
	public static implicit operator WindowHandle(nuint integer) => new(integer);
	public static implicit operator void*(WindowHandle handle) => handle.AsPointer;
	public static implicit operator WindowHandle(void* pointer) => new(pointer);

	static WindowHandle IResourceHandle<WindowHandle>.CreateFromInteger(nuint integer) => new(integer);
	static WindowHandle IResourceHandle<WindowHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(WindowHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is WindowHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(WindowHandle left, WindowHandle right) => left.Equals(right);
	public static bool operator !=(WindowHandle left, WindowHandle right) => !left.Equals(right);

	public override string ToString() => $"Window Handle 0x{AsInteger:X16}";
}