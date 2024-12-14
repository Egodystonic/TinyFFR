
// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly unsafe struct LightHandle : IResourceHandle<LightHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Light).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public LightHandle(nuint val) => AsInteger = val;
	public LightHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(LightHandle handle) => handle.AsInteger;
	public static implicit operator LightHandle(nuint val) => new(val);
	public static implicit operator void*(LightHandle handle) => handle.AsPointer;
	public static implicit operator LightHandle(void* val) => new(val);

	static LightHandle IResourceHandle<LightHandle>.CreateFromInteger(nuint val) => new(val);
	static LightHandle IResourceHandle<LightHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(LightHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is LightHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(LightHandle left, LightHandle right) => left.Equals(right);
	public static bool operator !=(LightHandle left, LightHandle right) => !left.Equals(right);

	public override string ToString() => $"Light Handle 0x{AsInteger:X16}";
}