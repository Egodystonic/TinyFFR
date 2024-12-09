// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly unsafe struct CameraHandle : IResourceHandle<CameraHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Camera).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public CameraHandle(nuint val) => AsInteger = val;
	public CameraHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(CameraHandle handle) => handle.AsInteger;
	public static implicit operator CameraHandle(nuint val) => new(val);
	public static implicit operator void*(CameraHandle handle) => handle.AsPointer;
	public static implicit operator CameraHandle(void* val) => new(val);

	static CameraHandle IResourceHandle<CameraHandle>.CreateFromInteger(nuint val) => new(val);
	static CameraHandle IResourceHandle<CameraHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(CameraHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is CameraHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(CameraHandle left, CameraHandle right) => left.Equals(right);
	public static bool operator !=(CameraHandle left, CameraHandle right) => !left.Equals(right);

	public override string ToString() => $"Camera Handle 0x{AsInteger:X16}";
}