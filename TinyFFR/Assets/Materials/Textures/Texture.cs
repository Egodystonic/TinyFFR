// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Texture : IDisposableResource<Texture, ITextureImplProvider> {
	readonly ResourceHandle<Texture> _handle;
	readonly ITextureImplProvider _impl;

	internal ResourceHandle<Texture> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Texture)) : _handle;
	internal ITextureImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Texture>();

	ITextureImplProvider IResource<Texture, ITextureImplProvider>.Implementation => Implementation;
	ResourceHandle<Texture> IResource<Texture>.Handle => Handle;

	public XYPair<int> Dimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDimensions(_handle);
	}

	public TexelType TexelType {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTexelType(_handle);
	}

	public bool AllowsDynamicWrites {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAllowsDynamicWrites(_handle);
	}
	
	public bool ContainsMipMaps {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetContainsMipMaps(_handle);
	}
	
	public TextureSamplingConfig SamplingConfig {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSamplingConfig(_handle);
	}

	internal Texture(ResourceHandle<Texture> handle, ITextureImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	// TODO make it clear this throws if AllowsDynamicWrites is false
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void OverwriteTexels<TTexel>(ReadOnlySpan<TTexel> newTexels) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		OverwriteTexels(newTexels, Dimensions, XYPair<int>.Zero);
	}
	// TODO make it clear this throws if AllowsDynamicWrites is false
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void OverwriteTexels<TTexel>(ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		Implementation.OverwriteTexels(_handle, newTexels, dimensions, offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Texture IResource<Texture>.CreateFromHandleAndImpl(ResourceHandle<Texture> handle, IResourceImplProvider impl) {
		return new Texture(handle, impl as ITextureImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<Texture> GetHandleWithoutDisposeCheck() => _handle;

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Texture {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(Texture other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	public override bool Equals(object? obj) => obj is Texture other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Texture left, Texture right) => left.Equals(right);
	public static bool operator !=(Texture left, Texture right) => !left.Equals(right);
	#endregion
}