// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly unsafe struct WindowHandle : IResourceHandle<WindowHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Window).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public WindowHandle(nuint val) => AsInteger = val;
	public WindowHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(WindowHandle handle) => handle.AsInteger;
	public static implicit operator WindowHandle(nuint val) => new(val);
	public static implicit operator void*(WindowHandle handle) => handle.AsPointer;
	public static implicit operator WindowHandle(void* val) => new(val);

	static WindowHandle IResourceHandle<WindowHandle>.CreateFromInteger(nuint val) => new(val);
	static WindowHandle IResourceHandle<WindowHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(WindowHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is WindowHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(WindowHandle left, WindowHandle right) => left.Equals(right);
	public static bool operator !=(WindowHandle left, WindowHandle right) => !left.Equals(right);

	public override string ToString() => $"Window Handle 0x{AsInteger:X16}";
}