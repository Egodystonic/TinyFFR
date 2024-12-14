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
	const string DefaultModelInstanceName = "Unnamed Light";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<LightHandle, Transform> _activeInstanceMap = new();
	bool _isDisposed = false;

	public LocalLightBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}
	


	public ReadOnlySpan<char> GetName(LightHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultModelInstanceName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Light HandleToInstance(LightHandle h) => new(h, this);

	#region Native Methods
	
	#endregion

	#region Disposal
	public bool IsDisposed(LightHandle handle) => _isDisposed || !_activeInstanceMap.ContainsKey(handle);
	public void Dispose(LightHandle handle) => Dispose(handle, removeFromMap: true);

	void Dispose(LightHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeLight(handle).ThrowIfFailure();
		if (removeFromMap) _activeInstanceMap.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeInstanceMap) Dispose(kvp.Key, removeFromMap: false);
			_activeInstanceMap.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(LightHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Light));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}