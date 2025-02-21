// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory;

public interface IResourceAllocator {
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity);

	Memory<T> CreatePooledMemoryBuffer<T>(int numElements);
	void ReturnPooledMemoryBuffer<T>(Memory<T> buffer);

	IArrayPoolBackedList<T> CreateNewArrayPoolBackedVector<T>(int? initialCapacity = null);
	IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedMap<TKey, TValue>();
}