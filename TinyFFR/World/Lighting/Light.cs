// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct Light : IDisposableResource<Light, LightHandle, ILightImplProvider>, IPositionedSceneObject, IOrientedSceneObject {
	readonly LightHandle _handle;
	readonly ILightImplProvider _impl;

	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Light>();
	internal LightHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Light)) : _handle;

	ILightImplProvider IResource<LightHandle, ILightImplProvider>.Implementation => Implementation;
	LightHandle IResource<LightHandle, ILightImplProvider>.Handle => Handle;

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
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetRotation(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetRotation(_handle, value);
	}

	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetColor(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetColor(_handle, value);
	}

	internal Light(LightHandle handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Light IResource<Light>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Light(rawHandle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);
	public void RotateBy(Rotation rotation) => Implementation.RotateBy(_handle, rotation);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Light {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(ModelInstance other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ModelInstance other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(ModelInstance left, ModelInstance right) => left.Equals(right);
	public static bool operator !=(ModelInstance left, ModelInstance right) => !left.Equals(right);
	#endregion
}