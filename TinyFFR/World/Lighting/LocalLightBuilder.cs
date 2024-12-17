// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed class LocalLightBuilder : ILightBuilder, ILightImplProvider, IDisposable {
	readonly record struct LightData(LightType Type);
	const string DefaultModelInstanceName = "Unnamed Light";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<LightHandle, LightData> _activeLightMap = new();
	bool _isDisposed = false;

	public LocalLightBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public PointLight CreatePointLight(in PointLightCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocatePointLight(out var handle).ThrowIfFailure();
		_activeLightMap.Add(handle, new(LightType.PointLight));
		_globals.StoreResourceNameIfNotDefault(new LightHandle(handle).Ident, config.Name);
		SetLightPosition(handle, config.InitialPosition.ToVector3());
		SetLightColor(handle, config.InitialColor.ToVector3());
		return HandleToInstance<PointLight>(handle);
	}

	public LightType GetType(LightHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeLightMap[handle].Type;
	}

	public Location GetPosition(LightHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		return Location.FromVector3(result);
	}
	public void SetPosition(LightHandle handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightPosition(handle, newPosition.ToVector3()).ThrowIfFailure();
	}
	public void TranslateBy(LightHandle handle, Vect translation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		SetLightPosition(handle, result + translation.ToVector3()).ThrowIfFailure();
	}

	public ColorVect GetColor(LightHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightColor(handle, out var result);
		return ColorVect.FromVector3(result);
	}
	public void SetColor(LightHandle handle, ColorVect newColor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightColor(handle, newColor.ToVector3()).ThrowIfFailure();
	}

	public ReadOnlySpan<char> GetName(LightHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultModelInstanceName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Light HandleToInstance(LightHandle h) => new(h, this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	T HandleToInstance<T>(LightHandle h) where T : ILight<T> => T.FromBaseLight(HandleToInstance(h));

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_point_light")]
	static extern InteropResult AllocatePointLight(
		out UIntPtr outLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_light_position")]
	static extern InteropResult GetLightPosition(
		UIntPtr lightHandle,
		out Vector3 outPosition
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_position")]
	static extern InteropResult SetLightPosition(
		UIntPtr lightHandle,
		Vector3 newPosition
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_light_color")]
	static extern InteropResult GetLightColor(
		UIntPtr lightHandle,
		out Vector3 outColor
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_color")]
	static extern InteropResult SetLightColor(
		UIntPtr lightHandle,
		Vector3 newColor
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_light")]
	static extern InteropResult DisposeLight(
		UIntPtr lightHandle
	);
	#endregion

	#region Disposal
	public bool IsDisposed(LightHandle handle) => _isDisposed || !_activeLightMap.ContainsKey(handle);
	public void Dispose(LightHandle handle) => Dispose(handle, removeFromMap: true);

	void Dispose(LightHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeLight(handle).ThrowIfFailure();
		if (removeFromMap) _activeLightMap.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeLightMap) Dispose(kvp.Key, removeFromMap: false);
			_activeLightMap.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(LightHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Light));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}