// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed unsafe class LocalMeshAnimationTable : IMeshAnimationImplProvider, IDisposable {
	public readonly record struct AnimationData();
	readonly ArrayPoolBackedStringKeyMap<MeshAnimation> _nameMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<MeshAnimation>, AnimationData> _dataMap = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalMeshAnimationTable(LocalFactoryGlobalObjectGroup globals) => _globals = globals;

	public int Count => _nameMap.Count;
	public MeshAnimation? FindByName(ReadOnlySpan<char> name) => _nameMap.TryGetValue(name, out var animData) ? animData : null;
	public void AddAnimation(ReadOnlySpan<char> name, MeshAnimation animData) => _nameMap.Add(name, animData);
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.KeyEnumerator Keys => _nameMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.ValueEnumerator Values => _nameMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.Enumerator GetEnumerator() => _nameMap.GetEnumerator();
	
	public MeshAnimation Add(/* data structure(s) here for skeletal anim */) {
		ThrowIfThisIsDisposed();
		// TODO add data to _dataMap and add the resultant MeshAnimation to _nameMap. Store name with _globals.StoreMandatoryResourceName(...)
		return HandleToInstance(newHandle);
	}

	public float GetDefaultCompletionTimeSeconds(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		// TODO return the default animation completion time
	}
	public MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return MeshAnimationType.Skeletal;
	}
	public void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		// TODO apply the animation at the given target time point to the given targetInstance
	}
	
	public string GetNameAsNewStringObject(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetMandatoryResourceName(handle.Ident));
	}
	public int GetNameLength(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetMandatoryResourceName(handle.Ident).Length;
	}
	public void CopyName(ResourceHandle<MeshAnimation> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyMandatoryResourceName(handle.Ident, destinationBuffer);
	}
	
	public MeshAnimation GetAnimationAtUnstableIndex(int index) => HandleToInstance(_dataMap.GetPairAtIndex(index).Key);
	
	public override string ToString() => _isDisposed ? "TinyFFR Local Mesh Animation Table [Disposed]" : "TinyFFR Local Mesh Animation Table";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	MeshAnimation HandleToInstance(ResourceHandle<MeshAnimation> h) => new(h, this);
	
	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_skeletal_animation")]
	static extern InteropResult DisposeSkeletalAnimation(
		UIntPtr animHandle
	);
	#endregion
	
	#region Disposal
	public bool IsDisposed(ResourceHandle<MeshAnimation> handle) => _isDisposed || !_dataMap.ContainsKey(handle);
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<MeshAnimation> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(MeshAnimation));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	
	public void QueueAllAnimationsForDisposal() {
		foreach (var kvp in _dataMap) {
			LocalFrameSynchronizationManager.QueueResourceDisposal(kvp.Key, &DisposeSkeletalAnimation);
		}
	}
	
	public void Recycle() {
		_nameMap.Clear();
		_dataMap.Clear();
	}

	public void Dispose() {
		_isDisposed = true;
		_nameMap.Dispose();
		_dataMap.Dispose();
	}
	#endregion
	
	

}