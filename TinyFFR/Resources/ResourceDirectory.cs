// Created on 2026-04-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

sealed class ResourceDirectory : IResourceDirectory, IDisposable {
	readonly ArrayPoolBackedMap<Type, object> _typedDirectoryMap;

	public ResourceDirectory(ArrayPoolBackedMap<Type, object> typedDirectoryMap) {
		ArgumentNullException.ThrowIfNull(typedDirectoryMap);
		_typedDirectoryMap = typedDirectoryMap;
	}

	public void Dispose() => _typedDirectoryMap.Dispose();

	public IResourceDirectory<TResource> GetDirectoryForType<TResource>() where TResource : struct, IResource {
		if (!_typedDirectoryMap.TryGetValue(typeof(TResource), out var obj)) {
			throw new InvalidOperationException($"This resource directory does not support resources of type '{typeof(TResource).Name}'.");
		}
		
		if (obj is not IResourceDirectory<TResource> result) {
			throw new InvalidOperationException($"Resource directory for type {typeof(TResource).Name} was {obj} (type {obj?.GetType().Name ?? "null"})-- this is a bug in TinyFFR.");
		}
		
		return result;
	}
}