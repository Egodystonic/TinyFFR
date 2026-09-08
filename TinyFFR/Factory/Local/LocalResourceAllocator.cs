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

sealed unsafe class LocalResourceAllocator : IResourceAllocator, IDisposable {
	readonly record struct ArrayPoolLeaseData(object ArrayPool, object Array, bool ClearMemory);
	readonly ArrayPoolBackedMap<Type, object> _arrayPools = new();
	readonly ArrayPoolBackedMap<nuint, ArrayPoolLeaseData> _arrayPoolLeases = new();
	readonly ArrayPoolBackedMap<Type, ArrayPoolBackedMap<int, object>> _sharedScratchCollections = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	nuint _prevLeaseId = 0U;
	bool _isDisposed = false;

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
	
	ArrayPool<T> GetArrayPool<T>() {
		if (_arrayPools.TryGetValue(typeof(T), out var arrayPoolAsObj)) return (ArrayPool<T>) arrayPoolAsObj;
		
		var result = TinyFfrArrayPool<T>.Shared;
		_arrayPools.Add(typeof(T), result);
		return result;
	}

	public ScopedSpanLease<T> BorrowSpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true) {
		static void DisposeLease(object? arg, nuint leaseId) {
			var @this = (LocalResourceAllocator) arg!;
			if (!@this._arrayPoolLeases.Remove(leaseId, out var leaseDataTuple)) return;
			((ArrayPool<T>) leaseDataTuple.ArrayPool).Return((T[]) leaseDataTuple.Array, clearArray: leaseDataTuple.ClearMemory);
		}
		
		var leaseId = ++_prevLeaseId;
		var arrayPool = GetArrayPool<T>();
		var array = arrayPool.Rent(numElements);
		_arrayPoolLeases.Add(leaseId, new(arrayPool, array, clearMemoryOnLeaseEnd)); 
		
		return new ScopedSpanLease<T>(&DisposeLease, this, leaseId, array.AsSpan()[..numElements]);
	}
	public ScopedReadOnlySpanLease<T> BorrowReadOnlySpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true) => BorrowSpan<T>(numElements, clearMemoryOnLeaseEnd);

	public Memory<T> CreatePooledMemoryBuffer<T>(int numElements) {
		var arrayPool = GetArrayPool<T>();
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

	public IList<T> GetSharedScratchList<T>(int bufferIndex = 0, bool clearBuffer = true) {
		var bufferMap = GetSharedScratchBufferMap(typeof(IArrayPoolBackedList<T>));
		if (!bufferMap.TryGetValue(bufferIndex, out var bufferAsObj)) {
			bufferAsObj = new ArrayPoolBackedVector<T>();
			bufferMap.Add(bufferIndex, bufferAsObj);
		}
		var result = (IArrayPoolBackedList<T>) bufferAsObj;
		if (clearBuffer) result.Clear();
		return result;
	}
	public IDictionary<TKey, TValue> GetSharedScratchDictionary<TKey, TValue>(int bufferIndex = 0, bool clearBuffer = true) {
		var bufferMap = GetSharedScratchBufferMap(typeof(IArrayPoolBackedDictionary<TKey, TValue>));
		if (!bufferMap.TryGetValue(bufferIndex, out var bufferAsObj)) {
			bufferAsObj = new ArrayPoolBackedMap<TKey, TValue>();
			bufferMap.Add(bufferIndex, bufferAsObj);
		}
		var result = (IArrayPoolBackedDictionary<TKey, TValue>) bufferAsObj;
		if (clearBuffer) result.Clear();
		return result;
	}
	public ISet<T> GetSharedScratchSet<T>(int bufferIndex = 0, bool clearBuffer = true) {
		var bufferMap = GetSharedScratchBufferMap(typeof(IArrayPoolBackedSet<T>));
		if (!bufferMap.TryGetValue(bufferIndex, out var bufferAsObj)) {
			bufferAsObj = new ArrayPoolBackedSet<T>();
			bufferMap.Add(bufferIndex, bufferAsObj);
		}
		var result = (IArrayPoolBackedSet<T>) bufferAsObj;
		if (clearBuffer) result.Clear();
		return result;
	}
	ArrayPoolBackedMap<int, object> GetSharedScratchBufferMap(Type collectionType) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceAllocator));
		if (_sharedScratchCollections.TryGetValue(collectionType, out var existingMap)) return existingMap;
		var result = new ArrayPoolBackedMap<int, object>();
		_sharedScratchCollections.Add(collectionType, result);
		return result;
	}

	public IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null) => new ArrayPoolBackedVector<T>(initialCapacity ?? ArrayPoolBackedVector<T>.DefaultInitialCapacity);
	public IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>() => new ArrayPoolBackedMap<TKey, TValue>();
	public IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>() => new ArrayPoolBackedSet<T>();
	public IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache) => new ArrayPoolBackedLruCache<TKey, TValue>(maxValuesInCache);
	public IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate*<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null) => new ArrayPoolBackedLruCache<TKey, TValue>(maxValuesInCache, cacheEvictionCallback, cacheEvictionCallbackArg);

	public TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, Func<TContext?, TResult?> work) where TContext : class where TResult : class {
		static TResult? Work(Tuple<TContext?, Func<TContext?, TResult?>>? contextAndWorkTuple) {
			return contextAndWorkTuple!.Item2(contextAndWorkTuple.Item1);
		}
		
		// Deliberately using older class-type-based tuple
		var contextAndWorkTuple = new Tuple<TContext?, Func<TContext?, TResult?>>(context, work);
		return DispatchWorkerThreadJob(contextAndWorkTuple, &Work);
	}
	public TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, delegate*<TContext?, TResult?> work) where TContext : class where TResult : class {
		var result = new TinyFfrAsyncOperation<TResult?>(_globals.PrimaryThreadDispatcher);
		_globals.ThreadPoolWorkScheduler.AddWorkerThreadJob(ThreadJob.CreateWithAsyncOpArbitraryResult(context, work, result));
		return result;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			try {
				foreach (var bufferMap in _sharedScratchCollections.Values) {
					try {
						foreach (var buffer in bufferMap.Values) ((IDisposable) buffer).Dispose();
					}
					finally {
						bufferMap.Dispose();
					}
				}
			}
			finally {
				_sharedScratchCollections.Dispose();
			}
		}
		finally {
			try {
				_arrayPools.Dispose();
				_arrayPoolLeases.Dispose();
			}
			finally {
				_isDisposed = true;
			}
		}
	}
}