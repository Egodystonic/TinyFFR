// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Scene;

public readonly unsafe struct RendererHandle : IResourceHandle<RendererHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Renderer).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public RendererHandle(nuint integer) => AsInteger = integer;
	public RendererHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(RendererHandle handle) => handle.AsInteger;
	public static implicit operator RendererHandle(nuint integer) => new(integer);
	public static implicit operator void*(RendererHandle handle) => handle.AsPointer;
	public static implicit operator RendererHandle(void* pointer) => new(pointer);

	static RendererHandle IResourceHandle<RendererHandle>.CreateFromInteger(nuint integer) => new(integer);
	static RendererHandle IResourceHandle<RendererHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(RendererHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is RendererHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(RendererHandle left, RendererHandle right) => left.Equals(right);
	public static bool operator !=(RendererHandle left, RendererHandle right) => !left.Equals(right);

	public override string ToString() => $"Renderer Handle 0x{AsInteger:X16}";
}