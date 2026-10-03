// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Xml.Linq;
using static Egodystonic.TinyFFR.Resources.IResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Factory.Local;

readonly unsafe struct SerializedResourceData {
	public ResourceStub Stub { get; init; }
	public nint AddedAsTypeHandle { get; init; }
	public bool DoNotDispose { get; init; }
	public delegate* managed<ResourceStub, object> ResourceBoxingStub { get; init; }
	public SerializedResourceData(ResourceStub stub, nint addedAsTypeHandle, delegate* managed<ResourceStub, object> resourceBoxingStub) {
		Stub = stub;
		AddedAsTypeHandle = addedAsTypeHandle;
		ResourceBoxingStub = resourceBoxingStub;
	}
}

sealed unsafe class LocalResourceGroupImplProvider : IResourceGroupImplProvider, IResourceDirectory<ResourceGroup>, IDisposable, ILocalResourceImplProvider {
	readonly record struct GroupData(SerializedResourceData[] DataArray, int Count, bool DisposeContainedResourcesWhenDisposed, bool IsSealed, int[]? TypeIndex) {
		public void ThrowIfSealed(ReadOnlySpan<char> name) {
			if (IsSealed) throw new ResourceGroupSealedException($"Can not add resource to {nameof(ResourceGroup)} '{name}' as it is sealed.");
		}
	}

	static class TypeKey<TResource> where TResource : IResource {
		public static readonly int Value = GetTypeKey(typeof(TResource).TypeHandle.Value);
	}

	public const int DefaultInitialCapacity = 4;
	const string DefaultGroupName = "Unnamed Resource Group";
	static readonly nint[] IndexedTypeHandles = [
		typeof(ApplicationLoop).TypeHandle.Value,
		typeof(BackdropTexture).TypeHandle.Value,
		typeof(Camera).TypeHandle.Value,
		typeof(CameraLockedQuadInstance).TypeHandle.Value,
		typeof(CameraLockedTextInstance).TypeHandle.Value,
		typeof(CanvasScene).TypeHandle.Value,
		typeof(CanvasText).TypeHandle.Value,
		typeof(CanvasTexture).TypeHandle.Value,
		typeof(DirectionalLight).TypeHandle.Value,
		typeof(Display).TypeHandle.Value,
		typeof(Font).TypeHandle.Value,
		typeof(FontPen).TypeHandle.Value,
		typeof(FontString).TypeHandle.Value,
		typeof(Material).TypeHandle.Value,
		typeof(Mesh).TypeHandle.Value,
		typeof(MeshGroupAnimationTable).TypeHandle.Value,
		typeof(MeshAnimation).TypeHandle.Value,
		typeof(MeshNode).TypeHandle.Value,
		typeof(Model).TypeHandle.Value,
		typeof(ModelInstance).TypeHandle.Value,
		typeof(PointLight).TypeHandle.Value,
		typeof(QuadInstance).TypeHandle.Value,
		typeof(QuadMesh).TypeHandle.Value,
		typeof(Renderer).TypeHandle.Value,
		typeof(RendererCompositor).TypeHandle.Value,
		typeof(RenderOutputBuffer).TypeHandle.Value,
		typeof(ResourceGroup).TypeHandle.Value,
		typeof(Scene).TypeHandle.Value,
		typeof(SpotLight).TypeHandle.Value,
		typeof(TextInstance).TypeHandle.Value,
		typeof(Texture).TypeHandle.Value,
		typeof(Window).TypeHandle.Value,
	];
	static readonly int TypeIndexHeaderLength = IndexedTypeHandles.Length * 2;

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<SerializedResourceData> _dataArrayPool = TinyFfrArrayPool<SerializedResourceData>.Shared;
	readonly ArrayPool<int> _typeIndexPool = TinyFfrArrayPool<int>.Shared;
	readonly ArrayPoolBackedMap<ResourceHandle<ResourceGroup>, GroupData> _dataMap = new();
	readonly ArrayPoolBackedVector<ResourceHandle<ResourceGroup>> _cycleCheckStack = new();
	readonly ArrayPoolBackedSet<ResourceHandle<ResourceGroup>> _cycleCheckVisited = new();
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
		_dataMap.Add(handle, new(stubArray, 0, disposeContainedResourcesWhenDisposed, false, null));

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
		if (data.IsSealed) return;
		_dataMap[handle] = data with { IsSealed = true, TypeIndex = data.Count > 0 ? CreateTypeIndex(data) : null };
	}

	int[] CreateTypeIndex(GroupData data) {
		var result = _typeIndexPool.Rent(TypeIndexHeaderLength + data.Count);
		var header = result.AsSpan(0, TypeIndexHeaderLength);
		var indices = result.AsSpan(TypeIndexHeaderLength, data.Count);
		header.Clear();

		for (var i = 0; i < data.Count; ++i) {
			var key = GetTypeKey(data.DataArray[i].AddedAsTypeHandle);
			if (key >= 0) ++header[key * 2 + 1];
		}

		Span<int> cursors = stackalloc int[IndexedTypeHandles.Length];
		var start = 0;
		for (var key = 0; key < cursors.Length; ++key) {
			header[key * 2] = start;
			cursors[key] = start;
			start += header[key * 2 + 1];
		}

		for (var i = 0; i < data.Count; ++i) {
			var key = GetTypeKey(data.DataArray[i].AddedAsTypeHandle);
			if (key >= 0) indices[cursors[key]++] = i;
		}

		return result;
	}

	static int GetTypeKey(nint typeHandle) {
		for (var i = 0; i < IndexedTypeHandles.Length; ++i) {
			if (IndexedTypeHandles[i] == typeHandle) return i;
		}
		return -1;
	}

	static bool TryGetIndexedRange(in GroupData data, int typeKey, out ReadOnlySpan<int> indices) {
		var typeIndex = data.TypeIndex;
		if (typeKey < 0 || typeIndex == null) {
			indices = default;
			return false;
		}
		indices = new ReadOnlySpan<int>(typeIndex, TypeIndexHeaderLength + typeIndex[typeKey * 2], typeIndex[typeKey * 2 + 1]);
		return true;
	}

	public void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource {
		var data = ValidateCanAddAndGetData(handle);
		if (typeof(TResource) == typeof(ResourceGroup) && ReferenceEquals(resource.Implementation, this)) {
			ThrowIfAdditionWouldCreateCycle(handle, resource.Ident.RawResourceHandle);
		}
		AddResource(handle, data, new SerializedResourceData(resource.AsStub, typeof(TResource).TypeHandle.Value, &BoxResourceViaStub<TResource>), resource);
	}

	static object BoxResourceViaStub<TResource>(ResourceStub stub) where TResource : IResource {
		return TResource.BoxFromStub(stub);
	}

	public void SetDoNotDisposeFlag<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource {
		var data = ValidateCanAddAndGetData(handle);
		var stub = resource.AsStub;
		for (var i = 0; i < data.Count; ++i) {
			if (data.DataArray[i].Stub != stub) continue;
			data.DataArray[i] = data.DataArray[i] with { DoNotDispose = true };
			return;
		}
		throw new ArgumentException($"{resource} is not a member of {nameof(ResourceGroup)} '{_globals.GetResourceName(handle.Ident, DefaultGroupName)}'.", nameof(resource));
	}

	void ThrowIfAdditionWouldCreateCycle(ResourceHandle<ResourceGroup> targetGroup, ResourceHandle<ResourceGroup> addedGroup) {
		if (addedGroup == targetGroup) {
			throw new InvalidOperationException($"Can not add {nameof(ResourceGroup)} '{_globals.GetResourceName(targetGroup.Ident, DefaultGroupName)}' to itself.");
		}

		try {
			_cycleCheckStack.Add(addedGroup);
			_cycleCheckVisited.Add(addedGroup);
			for (var cursor = 0; cursor < _cycleCheckStack.Count; ++cursor) {
				if (!_dataMap.TryGetValue(_cycleCheckStack[cursor], out var groupData)) continue;

				for (var i = 0; i < groupData.Count; ++i) {
					var stub = groupData.DataArray[i].Stub;
					if (stub.TypeHandle != ResourceHandle<ResourceGroup>.TypeHandle || !ReferenceEquals(stub.Implementation, this)) continue;
					var nestedGroup = (ResourceHandle<ResourceGroup>) stub.Handle;
					if (nestedGroup == targetGroup) {
						throw new InvalidOperationException(
							$"Can not add {nameof(ResourceGroup)} '{_globals.GetResourceName(addedGroup.Ident, DefaultGroupName)}' to {nameof(ResourceGroup)} " +
							$"'{_globals.GetResourceName(targetGroup.Ident, DefaultGroupName)}' because it already contains '{_globals.GetResourceName(targetGroup.Ident, DefaultGroupName)}' " +
							$"(directly or via other nested groups); this would create a cyclical dependency."
						);
					}
					if (_cycleCheckVisited.Add(nestedGroup)) _cycleCheckStack.Add(nestedGroup);
				}
			}
		}
		finally {
			for (var i = 0; i < _cycleCheckStack.Count; ++i) _cycleCheckVisited.Remove(_cycleCheckStack[i]);
			_cycleCheckStack.ClearWithoutZeroingMemory();
		}
	}

	GroupData ValidateCanAddAndGetData(ResourceHandle<ResourceGroup> handle) {
		var data = GetDataForHandleOrThrow(handle);
		data.ThrowIfSealed(_globals.GetResourceName(handle.Ident, DefaultGroupName));
		return data;
	}

	void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, GroupData data, SerializedResourceData serializedData, TResource underlyingResource) where TResource : IResource {
		if (data.Count == data.DataArray.Length) {
			var newArray = _dataArrayPool.Rent(data.Count * 2);
			data.DataArray.CopyTo(newArray.AsSpan());
			_dataArrayPool.Return(data.DataArray, clearArray: true);
			data = data with { DataArray = newArray };
		}
		data.DataArray[data.Count] = serializedData;
		_dataMap[handle] = data with { Count = data.Count + 1 };
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), underlyingResource);
	}

	#region Resource Enumeration
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceHandle<ResourceGroup> handle) where TResource : IResource<TResource> {
		return new IndirectEnumerable<EnumerationInput, TResource>(
			new(this, handle, typeof(TResource).TypeHandle.Value),
			GetDataForHandleOrThrow(handle).Count,
			&GetEnumeratorResourceCount<TResource>,
			&GetEnumeratorStateVersion,
			&GetEnumeratorResourceAtIndex<TResource>
		);
	}
	static int GetEnumeratorStateVersion(EnumerationInput input) => (input.Impl as LocalResourceGroupImplProvider)!.GetDataForHandleOrThrow(input.Handle).Count;
	static int GetEnumeratorResourceCount<TResource>(EnumerationInput input) where TResource : IResource<TResource> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);
		if (TryGetIndexedRange(data, TypeKey<TResource>.Value, out var indices)) return indices.Length;

		var result = 0;
		for (var i = 0; i < data.Count; ++i) {
			if (data.DataArray[i].AddedAsTypeHandle == input.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorResourceAtIndex<TResource>(EnumerationInput input, int index) where TResource : IResource<TResource> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);
		if (TryGetIndexedRange(data, TypeKey<TResource>.Value, out var indices)) {
			if ((uint) index < (uint) indices.Length) return TResource.CreateFromStub(data.DataArray[indices[index]].Stub);
			throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {indices.Length}).");
		}

		var count = 0;
		for (var i = 0; i < data.Count; ++i) {
			var entry = data.DataArray[i];
			if (entry.AddedAsTypeHandle != input.ResourceTypeHandle) continue;
			if (count == index) return TResource.CreateFromStub(entry.Stub);
			++count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource> {
		return GetEnumeratorResourceAtIndex<TResource>(new EnumerationInput(this, handle, typeof(TResource).TypeHandle.Value), index);
	}
	#endregion

	public IReadOnlyCollection<object> GetAllResourcesBoxed(ResourceHandle<ResourceGroup> handle) {
		var data = GetDataForHandleOrThrow(handle);

		var result = new List<object>(data.Count);
		for (var i = 0; i < data.Count; ++i) {
			var d = data.DataArray[i];
			result.Add(d.ResourceBoxingStub(d.Stub));
		}
		return result;
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
		Dispose(handle, data, data.DisposeContainedResourcesWhenDisposed);
	}
	public void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources) {
		if (!_dataMap.TryGetValue(handle, out var data)) return;
		Dispose(handle, data, disposeContainedResources);
	}
	void Dispose(ResourceHandle<ResourceGroup> handle, GroupData groupData, bool disposeContainedResources) {
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var dataArray = groupData.DataArray;
		var count = groupData.Count;

		var stubPool = TinyFfrArrayPool<ResourceStub>.Shared;
		var intPool = TinyFfrArrayPool<int>.Shared;
		ResourceStub[]? disposalTargets = null;
		int[]? disposalEntryIndices = null;
		int[]? disposalOrder = null;
		var disposalCount = 0;
		try {
			if (disposeContainedResources) {
				disposalTargets = stubPool.Rent(count);
				disposalEntryIndices = intPool.Rent(count);
				disposalOrder = intPool.Rent(count);
				for (var i = 0; i < count; ++i) {
					var data = dataArray[i];
					if (data.DoNotDispose) continue;
					disposalTargets[disposalCount] = data.Stub;
					disposalEntryIndices[disposalCount] = i;
					++disposalCount;
				}

				if (!_globals.DependencyTracker.TryGetDisposalOrder(disposalTargets.AsSpan(0, disposalCount), handle.Ident, disposalOrder, out var failure)) {
					throw ResourceDependencyException.CreateForGroupDisposal(
						_globals.GetResourceName(handle.Ident, DefaultGroupName).ToString(),
						failure.BlockedResource.GetNameAsNewStringObject(),
						failure.BlockingDependent?.GetNameAsNewStringObject()
					);
				}
			}

			for (var i = count - 1; i >= 0; --i) {
				var data = dataArray[i];
				_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Stub);
			}

			for (var i = 0; i < disposalCount; ++i) {
				dataArray[disposalEntryIndices![disposalOrder![i]]].Stub.Dispose();
			}
		}
		finally {
			if (disposalTargets != null) stubPool.Return(disposalTargets, clearArray: true);
			if (disposalEntryIndices != null) intPool.Return(disposalEntryIndices);
			if (disposalOrder != null) intPool.Return(disposalOrder);
		}

		if (groupData.TypeIndex is { } typeIndex) _typeIndexPool.Return(typeIndex);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		_dataMap.Remove(handle);
		_dataArrayPool.Return(dataArray, clearArray: true);
	}
	public bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceHandle<ResourceGroup> handle) => GetDataForHandleOrThrow(handle).DisposeContainedResourcesWhenDisposed;
	public void Dispose() {
		if (_isDisposed) return;
		for (var i = 0; i < _dataMap.Count; ++i) {
			var data = _dataMap.GetPairAtIndex(i).Value;
			if (data.TypeIndex is { } typeIndex) _typeIndexPool.Return(typeIndex);
			_dataArrayPool.Return(data.DataArray, clearArray: true);
		}
		_dataMap.Dispose();
		_cycleCheckStack.Dispose();
		_cycleCheckVisited.Dispose();
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