// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly unsafe struct ModelInstanceHandle : IResourceHandle<ModelInstanceHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(ModelInstance).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public ModelInstanceHandle(nuint val) => AsInteger = val;
	public ModelInstanceHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(ModelInstanceHandle handle) => handle.AsInteger;
	public static implicit operator ModelInstanceHandle(nuint val) => new(val);
	public static implicit operator void*(ModelInstanceHandle handle) => handle.AsPointer;
	public static implicit operator ModelInstanceHandle(void* val) => new(val);

	static ModelInstanceHandle IResourceHandle<ModelInstanceHandle>.CreateFromInteger(nuint val) => new(val);
	static ModelInstanceHandle IResourceHandle<ModelInstanceHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(ModelInstanceHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is ModelInstanceHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(ModelInstanceHandle left, ModelInstanceHandle right) => left.Equals(right);
	public static bool operator !=(ModelInstanceHandle left, ModelInstanceHandle right) => !left.Equals(right);

	public override string ToString() => $"Model Instance Handle 0x{AsInteger:X16}";
}