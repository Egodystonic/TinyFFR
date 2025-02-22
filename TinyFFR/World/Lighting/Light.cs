// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct Light : ILight, IDisposableResource<Light, ILightImplProvider> {
	readonly ResourceHandle<Light> _handle;
	readonly ILightImplProvider _impl;

	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Light>();
	internal ResourceHandle<Light> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Light)) : _handle;

	ILightImplProvider IResource<Light, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<Light> IResource<Light>.Handle => Handle;

	public LightType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetType(_handle);
	}

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetColor(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetColor(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Color = color;

	internal Light(ResourceHandle<Light> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Light IResource<Light>.CreateFromHandleAndImpl(ResourceHandle<Light> handle, IResourceImplProvider impl) {
		return new Light(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Light {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	internal static void ThrowIfInvalidType(Light input, LightType requiredType) {
		if (input.Type == requiredType) return;
		throw TypeUtils.InvalidCast(input, requiredType, input.Type);
	}

	#region Equality
	public bool Equals(Light other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Light other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Light left, Light right) => left.Equals(right);
	public static bool operator !=(Light left, Light right) => !left.Equals(right);
	#endregion
}