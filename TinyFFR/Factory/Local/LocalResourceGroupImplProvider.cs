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

readonly unsafe struct SerializedResourceData {
	public ResourceStub Stub { get; init; }
	public IntPtr SpecializationTypeIdentifier { get; init; }
	public PooledHeapMemory<byte> SpecializationData { get; init; }
	public ResourceStub? AdditionalResourceRef { get; init; }
	public delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, void> SpecializedResourceDisposalStub { get; init; }
	public SerializedResourceData(ResourceStub stub, IntPtr specializationTypeIdentifier, PooledHeapMemory<byte> specializationData, ResourceStub? additionalResourceRef, delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, void> specializedResourceDisposalStub) {
		Stub = stub;
		SpecializationTypeIdentifier = specializationTypeIdentifier;
		SpecializationData = specializationData;
		AdditionalResourceRef = additionalResourceRef;
		SpecializedResourceDisposalStub = specializedResourceDisposalStub;
	}
}

sealed unsafe class LocalResourceGroupImplProvider : IResourceGroupImplProvider, IResourceDirectory<ResourceGroup>, IDisposable {
	readonly record struct GroupData(SerializedResourceData[] DataArray, int Count, bool DisposeContainedResourcesWhenDisposed, bool IsSealed) {
		public void ThrowIfSealed(ReadOnlySpan<char> name) {
			if (IsSealed) throw new ResourceGroupSealedException($"Can not add resource to {nameof(ResourceGroup)} '{name}' as it is sealed.");
		}
	}

	public const int DefaultInitialCapacity = 4;
	const string DefaultGroupName = "Unnamed Resource Group";

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<SerializedResourceData> _dataArrayPool = ArrayPool<SerializedResourceData>.Shared;
	readonly ArrayPoolBackedMap<ResourceHandle<ResourceGroup>, GroupData> _dataMap = new();
	nuint _previousGroupId = 0;
	bool _isDisposed = false;

	public LocalResourceGroupImplProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public ResourceGroup CreateGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity = DefaultInitialCapacity) {
		if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "Must be positive value.");

		var stubArray = _dataArrayPool.Rent(initialCapacity);
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

	public void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource {
		AddResource(handle, new SerializedResourceData(resource.AsStub, default, default, default, null), resource);
	}

	public void AddResource<TResource, TBase>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		var specializationDataBuffer = _globals.HeapPool.Borrow(resource.SpecializationDataLength);
		TResource.Smuggle(resource, specializationDataBuffer.Span, out var underlyingResource, out var additionalResourceRef);
		AddResource(handle, new(underlyingResource.AsStub, TResource.SpecializationTypeIdentifier, specializationDataBuffer, additionalResourceRef, &IResourceSpecialization<TResource, TBase>.DisposeViaStub), underlyingResource);
	}
	
	void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, SerializedResourceData serializedData, TResource underlyingResource) where TResource : IResource {
		var data = GetDataForHandleOrThrow(handle);
		data.ThrowIfSealed(_globals.GetResourceName(handle.Ident, DefaultGroupName));

		if (data.Count == data.DataArray.Length) {
			var newArray = _dataArrayPool.Rent(data.Count * 2);
			data.DataArray.CopyTo(newArray.AsSpan());
			_dataArrayPool.Return(data.DataArray);
			data = data with { DataArray = newArray };
		}
		data.DataArray[data.Count] = serializedData;
		_dataMap[handle] = data with { Count = data.Count + 1 };
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), underlyingResource);
	}

	#region Standard Resource Enumeration
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
			if (data.DataArray[i].Stub.TypeHandle == input.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorResourceAtIndex<TResource>(EnumerationInput input, int index) where TResource : IResource<TResource> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var count = 0;
		for (var i = 0; i < data.Count; ++i) {
			var stub = data.DataArray[i].Stub;
			if (stub.TypeHandle != input.ResourceTypeHandle) continue;
			if (count == index) return TResource.CreateFromStub(stub);
			++count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource> {
		return GetEnumeratorResourceAtIndex<TResource>(new EnumerationInput(this, handle, typeof(TResource).TypeHandle.Value), index);
	}
	#endregion

	#region Specialized Resource Enumeration
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle) where TResource : IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return new IndirectEnumerable<EnumerationInput, TResource>(
			new(this, handle, (nint) TResource.SpecializationTypeIdentifier),
			GetDataForHandleOrThrow(handle).Count,
			&GetEnumeratorSpecializedResourceCount,
			&GetEnumeratorStateVersion,
			&GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>
		);
	}
	static int GetEnumeratorSpecializedResourceCount(EnumerationInput input) {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var result = 0;
		for (var i = 0; i < data.Count; ++i) {
			if (data.DataArray[i].SpecializationTypeIdentifier == input.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>(EnumerationInput input, int index) where TResource : IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var count = 0;
		for (var i = 0; i < data.Count; ++i) {
			var d = data.DataArray[i];
			if (d.SpecializationTypeIdentifier != input.ResourceTypeHandle) continue;
			if (count == index) return TResource.DeSmuggle(TBase.CreateFromStub(d.Stub), d.SpecializationData.Span, d.AdditionalResourceRef);
			++count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>(new EnumerationInput(this, handle, typeof(TResource).TypeHandle.Value), index);
	}
	#endregion

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
	
	#region Resource Directory
	public IndirectEnumerable<object, ResourceGroup> AllActiveInstances {
		get {
			static LocalResourceGroupImplProvider CastSelf(object self) => self as LocalResourceGroupImplProvider ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._dataMap.Count;
			static int GetVersion(object self) => CastSelf(self)._dataMap.Version;
			static ResourceGroup GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._dataMap.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(ResourceGroup resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultGroupName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultGroupName).Equals(name, comparisonType);
	}
	#endregion

	public bool IsDisposed(ResourceHandle<ResourceGroup> handle) => !_dataMap.ContainsKey(handle.AsInteger);
	public void Dispose(ResourceHandle<ResourceGroup> handle) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.DataArray, data.Count, data.DisposeContainedResourcesWhenDisposed);
	}
	public void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data.DataArray, data.Count, disposeContainedResources);
	}
	void Dispose(ResourceHandle<ResourceGroup> handle, SerializedResourceData[] dataArray, int count, bool disposeContainedResources) {
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		// Maintainer's note: Reverse order of disposal is important to help dispose items in the correct order according to their dependency chains
		// This doesn't guarantee anything of course, but makes it more likely that thoughtless use of this type will work okay
		for (var i = count - 1; i >= 0; --i) {
			var data = dataArray[i];
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Stub);
			var isSpecializedResource = data.SpecializationTypeIdentifier != 0U;
			if (isSpecializedResource) data.SpecializationData.Dispose();
			if (!disposeContainedResources) continue;
			
			if (isSpecializedResource) {
				if (data.SpecializedResourceDisposalStub != null) {
					data.SpecializedResourceDisposalStub(data.Stub, data.SpecializationData.Span, data.AdditionalResourceRef);
				}
			}
			else data.Stub.Dispose();
		}

		_globals.DisposeResourceNameIfExists(handle.Ident);
		_dataMap.Remove(handle);
		_dataArrayPool.Return(dataArray);
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