// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed class LocalResourceAllocator : IResourceAllocator, IDisposable {
	readonly ArrayPoolBackedMap<Type, object> _arrayPools = new();
	bool _isDisposed = false;
	readonly LocalFactoryGlobalObjectGroup _globals;

	public LocalResourceAllocator(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceAllocator));
		return _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed);
	}
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceAllocator));
		return _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, name);
	}
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceAllocator));
		return _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, initialCapacity);
	}
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceAllocator));
		return _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, name, initialCapacity);
	}

	public Memory<T> CreatePooledMemoryBuffer<T>(int numElements) {
		ArrayPool<T> arrayPool;
		if (_arrayPools.TryGetValue(typeof(T), out var arrayPoolAsObj)) {
			arrayPool = (ArrayPool<T>) arrayPoolAsObj;
		}
		else {
			arrayPool = ArrayPool<T>.Shared;
			_arrayPools.Add(typeof(T), arrayPool);
		}

		var rentedArray = arrayPool.Rent(numElements);
		return rentedArray.AsMemory(0, numElements);
	}
	public void ReturnPooledMemoryBuffer<T>(Memory<T> buffer) {
		T[]? array = null;
		var isValid = 
			_arrayPools.TryGetValue(typeof(T), out var arrayPoolAsObj)
			&& MemoryMarshal.TryGetArray((ReadOnlyMemory<T>) buffer, out var arraySegment)
			&& (array = arraySegment.Array) != null;
		if (!isValid) {
			throw new ArgumentException(
				"Given buffer was not previously rented " +
				"from this resource allocator or the compilation type " +
				"has changed compared to when rented.",
				nameof(buffer)
			);
		}
		((ArrayPool<T>) arrayPoolAsObj).Return(array!, clearArray: true);
	}

	public IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null) => new ArrayPoolBackedVector<T>(initialCapacity ?? ArrayPoolBackedVector<T>.DefaultInitialCapacity);
	public IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>() => new ArrayPoolBackedMap<TKey, TValue>();
	public IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>() => new ArrayPoolBackedSet<T>();
	public IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache) => new ArrayPoolBackedLruCache<TKey, TValue>(maxValuesInCache);
	public unsafe IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate*<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null) => new ArrayPoolBackedLruCache<TKey, TValue>(maxValuesInCache, cacheEvictionCallback, cacheEvictionCallbackArg);

	public unsafe TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, Func<TContext?, TResult?> work) where TContext : class where TResult : class {
		static TResult? Work(Tuple<TContext?, Func<TContext?, TResult?>>? contextAndWorkTuple) {
			return contextAndWorkTuple!.Item2(contextAndWorkTuple.Item1);
		}
		
		// Deliberately using older class-type-based tuple
		var contextAndWorkTuple = new Tuple<TContext?, Func<TContext?, TResult?>>(context, work);
		return DispatchWorkerThreadJob(contextAndWorkTuple, &Work);
	}
	public unsafe TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, delegate*<TContext?, TResult?> work) where TContext : class where TResult : class {
		var result = new TinyFfrAsyncOperation<TResult?>(_globals.PrimaryThreadDispatcher);
		_globals.ThreadPoolWorkScheduler.AddWorkerThreadJob(ThreadJob.CreateWithAsyncOpArbitraryResult(context, work, result));
		return result;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_arrayPools.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}