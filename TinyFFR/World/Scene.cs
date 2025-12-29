// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct Scene : IDisposableResource<Scene, ISceneImplProvider> {
	public const float DefaultLux = 10_000f;
	public const float MaxBrightness = 1E15f;

	readonly ResourceHandle<Scene> _handle;
	readonly ISceneImplProvider _impl;

	internal ISceneImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Scene>();
	internal ResourceHandle<Scene> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Scene)) : _handle;

	ISceneImplProvider IResource<Scene, ISceneImplProvider>.Implementation => Implementation;
	ResourceHandle<Scene> IResource<Scene>.Handle => Handle;

	internal Scene(ResourceHandle<Scene> handle, ISceneImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Scene IResource<Scene>.CreateFromHandleAndImpl(ResourceHandle<Scene> handle, IResourceImplProvider impl) {
		return new Scene(handle, impl as ISceneImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(ModelInstance modelInstance) => Implementation.Add(_handle, modelInstance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(ModelInstance modelInstance) => Implementation.Remove(_handle, modelInstance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TLight>(TLight light) where TLight : ILight<TLight> => Implementation.Add(_handle, light);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove<TLight>(TLight light) where TLight : ILight<TLight> => Implementation.Remove(_handle, light);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdrop(BackdropTexture backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => Implementation.SetBackdrop(_handle, backdrop, backdropIntensity, rotation ?? Rotation.None);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdrop(ColorVect color, float indirectLightingIntensity = 1f) => Implementation.SetBackdrop(_handle, color, indirectLightingIntensity);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdropWithoutIndirectLighting(BackdropTexture backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => Implementation.SetBackdropWithoutIndirectLighting(_handle, backdrop, backdropIntensity, rotation ?? Rotation.None);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdropWithoutIndirectLighting(ColorVect color) => Implementation.SetBackdropWithoutIndirectLighting(_handle, color);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveBackdrop() => Implementation.RemoveBackdrop(_handle);

	public static float LuxToBrightness(float lux) {
		if (!lux.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(MathF.Sqrt(lux / DefaultLux), MaxBrightness);
	}

	public static float BrightnessToLux(float brightness) {
		if (!brightness.IsNonNegativeAndFinite()) return 0f;
		brightness = Single.Min(brightness, MaxBrightness);
		return DefaultLux * brightness * brightness;
	}

	internal void SetLightShadowFidelity(Quality qualityPreset, LightShadowFidelityData pointLightFidelity, LightShadowFidelityData spotLightFidelity, LightShadowFidelityData directionalLightFidelity) {
		Implementation.SetLightShadowFidelity(_handle, qualityPreset, pointLightFidelity, spotLightFidelity, directionalLightFidelity);
	}

	public override string ToString() => $"Scene {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(Scene other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Scene other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Scene left, Scene right) => left.Equals(right);
	public static bool operator !=(Scene left, Scene right) => !left.Equals(right);
	#endregion
}