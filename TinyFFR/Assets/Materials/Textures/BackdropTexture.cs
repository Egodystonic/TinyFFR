// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// The surroundings of a scene: the sky visible behind it, and the ambient light that sky casts on everything in it. Loaded via the factory's <see cref="IAssetLoader"/>.
/// </summary>
/// <remarks>
/// <para>
/// A backdrop does both jobs at once, which is what makes objects lit by one look as though they genuinely belong in it. A
/// scene set at sunset picks up warm light from the side without any light being placed by hand.
/// </para>
/// <para>
/// Backdrops are loaded from high-dynamic-range images by the asset loader.
/// </para>
/// </remarks>
public readonly struct BackdropTexture : IDisposableResource<BackdropTexture, IBackdropTextureImplProvider> {
	readonly ResourceHandle<BackdropTexture> _handle;
	readonly IBackdropTextureImplProvider _impl;

	internal ResourceHandle<BackdropTexture> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(BackdropTexture)) : _handle;
	internal IBackdropTextureImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<BackdropTexture>();

	IBackdropTextureImplProvider IResource<BackdropTexture, IBackdropTextureImplProvider>.Implementation => Implementation;
	ResourceHandle<BackdropTexture> IResource<BackdropTexture>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal UIntPtr SkyboxTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSkyboxTextureHandle(_handle);
	}
	internal UIntPtr IndirectLightingTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndirectLightingTextureHandle(_handle);
	} 

	/// <summary>
	/// The illuminance, in lux, that this backdrop delivers to an upward-facing surface when used at an intensity of <c>1f</c> (i.e. as authored), measured from its lighting data when it was loaded.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Image files rarely record how bright the scene they depict really was, so different files can light a scene very differently at the same intensity. This value
	/// describes how brightly <i>this</i> file lights a scene, so that it can be lit to a chosen real-world illuminance instead (see <see cref="LuxToIntensity"/>, or pass a
	/// <see cref="SceneBackdropBrightnessPreset"/> to <c>Scene.SetBackdrop()</c>).
	/// </para>
	/// <para>
	/// The measurement includes all the light in the image, including that of a sun if the image shows one, and is made with the backdrop unrotated. If the file contains no
	/// lighting data that can be measured, this is an estimate based on a typical backdrop.
	/// </para>
	/// </remarks>
	public float MeasuredLux {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMeasuredLux(_handle);
	}

	/// <summary>
	/// Returns the intensity at which this backdrop lights a scene to the given illuminance (see <see cref="MeasuredLux"/>).
	/// </summary>
	/// <remarks>
	/// Like the brightness of a light, intensity is perceptual; an intensity of <c>2f</c> looks twice as bright as <c>1f</c>, which takes four times the illuminance.
	/// A negative or non-finite <paramref name="lux"/> returns <c>0f</c>.
	/// </remarks>
	/// <param name="lux">The illuminance to light a scene to, in lux.</param>
	public float LuxToIntensity(float lux) => BackdropIntensityUtils.LuxToIntensity(lux, MeasuredLux);
	/// <summary>
	/// Returns the illuminance, in lux, to which this backdrop lights a scene at the given intensity (see <see cref="MeasuredLux"/>).
	/// </summary>
	/// <remarks>
	/// Like the brightness of a light, intensity is perceptual; an intensity of <c>2f</c> looks twice as bright as <c>1f</c>, which takes four times the illuminance.
	/// A negative or non-finite <paramref name="intensity"/> returns <c>0f</c>.
	/// </remarks>
	/// <param name="intensity">The backdrop intensity, where <c>1f</c> is the backdrop as authored.</param>
	public float IntensityToLux(float intensity) => BackdropIntensityUtils.IntensityToLux(intensity, MeasuredLux);

	internal BackdropTexture(ResourceHandle<BackdropTexture> handle, IBackdropTextureImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static BackdropTexture IResource<BackdropTexture>.CreateFromHandleAndImpl(ResourceHandle<BackdropTexture> handle, IResourceImplProvider impl) {
		return new BackdropTexture(handle, impl as IBackdropTextureImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<BackdropTexture> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<BackdropTexture> IResource<BackdropTexture>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this backdrop, releasing its video memory.
	/// </summary>
	/// <remarks>
	/// Every scene using this backdrop must have stopped using it first.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Backdrop Texture {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(BackdropTexture other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is BackdropTexture other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given backdrop textures are the same backdrop texture.
	/// </summary>
	/// <param name="left">The first backdrop texture to compare.</param>
	/// <param name="right">The second backdrop texture to compare.</param>
	public static bool operator ==(BackdropTexture left, BackdropTexture right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given backdrop textures are different backdrop textures.
	/// </summary>
	/// <param name="left">The first backdrop texture to compare.</param>
	/// <param name="right">The second backdrop texture to compare.</param>
	public static bool operator !=(BackdropTexture left, BackdropTexture right) => !left.Equals(right);
	#endregion
}