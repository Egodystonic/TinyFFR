// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory;

public interface IResourceAllocator {
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity);

	Memory<T> CreatePooledMemoryBuffer<T>(int numElements);
	void ReturnPooledMemoryBuffer<T>(Memory<T> buffer);

	IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null);
	IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>();
	IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>();
	IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache);
	unsafe IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate* managed<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null);
	
	// TODO xmldoc make it clear that these two are a way of sharing TinyFFR's worker thread pool, NOT a way of making
	// TODO xmldoc currently-non-async library functionality suddenly async .. everything that can be made async in TinyFFR
	// TODO xmldoc already is
	TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, Func<TContext?, TResult?> work) where TContext : class where TResult : class;
	unsafe TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, delegate* managed<TContext?, TResult?> work) where TContext : class where TResult : class;
}