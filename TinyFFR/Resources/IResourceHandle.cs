// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public unsafe interface IResourceHandle {
	nuint AsInteger { get; }
	void* AsPointer { get; }
	internal ResourceIdent Ident { get; }
	public static 
}
public unsafe interface IResourceHandle<TSelf> : IResourceHandle, IEquatable<TSelf> where TSelf : IResourceHandle<TSelf> {
	public static abstract TSelf CreateFromInteger(nuint integer);
	public static abstract TSelf CreateFromPointer(void* pointer);
}