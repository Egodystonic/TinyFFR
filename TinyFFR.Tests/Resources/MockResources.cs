// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IMockResourceImplProvider : IResourceImplProvider;

readonly struct MockHandleAlpha : IResourceHandle<MockHandleAlpha> {
	public Func<MockHandleAlpha, bool>? OnEquals { get; init; }

	public UIntPtr AsInteger { get; init; }
	public unsafe void* AsPointer { get; init; }
	public ResourceIdent Ident { get; init; }
	public static IntPtr TypeHandle => typeof(MockResourceAlpha).TypeHandle.Value;

	public bool Equals(MockHandleAlpha other) 
	public static MockHandleAlpha CreateFromInteger(UIntPtr integer) { return default; }
	public static unsafe MockHandleAlpha CreateFromPointer(void* pointer) { return default; }
} 
readonly struct MockResourceAlpha : IResource<MockResourceAlpha, MockHandleAlpha, IMockResourceImplProvider> {

}