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
	public IntPtr SpecializationTypeIdentifier { get; init; }
	public PooledHeapMemory<byte> SpecializationData { get; init; }
	public ResourceStub? AdditionalResourceRef { get; init; }
	public bool DoNotDispose { get; init; }
	public delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, void> SpecializedResourceDisposalStub { get; init; }
	public delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, object> ResourceBoxingStub { get; init; }
	public bool IsSpecialized => SpecializationTypeIdentifier != default;
	public SerializedResourceData(ResourceStub stub, IntPtr specializationTypeIdentifier, PooledHeapMemory<byte> specializationData, ResourceStub? additionalResourceRef, delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, void> specializedResourceDisposalStub, delegate* managed<ResourceStub, ReadOnlySpan<byte>, ResourceStub?, object> resourceBoxingStub) {
		Stub = stub;
		SpecializationTypeIdentifier = specializationTypeIdentifier;
		SpecializationData = specializationData;
		AdditionalResourceRef = additionalResourceRef;
		SpecializedResourceDisposalStub = specializedResourceDisposalStub;
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
		typeof(DirectionalLight).TypeHandle.Value,
		typeof(Display).TypeHandle.Value,
		typeof(Font).TypeHandle.Value,
		typeof(Material).TypeHandle.Value,
		typeof(Mesh).TypeHandle.Value,
		typeof(MeshGroupAnimationTable).TypeHandle.Value,
		typeof(MeshAnimation).TypeHandle.Value,
		typeof(MeshNode).TypeHandle.Value,
		typeof(Model).TypeHandle.Value,
		typeof(ModelInstance).TypeHandle.Value,
		typeof(PointLight).TypeHandle.Value,
		typeof(Renderer).TypeHandle.Value,
		typeof(RendererCompositor).TypeHandle.Value,
		typeof(RenderOutputBuffer).TypeHandle.Value,
		typeof(ResourceGroup).TypeHandle.Value,
		typeof(Scene).TypeHandle.Value,
		typeof(SpotLight).TypeHandle.Value,
		typeof(Texture).TypeHandle.Value,
		typeof(Window).TypeHandle.Value,
	];
	static readonly int TypeIndexHeaderLength = IndexedTypeHandles.Length * 2;

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<SerializedResourceData> _dataArrayPool = TinyFfrArrayPool<SerializedResourceData>.Shared;
	readonly ArrayPool<int> _typeIndexPool = TinyFfrArrayPool<int>.Shared;
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
			var key = GetTypeKey(data.DataArray[i].Stub.TypeHandle);
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
			var key = GetTypeKey(data.DataArray[i].Stub.TypeHandle);
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
		AddResource(handle, data, new SerializedResourceData(resource.AsStub, default, default, default, null, &BoxResourceViaStub<TResource>), resource);
	}

	static object BoxResourceViaStub<TResource>(ResourceStub stub, ReadOnlySpan<byte> specializationData, ResourceStub? additionalResourceRef) where TResource : IResource {
		return TResource.BoxFromStub(stub);
	}

	public void AddResource<TResource, TBase>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		var data = ValidateCanAddAndGetData(handle);
		var specializationDataBuffer = _globals.HeapPool.Borrow(resource.SpecializationDataLength);
		TResource.Smuggle(resource, specializationDataBuffer.Span, out var underlyingResource, out var additionalResourceRef);
		AddResource(
			handle,
			data,
			new(underlyingResource.AsStub, TResource.SpecializationTypeIdentifier, specializationDataBuffer, additionalResourceRef, &IResourceSpecialization<TResource, TBase>.DisposeViaStub, &IResourceSpecialization<TResource, TBase>.BoxViaStub),
			underlyingResource
		);
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
		if (serializedData.AdditionalResourceRef is { } additionalResourceRef) {
			_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), additionalResourceRef);
		}
	}

	#region Standard Resource Enumeration
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
			if (data.DataArray[i].Stub.TypeHandle == input.ResourceTypeHandle) ++result;
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
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return new IndirectEnumerable<EnumerationInput, TResource>(
			new(this, handle, (nint) TResource.SpecializationTypeIdentifier),
			GetDataForHandleOrThrow(handle).Count,
			&GetEnumeratorSpecializedResourceCount<TResource, TBase>,
			&GetEnumeratorStateVersion,
			&GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>
		);
	}
	static int GetEnumeratorSpecializedResourceCount<TResource, TBase>(EnumerationInput input) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var result = 0;
		if (TryGetIndexedRange(data, TypeKey<TBase>.Value, out var indices)) {
			for (var i = 0; i < indices.Length; ++i) {
				if (data.DataArray[indices[i]].SpecializationTypeIdentifier == input.ResourceTypeHandle) ++result;
			}
			return result;
		}
		for (var i = 0; i < data.Count; ++i) {
			if (data.DataArray[i].SpecializationTypeIdentifier == input.ResourceTypeHandle) ++result;
		}
		return result;
	}
	static TResource GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>(EnumerationInput input, int index) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		var implProvider = (input.Impl as LocalResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalResourceGroupImplProvider)}.");
		var data = implProvider.GetDataForHandleOrThrow(input.Handle);

		var count = 0;
		if (TryGetIndexedRange(data, TypeKey<TBase>.Value, out var indices)) {
			for (var i = 0; i < indices.Length; ++i) {
				ref readonly var d = ref data.DataArray[indices[i]];
				if (d.SpecializationTypeIdentifier != input.ResourceTypeHandle) continue;
				if (count == index) return TResource.DeSmuggle(TBase.CreateFromStub(d.Stub), d.SpecializationData.Span, d.AdditionalResourceRef);
				++count;
			}
			throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
		}

		for (var i = 0; i < data.Count; ++i) {
			var d = data.DataArray[i];
			if (d.SpecializationTypeIdentifier != input.ResourceTypeHandle) continue;
			if (count == index) return TResource.DeSmuggle(TBase.CreateFromStub(d.Stub), d.SpecializationData.Span, d.AdditionalResourceRef);
			++count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), $"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}
	public TResource GetNthResourceOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle, int index) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return GetEnumeratorSpecializedResourceAtIndex<TResource, TBase>(new EnumerationInput(this, handle, (nint) TResource.SpecializationTypeIdentifier), index);
	}
	#endregion

	public IReadOnlyCollection<object> GetAllResourcesBoxed(ResourceHandle<ResourceGroup> handle) {
		var data = GetDataForHandleOrThrow(handle);

		var result = new List<object>(data.Count);
		for (var i = 0; i < data.Count; ++i) {
			var d = data.DataArray[i];
			result.Add(d.ResourceBoxingStub(d.Stub, d.SpecializationData.Span, d.AdditionalResourceRef));
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
		// Maintainer's note: Reverse order of disposal is important to help dispose items in the correct order according to their dependency chains
		// This doesn't guarantee anything of course, but makes it more likely that thoughtless use of this type will work okay
		for (var i = count - 1; i >= 0; --i) {
			var data = dataArray[i];
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Stub);
			if (data.AdditionalResourceRef is { } additionalResourceRef) {
				_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), additionalResourceRef);
			}

			if (disposeContainedResources && !data.DoNotDispose) {
				if (data.IsSpecialized) {
					if (data.SpecializedResourceDisposalStub != null) {
						data.SpecializedResourceDisposalStub(data.Stub, data.SpecializationData.Span, data.AdditionalResourceRef);
					}
				}
				else data.Stub.Dispose();
			}

			if (data.IsSpecialized) data.SpecializationData.Dispose();
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
			for (var j = 0; j < data.Count; ++j) {
				if (data.DataArray[j].IsSpecialized) data.DataArray[j].SpecializationData.Dispose();
			}
			if (data.TypeIndex is { } typeIndex) _typeIndexPool.Return(typeIndex);
			_dataArrayPool.Return(data.DataArray, clearArray: true);
		}
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