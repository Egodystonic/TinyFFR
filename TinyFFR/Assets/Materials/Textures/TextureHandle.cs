// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly unsafe struct TextureHandle : IResourceHandle<TextureHandle> {
	public nuint AsInteger { get; }
	public void* AsPointer => (void*) AsInteger;
	internal static nint TypeHandle { get; } = typeof(Texture).TypeHandle.Value;
	static nint IResourceHandle.TypeHandle => TypeHandle;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	ResourceIdent IResourceHandle.Ident => Ident;

	public TextureHandle(nuint val) => AsInteger = val;
	public TextureHandle(void* val) : this((nuint) val) { }

	public static implicit operator nuint(TextureHandle handle) => handle.AsInteger;
	public static implicit operator TextureHandle(nuint val) => new(val);
	public static implicit operator void*(TextureHandle handle) => handle.AsPointer;
	public static implicit operator TextureHandle(void* val) => new(val);

	static TextureHandle IResourceHandle<TextureHandle>.CreateFromInteger(nuint val) => new(val);
	static TextureHandle IResourceHandle<TextureHandle>.CreateFromPointer(void* val) => new(val);

	public bool Equals(TextureHandle other) => AsInteger == other.AsInteger;
	public override bool Equals(object? obj) => obj is TextureHandle other && Equals(other);
	public override int GetHashCode() => AsInteger.GetHashCode();
	public static bool operator ==(TextureHandle left, TextureHandle right) => left.Equals(right);
	public static bool operator !=(TextureHandle left, TextureHandle right) => !left.Equals(right);

	public override string ToString() => $"Texture Handle 0x{AsInteger:X16}";
}