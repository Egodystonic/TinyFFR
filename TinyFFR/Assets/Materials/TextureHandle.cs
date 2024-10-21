// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly unsafe struct TextureHandle : IResourceHandle<TextureHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static IntPtr TypeHandle { get; } = typeof(TextureHandle).TypeHandle.Value;
	static IntPtr IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public TextureHandle(nuint integer) => AsInteger = integer;
	public TextureHandle(void* pointer) : this((nuint) pointer) { }

	public static implicit operator nuint(TextureHandle handle) => handle.AsInteger;
	public static implicit operator TextureHandle(nuint integer) => new(integer);
	public static implicit operator void*(TextureHandle handle) => handle.AsPointer;
	public static implicit operator TextureHandle(void* pointer) => new(pointer);

	static TextureHandle IResourceHandle<TextureHandle>.CreateFromInteger(nuint integer) => new(integer);
	static TextureHandle IResourceHandle<TextureHandle>.CreateFromPointer(void* pointer) => new(pointer);

	public bool Equals(TextureHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is TextureHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(TextureHandle left, TextureHandle right) => left.Equals(right);
	public static bool operator !=(TextureHandle left, TextureHandle right) => !left.Equals(right);

	public override string ToString() => $"Texture Handle 0x{AsInteger:X16}";
}