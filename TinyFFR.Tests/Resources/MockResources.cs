// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IMockResourceImplProvider : IResourceImplProvider;

readonly unsafe struct MockHandleAlpha : IResourceHandle<MockHandleAlpha> {
	public UIntPtr AsInteger { get; init; }
	public void* AsPointer { get; init; }
	public ResourceIdent Ident { get; init; }
	public static IntPtr TypeHandle => typeof(MockResourceAlpha).TypeHandle.Value;

	public bool Equals(MockHandleAlpha other) => AsInteger.Equals(other.AsInteger) && AsPointer == other.AsPointer;
	public static MockHandleAlpha CreateFromInteger(UIntPtr integer) => new() { AsInteger = integer, AsPointer = (void*) integer, Ident = new(TypeHandle, integer) };
	public static MockHandleAlpha CreateFromPointer(void* pointer) => new() { AsInteger = (UIntPtr) pointer, AsPointer = pointer, Ident = new(TypeHandle, (UIntPtr) pointer) };
} 
readonly struct MockResourceAlpha : IResource<MockResourceAlpha, MockHandleAlpha, IMockResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceAlpha other) { throw new NotImplementedException(); }
	public static MockResourceAlpha RecreateFromRawHandleAndImpl(UIntPtr rawHandle, IResourceImplProvider impl) { throw new NotImplementedException(); }
	public MockHandleAlpha Handle { get; }
	public IMockResourceImplProvider Implementation { get; }
}