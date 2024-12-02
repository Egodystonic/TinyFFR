// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IDisposable {
	const string DefaultMaterialName = "Unnamed Material";
	readonly ArrayPoolBackedMap<MaterialHandle, CombinedResourceGroup> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_material")]
	static extern InteropResult CreateMaterial(
		MaterialType type, 
		byte* argumentsBuffer, 
		int argumentsBufferLengthBytes, 
		out UIntPtr outMaterialHandle
	);
	#endregion

	public Material CreateBasicSolidColorMat(ColorVect color, ReadOnlySpan<char> name = default) => CreateBasicSolidColorMat(color, new MaterialCreationConfig { Name = name });
	public Material CreateBasicSolidColorMat(ColorVect color, in MaterialCreationConfig config) {
		CreateMaterial(
			MaterialType.BasicSolidColor,
			null,
			0,
			out var handle
		).ThrowIfFailure();

		_activeMaterials.Add(handle, _globals.ResourceGroupProvider.CreateGroup(false, 1));
		_globals.StoreResourceNameIfNotDefault(new MaterialHandle(handle).Ident, config.Name);
		return new(handle, this);
	}

	public ReadOnlySpan<char> GetName(MaterialHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMaterialName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Material HandleToInstance(MaterialHandle h) => new(h, this);

	#region Disposal
	public bool IsDisposed(MaterialHandle handle) => _isDisposed || !_activeMaterials.ContainsKey(handle);

	public void Dispose(MaterialHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(MaterialHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_activeMaterials[handle].Dispose();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeMaterials.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeMaterials) Dispose(kvp.Key, removeFromMap: false);
			_activeMaterials.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(MaterialHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Material));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}