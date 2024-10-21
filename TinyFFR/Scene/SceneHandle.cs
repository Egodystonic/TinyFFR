// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Scene;

public readonly unsafe struct SceneHandle : IResourceHandle<SceneHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(SceneHandle).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public SceneHandle(nuint integer) => AsInteger = integer;
	public SceneHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(SceneHandle handle) => handle.AsInteger;
	public static implicit operator SceneHandle(nuint integer) => new(integer);
	public static implicit operator void*(SceneHandle handle) => handle.AsPointer;
	public static implicit operator SceneHandle(void* pointer) => new(pointer);

	static SceneHandle IResourceHandle<SceneHandle>.CreateFromInteger(nuint integer) => new(integer);
	static SceneHandle IResourceHandle<SceneHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(SceneHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is SceneHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);
	public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);

	public override string ToString() => $"Scene Handle 0x{AsInteger:X16}";
}