// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed class LocalFactoryGlobalObjectGroup {
	readonly ArrayPoolBackedMap<ResourceIdent, ManagedStringPool.RentedStringHandle> _resourceNameMap;
	readonly DeferredRef<LocalResourceGroupImplProvider> _resourceGroupProvider;
	readonly LocalTinyFfrFactory _factory;

	public IResourceDependencyTracker DependencyTracker { get; }
	public ManagedStringPool StringPool { get; }
	public HeapPool HeapPool { get; }
	public LocalResourceGroupImplProvider ResourceGroupProvider => _resourceGroupProvider;

	public LocalFactoryGlobalObjectGroup(LocalTinyFfrFactory factory, ArrayPoolBackedMap<ResourceIdent, ManagedStringPool.RentedStringHandle> resourceNameMap, IResourceDependencyTracker dependencyTracker, ManagedStringPool stringPool, HeapPool heapPool, DeferredRef<LocalResourceGroupImplProvider> resourceGroupProviderRef) {
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(resourceNameMap);
		ArgumentNullException.ThrowIfNull(dependencyTracker);
		ArgumentNullException.ThrowIfNull(stringPool);
		ArgumentNullException.ThrowIfNull(heapPool);
		ArgumentNullException.ThrowIfNull(resourceGroupProviderRef);

		_factory = factory;
		_resourceNameMap = resourceNameMap;
		DependencyTracker = dependencyTracker;
		StringPool = stringPool;
		HeapPool = heapPool;
		_resourceGroupProvider = resourceGroupProviderRef;
	}

	public void StoreResourceNameIfNotDefault(ResourceIdent ident, ReadOnlySpan<char> name) {
		if (name == default) return;
		_resourceNameMap.Add(ident, StringPool.RentAndCopy(name));
	}

	public ReadOnlySpan<char> GetResourceName(ResourceIdent ident, ReadOnlySpan<char> fallback) {
		return _resourceNameMap.TryGetValue(ident, out var handle) ? handle.AsSpan : fallback;
	}

	public int CopyResourceName(ResourceIdent ident, ReadOnlySpan<char> fallback, Span<char> dest) {
		var span = GetResourceName(ident, fallback);
		span.CopyTo(dest);
		return span.Length;
	}

	public int GetResourceNameLength(ResourceIdent ident, ReadOnlySpan<char> fallback) {
		return _resourceNameMap.TryGetValue(ident, out var handle) ? handle.Length : fallback.Length;
	}

	public void DisposeResourceNameIfExists(ResourceIdent ident) => _resourceNameMap.Remove(ident);

	public void ReplaceResourceName(ResourceIdent ident, ReadOnlySpan<char> newName) {
		DisposeResourceNameIfExists(ident);
		StoreResourceNameIfNotDefault(ident, newName);
	}

	public TemporaryLoadSpaceBuffer CreateGpuHoldingBufferAndCopyData<T>(ReadOnlySpan<T> data) where T : unmanaged => LocalNativeUtils.CreateGpuHoldingBufferAndCopyData(_factory, data);
	public TemporaryLoadSpaceBuffer CreateGpuHoldingBuffer<T>(int numElements) where T : unmanaged => LocalNativeUtils.CreateGpuHoldingBuffer<T>(_factory, numElements);
	public TemporaryLoadSpaceBuffer CreateGpuHoldingBuffer(int sizeBytes) => LocalNativeUtils.CreateGpuHoldingBuffer(_factory, sizeBytes);
}