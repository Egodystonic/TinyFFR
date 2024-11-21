// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Xml.Linq;
using static Egodystonic.TinyFFR.Resources.ICombinedResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed unsafe class LocalCombinedResourceGroupImplProvider : ICombinedResourceGroupImplProvider, IDisposable {
	readonly record struct GroupData(ResourceStub[] StubArray, int Count, bool DisposeContainedResourcesWhenDisposed, bool IsSealed) {
		public void ThrowIfSealed(ReadOnlySpan<char> name) {
			if (IsSealed) throw new ResourceGroupSealedException($"Can not add resource to {nameof(CombinedResourceGroup)} '{name}' as it is sealed.");
		}
	}

	public const int DefaultInitialCapacity = 4;
	const string DefaultGroupName = "Unnamed Resource Group";

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<ResourceStub> _stubArrayPool = ArrayPool<ResourceStub>.Shared;
	readonly ArrayPoolBackedMap<CombinedResourceGroupHandle, GroupData> _dataMap = new();
	nuint _previousGroupId = 0;
	bool _isDisposed = false;

	public LocalCombinedResourceGroupImplProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public CombinedResourceGroup CreateGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity = DefaultInitialCapacity) {
		if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "Must be positive value.");

		var stubArray = _stubArrayPool.Rent(initialCapacity);
		var handle = new CombinedResourceGroupHandle(++_previousGroupId);
		_dataMap.Add(handle, new(stubArray, 0, disposeContainedResourcesWhenDisposed, false));

		return HandleToInstance(handle);
	}

	public CombinedResourceGroup CreateGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity = DefaultInitialCapacity) {
		var result = CreateGroup(disposeContainedResourcesWhenDisposed, initialCapacity);
		_globals.StoreResourceNameIfNotDefault(result.Handle.Ident, name);
		return result;
	}

	public int GetResourceCount(CombinedResourceGroupHandle handle) {
		return GetDataForHandleOrThrow(handle).Count;
	}

	public bool IsSealed(CombinedResourceGroupHandle handle) {
		return GetDataForHandleOrThrow(handle).IsSealed;
	}

	public void Seal(CombinedResourceGroupHandle handle) {
		var data = GetDataForHandleOrThrow(handle);
		_dataMap[handle] = data with { IsSealed = true };
	}

	public ReadOnlySpan<ResourceStub> GetResources(CombinedResourceGroupHandle handle) {
		var data = GetDataForHandleOrThrow(handle);
		return data.StubArray.AsSpan(0, data.Count); 
	}

	public void AddResource<TResource>(CombinedResourceGroupHandle handle, TResource resource) where TResource : IResource {
		var data = GetDataForHandleOrThrow(handle);
		data.ThrowIfSealed(_globals.GetResourceName(handle.Ident, DefaultGroupName));

		if (data.Count == data.StubArray.Length) {
			var newArray = _stubArrayPool.Rent(data.Count * 2);
			data.StubArray.CopyTo(newArray.AsSpan());
			_stubArrayPool.Return(data.StubArray);
			data = data with { StubArray = newArray };
		}
		data.StubArray[data.Count] = resource.AsStub;
		_dataMap[handle] = data with { Count = data.Count + 1 };
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), resource);
	}

	public OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>(CombinedResourceGroupHandle handle) where TResource : IResource<TResource> {
		ThrowIfThisOrHandleIsDisposed(handle);

		return new OneToManyEnumerator<EnumerationArg, TResource>(
			new(this, handle, typeof(TResource).TypeHandle.Value),
			&GetEnumeratorResourceCount,
			&GetEnumeratorResourceAtIndex<TResource>
		);
	}
	static int GetEnumeratorResourceCount(EnumerationArg arg) {
		var implProvider = (arg.Impl as LocalCombinedResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalCombinedResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(arg.Handle);

		var result = 0;
		for (var i = 0; i < data.Count; ++i) {
			if (data.StubArray[i].TypeHandle == arg.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorResourceAtIndex<TResource>(EnumerationArg arg, int index) where TResource : IResource<TResource> {
		var implProvider = (arg.Impl as LocalCombinedResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalCombinedResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(arg.Handle);

		var count = 0;
		for (var i = 0; i < data.Count; ++i) {
			var stub = data.StubArray[i];
			if (stub.TypeHandle != arg.ResourceTypeHandle) continue;
			if (count == index) return TResource.RecreateFromStub(stub);
			++count;
		}

		throw new IndexOutOfRangeException($"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource>(CombinedResourceGroupHandle handle, int index) where TResource : IResource<TResource> {
		return GetEnumeratorResourceAtIndex<TResource>(new EnumerationArg(this, handle, typeof(TResource).TypeHandle.Value), index);
	}

	public ReadOnlySpan<char> GetName(CombinedResourceGroupHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultGroupName);
	}

	public bool IsDisposed(CombinedResourceGroupHandle handle) => !_dataMap.ContainsKey(handle.AsInteger);
	public void Dispose(CombinedResourceGroupHandle handle) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.StubArray, data.Count, data.DisposeContainedResourcesWhenDisposed);
	}
	public void Dispose(CombinedResourceGroupHandle handle, bool disposeContainedResources) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.StubArray, data.Count, disposeContainedResources);
	}
	void Dispose(CombinedResourceGroupHandle handle, ResourceStub[] stubArray, int count, bool disposeContainedResources) {
		for (var i = 0; i < count; ++i) {
			var resource = stubArray[i];
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), resource);
			if (disposeContainedResources) resource.Dispose();
		}

		_globals.DisposeResourceNameIfExists(handle.Ident);
		_dataMap.Remove(handle);
		_stubArrayPool.Return(stubArray);
	}
	public bool DisposedContainedResourcesByDefaultWhenDisposed(CombinedResourceGroupHandle handle) => GetDataForHandleOrThrow(handle).DisposeContainedResourcesWhenDisposed;
	public void Dispose() {
		if (_isDisposed) return;
		_dataMap.Dispose();
		_isDisposed = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	CombinedResourceGroup HandleToInstance(CombinedResourceGroupHandle handle) => new(handle, this);

	void ThrowIfThisOrHandleIsDisposed(CombinedResourceGroupHandle handle) {
		ThrowIfThisIsDisposed();
		if (_dataMap.ContainsKey(handle)) return;
		
		if (handle == default) throw InvalidObjectException.InvalidDefault<CombinedResourceGroup>();
		else throw new ObjectDisposedException(nameof(CombinedResourceGroup));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}

	GroupData GetDataForHandleOrThrow(CombinedResourceGroupHandle handle) {
		ThrowIfThisIsDisposed();
		if (_dataMap.TryGetValue(handle, out var result)) return result;

		if (handle == 0UL) throw InvalidObjectException.InvalidDefault<CombinedResourceGroup>();
		else throw new ObjectDisposedException(nameof(CombinedResourceGroup));
	}
}