// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public readonly unsafe struct ApplicationLoopHandle : IResourceHandle<ApplicationLoopHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(ApplicationLoop).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public ApplicationLoopHandle(nuint val) => AsInteger = val;
	public ApplicationLoopHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(ApplicationLoopHandle handle) => handle.AsInteger;
	public static implicit operator ApplicationLoopHandle(nuint val) => new(val);
	public static implicit operator void*(ApplicationLoopHandle handle) => handle.AsPointer;
	public static implicit operator ApplicationLoopHandle(void* val) => new(val);

	static ApplicationLoopHandle IResourceHandle<ApplicationLoopHandle>.CreateFromInteger(nuint val) => new(val);
	static ApplicationLoopHandle IResourceHandle<ApplicationLoopHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(ApplicationLoopHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is ApplicationLoopHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(ApplicationLoopHandle left, ApplicationLoopHandle right) => left.Equals(right);
	public static bool operator !=(ApplicationLoopHandle left, ApplicationLoopHandle right) => !left.Equals(right);

	public override string ToString() => $"Application Loop Handle 0x{AsInteger:X16}";
}