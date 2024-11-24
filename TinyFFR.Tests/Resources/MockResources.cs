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
	public bool Equals(MockResourceAlpha other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceAlpha RecreateFromRawHandleAndImpl(UIntPtr rawHandle, IResourceImplProvider impl) {
		return new() { Handle = MockHandleAlpha.CreateFromInteger(rawHandle), Implementation = (IMockResourceImplProvider) impl };
	}
	public MockHandleAlpha Handle { get; init; }
	public IMockResourceImplProvider Implementation { get; init; }
}
readonly unsafe struct MockHandleBravo : IResourceHandle<MockHandleBravo> {
	public UIntPtr AsInteger { get; init; }
	public void* AsPointer { get; init; }
	public ResourceIdent Ident { get; init; }
	public static IntPtr TypeHandle => typeof(MockResourceBravo).TypeHandle.Value;

	public bool Equals(MockHandleBravo other) => AsInteger.Equals(other.AsInteger) && AsPointer == other.AsPointer;
	public static MockHandleBravo CreateFromInteger(UIntPtr integer) => new() { AsInteger = integer, AsPointer = (void*) integer, Ident = new(TypeHandle, integer) };
	public static MockHandleBravo CreateFromPointer(void* pointer) => new() { AsInteger = (UIntPtr) pointer, AsPointer = pointer, Ident = new(TypeHandle, (UIntPtr) pointer) };
}
readonly struct MockResourceBravo : IResource<MockResourceBravo, MockHandleBravo, IMockResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceBravo other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceBravo RecreateFromRawHandleAndImpl(UIntPtr rawHandle, IResourceImplProvider impl) {
		return new() { Handle = MockHandleBravo.CreateFromInteger(rawHandle), Implementation = (IMockResourceImplProvider) impl };
	}
	public MockHandleBravo Handle { get; init; }
	public IMockResourceImplProvider Implementation { get; init; }
}
public class MockResourceImplProvider : IMockResourceImplProvider {
	public Func<UIntPtr, string>? OnRawHandleGetName { get; set; }
	public ReadOnlySpan<char> RawHandleGetName(UIntPtr handle) => OnRawHandleGetName?.Invoke(handle) ?? "";
}