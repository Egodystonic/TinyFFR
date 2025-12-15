// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Xml.Linq;
using static Egodystonic.TinyFFR.Resources.IResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed unsafe class LocalResourceGroupImplProvider : IResourceGroupImplProvider, IDisposable {
	readonly record struct GroupData(ResourceStub[] StubArray, int Count, bool DisposeContainedResourcesWhenDisposed, bool IsSealed) {
		public void ThrowIfSealed(ReadOnlySpan<char> name) {
			if (IsSealed) throw new ResourceGroupSealedException($"Can not add resource to {nameof(ResourceGroup)} '{name}' as it is sealed.");
		}
	}

	public const int DefaultInitialCapacity = 4;
	const string DefaultGroupName = "Unnamed Resource Group";

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<ResourceStub> _stubArrayPool = ArrayPool<ResourceStub>.Shared;
	readonly ArrayPoolBackedMap<ResourceHandle<ResourceGroup>, GroupData> _dataMap = new();
	nuint _previousGroupId = 0;
	bool _isDisposed = false;

	public LocalResourceGroupImplProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public ResourceGroup CreateGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity = DefaultInitialCapacity) {
		if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "Must be positive value.");

		var stubArray = _stubArrayPool.Rent(initialCapacity);
		var handle = new ResourceHandle<ResourceGroup>(++_previousGroupId);
		_dataMap.Add(handle, new(stubArray, 0, disposeContainedResourcesWhenDisposed, false));

		return HandleToInstance(handle);
	}

	public ResourceGroup CreateGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity = DefaultInitialCapacity) {
		var result = CreateGroup(disposeContainedResourcesWhenDisposed, initialCapacity);
		_globals.StoreResourceNameOrDefaultIfEmpty(result.Handle.Ident, name, DefaultGroupName);
		return result;
	}

	public int GetResourceCount(ResourceHandle<ResourceGroup> handle) {
		return GetDataForHandleOrThrow(handle).Count;
	}

	public bool IsSealed(ResourceHandle<ResourceGroup> handle) {
		return GetDataForHandleOrThrow(handle).IsSealed;
	}

	public void Seal(ResourceHandle<ResourceGroup> handle) {
		var data = GetDataForHandleOrThrow(handle);
		_dataMap[handle] = data with { IsSealed = true };
	}

	public ReadOnlySpan<ResourceStub> GetResources(ResourceHandle<ResourceGroup> handle) {
		var data = GetDataForHandleOrThrow(handle);
		return data.StubArray.AsSpan(0, data.Count); 
	}

	public void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource {
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

	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceHandle<ResourceGroup> handle) where TResource : IResource<TResource> {
		return new IndirectEnumerable<EnumerationInput, TResource>(
			new(this, handle, typeof(TResource).TypeHandle.Value),
			GetDataForHandleOrThrow(handle).Count,
			&GetEnumeratorResourceCount,
			&GetEnumeratorStateVersion,
			&GetEnumeratorResourceAtIndex<TResource>
		);
	}
	static int GetEnumeratorStateVersion(EnumerationInput input) => (input.Impl as LocalResourceGroupImplProvider)!.GetDataForHandleOrThrow(input.Handle).Count;
	static int GetEnumeratorResourceCount(EnumerationInput input) {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var result = 0;
		for (var i = 0; i < data.Count; ++i) {
			if (data.StubArray[i].TypeHandle == input.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorResourceAtIndex<TResource>(EnumerationInput input, int index) where TResource : IResource<TResource> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var count = 0;
		for (var i = 0; i < data.Count; ++i) {
			var stub = data.StubArray[i];
			if (stub.TypeHandle != input.ResourceTypeHandle) continue;
			if (count == index) return TResource.CreateFromStub(stub);
			++count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource> {
		return GetEnumeratorResourceAtIndex<TResource>(new EnumerationInput(this, handle, typeof(TResource).TypeHandle.Value), index);
	}

	public string GetNameAsNewStringObject(ResourceHandle<ResourceGroup> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultGroupName));
	}
	public int GetNameLength(ResourceHandle<ResourceGroup> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultGroupName).Length;
	}
	public void CopyName(ResourceHandle<ResourceGroup> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultGroupName, destinationBuffer);
	}

	public bool IsDisposed(ResourceHandle<ResourceGroup> handle) => !_dataMap.ContainsKey(handle.AsInteger);
	public void Dispose(ResourceHandle<ResourceGroup> handle) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.StubArray, data.Count, data.DisposeContainedResourcesWhenDisposed);
	}
	public void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.StubArray, data.Count, disposeContainedResources);
	}
	void Dispose(ResourceHandle<ResourceGroup> handle, ResourceStub[] stubArray, int count, bool disposeContainedResources) {
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		// Maintainer's note: Reverse order of disposal is important to help dispose items in the correct order according to their dependency chains
		// This doesn't guarantee anything of course, but makes it more likely that thoughtless use of this type will work okay
		for (var i = count - 1; i >= 0; --i) {
			var resource = stubArray[i];
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), resource);
			if (disposeContainedResources) resource.Dispose();
		}

		_globals.DisposeResourceNameIfExists(handle.Ident);
		_dataMap.Remove(handle);
		_stubArrayPool.Return(stubArray);
	}
	public bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceHandle<ResourceGroup> handle) => GetDataForHandleOrThrow(handle).DisposeContainedResourcesWhenDisposed;
	public void Dispose() {
		if (_isDisposed) return;
		_dataMap.Dispose();
		_isDisposed = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ResourceGroup HandleToInstance(ResourceHandle<ResourceGroup> handle) => new(handle, this);

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ResourceGroup> handle) {
		ThrowIfThisIsDisposed();
		if (_dataMap.ContainsKey(handle)) return;
		
		if (handle == default) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
		else throw new ObjectDisposedException(nameof(ResourceGroup));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}

	GroupData GetDataForHandleOrThrow(ResourceHandle<ResourceGroup> handle) {
		ThrowIfThisIsDisposed();
		if (_dataMap.TryGetValue(handle, out var result)) return result;

		if (handle == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
		else throw new ObjectDisposedException(nameof(ResourceGroup));
	}
}