// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public readonly struct Renderer : IDisposableResource<Renderer, IRendererImplProvider> {
	readonly ResourceHandle<Renderer> _handle;
	readonly IRendererImplProvider _impl;

	internal IRendererImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Renderer>();
	internal ResourceHandle<Renderer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Renderer)) : _handle;

	IRendererImplProvider IResource<Renderer, IRendererImplProvider>.Implementation => Implementation;
	ResourceHandle<Renderer> IResource<Renderer>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Renderer(ResourceHandle<Renderer> handle, IRendererImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Renderer IResource<Renderer>.CreateFromHandleAndImpl(ResourceHandle<Renderer> handle, IResourceImplProvider impl) {
		return new Renderer(handle, impl as IRendererImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Render() => Implementation.Render(_handle);

	public override string ToString() => $"Renderer {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(Renderer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Renderer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Renderer left, Renderer right) => left.Equals(right);
	public static bool operator !=(Renderer left, Renderer right) => !left.Equals(right);
	#endregion
}