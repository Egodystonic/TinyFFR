// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed class LocalFactoryGlobalObjectGroup {
	readonly ArrayPoolBackedMap<ResourceIdent, ManagedStringPool.RentedStringHandle> _resourceNameMap = new();
	readonly DeferredRef<LocalCombinedResourceGroupImplProvider> _resourceGroupProvider;
	readonly LocalRendererFactory _factory;

	public IResourceDependencyTracker DependencyTracker { get; }
	public ManagedStringPool StringPool { get; }
	public LocalCombinedResourceGroupImplProvider ResourceGroupProvider => _resourceGroupProvider;

	public LocalFactoryGlobalObjectGroup(LocalRendererFactory factory, IResourceDependencyTracker dependencyTracker, ManagedStringPool stringPool, DeferredRef<LocalCombinedResourceGroupImplProvider> resourceGroupProviderRef) {
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(dependencyTracker);
		ArgumentNullException.ThrowIfNull(stringPool);
		ArgumentNullException.ThrowIfNull(resourceGroupProviderRef);

		_factory = factory;
		DependencyTracker = dependencyTracker;
		StringPool = stringPool;
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

	public TemporaryLoadSpaceBuffer CopySpanToTemporaryCpuBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged => LocalNativeUtils.CopySpanToTemporaryCpuBuffer(_factory, data);
}