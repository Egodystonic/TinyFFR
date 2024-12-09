// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly unsafe struct SceneHandle : IResourceHandle<SceneHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(Scene).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public SceneHandle(nuint val) => AsInteger = val;
	public SceneHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(SceneHandle handle) => handle.AsInteger;
	public static implicit operator SceneHandle(nuint val) => new(val);
	public static implicit operator void*(SceneHandle handle) => handle.AsPointer;
	public static implicit operator SceneHandle(void* val) => new(val);

	static SceneHandle IResourceHandle<SceneHandle>.CreateFromInteger(nuint val) => new(val);
	static SceneHandle IResourceHandle<SceneHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(SceneHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is SceneHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);
	public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);

	public override string ToString() => $"Scene Handle 0x{AsInteger:X16}";
}