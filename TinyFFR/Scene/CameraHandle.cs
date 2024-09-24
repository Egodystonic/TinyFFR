// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Scene;

public readonly unsafe struct CameraHandle : IResourceHandle<CameraHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;

	public CameraHandle(nuint integer) => AsInteger = integer;
	public CameraHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(CameraHandle handle) => handle.AsInteger;
	public static implicit operator CameraHandle(nuint integer) => new(integer);
	public static implicit operator void*(CameraHandle handle) => handle.AsPointer;
	public static implicit operator CameraHandle(void* pointer) => new(pointer);

	static CameraHandle IResourceHandle<CameraHandle>.CreateFromInteger(nuint integer) => new(integer);
	static CameraHandle IResourceHandle<CameraHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(CameraHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is CameraHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(CameraHandle left, CameraHandle right) => left.Equals(right);
	public static bool operator !=(CameraHandle left, CameraHandle right) => !left.Equals(right);

	public override string ToString() => $"Camera Handle 0x{AsInteger:X16}";
}