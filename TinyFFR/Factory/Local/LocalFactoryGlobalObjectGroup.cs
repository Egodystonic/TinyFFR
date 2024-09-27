// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed class LocalFactoryGlobalObjectGroup {
	readonly DeferredRef<LocalCombinedResourceGroupImplProvider> _resourceGroupProvider;

	public IResourceDependencyTracker DependencyTracker { get; }
	public ManagedStringPool StringPool { get; }
	public LocalCombinedResourceGroupImplProvider ResourceGroupProvider => _resourceGroupProvider;

	public LocalFactoryGlobalObjectGroup(IResourceDependencyTracker dependencyTracker, ManagedStringPool stringPool, DeferredRef<LocalCombinedResourceGroupImplProvider> resourceGroupProviderRef) {
		ArgumentNullException.ThrowIfNull(dependencyTracker);
		ArgumentNullException.ThrowIfNull(stringPool);
		ArgumentNullException.ThrowIfNull(resourceGroupProviderRef);
		
		DependencyTracker = dependencyTracker;
		StringPool = stringPool;
		_resourceGroupProvider = resourceGroupProviderRef;
	}
}