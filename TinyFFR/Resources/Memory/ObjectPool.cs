// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class ObjectPool<T> : IDisposable {
	readonly delegate* managed<T> _newItemCreationFunc;
	readonly ArrayPoolBackedVector<T> _pool;

	public ObjectPool(delegate*<T> newItemCreationFunc, int initialPoolCount = ArrayPoolBackedVector<T>.DefaultInitialCapacity) {
		if (initialPoolCount < 0) throw new ArgumentOutOfRangeException(nameof(initialPoolCount), initialPoolCount, $"Must be a positive value (or 0).");
		_newItemCreationFunc = newItemCreationFunc;
		_pool = new ArrayPoolBackedVector<T>(Math.Max(initialPoolCount * 2, ArrayPoolBackedVector<T>.DefaultInitialCapacity));
		for (var i = 0; i < initialPoolCount; ++i) _pool.Add(_newItemCreationFunc());
	}

	public T Rent() {
		if (_pool.TryRemoveLast(out var result)) return result;
		return _newItemCreationFunc();
	}

	public void Return(T item) => _pool.Add(item);

	public void Dispose() => _pool.Dispose();
}

sealed unsafe class ObjectPool<T, TArg> : IDisposable {
	readonly delegate* managed<TArg, T> _newItemCreationFunc;
	readonly TArg _arg;
	readonly ArrayPoolBackedVector<T> _pool;

	public ObjectPool(delegate*<TArg, T> newItemCreationFunc, TArg arg, int initialPoolCount = ArrayPoolBackedVector<T>.DefaultInitialCapacity) {
		if (initialPoolCount < 0) throw new ArgumentOutOfRangeException(nameof(initialPoolCount), initialPoolCount, $"Must be a positive value (or 0).");
		_newItemCreationFunc = newItemCreationFunc;
		_pool = new ArrayPoolBackedVector<T>(Math.Max(initialPoolCount * 2, ArrayPoolBackedVector<T>.DefaultInitialCapacity));
		_arg = arg;
		for (var i = 0; i < initialPoolCount; ++i) _pool.Add(_newItemCreationFunc(arg));
	}

	public T Rent() {
		if (_pool.TryRemoveLast(out var result)) return result;
		return _newItemCreationFunc(_arg);
	}

	public void Return(T item) => _pool.Add(item);

	public void Dispose() => _pool.Dispose();
}

sealed unsafe class VectorPool<T> : IDisposable {
	readonly bool _zeroMemoryOnReturn;
	readonly ObjectPool<ArrayPoolBackedVector<T>> _objectPool;

	public VectorPool(bool zeroMemoryOnReturn, int initialPoolCount = ArrayPoolBackedVector<VectorPool<T>>.DefaultInitialCapacity) : this(zeroMemoryOnReturn, &CreateNewVector, initialPoolCount) { }

	public VectorPool(bool zeroMemoryOnReturn, delegate*<ArrayPoolBackedVector<T>> newItemCreationFunc, int initialPoolCount = ArrayPoolBackedVector<VectorPool<T>>.DefaultInitialCapacity) {
		_zeroMemoryOnReturn = zeroMemoryOnReturn;
		_objectPool = new(newItemCreationFunc, initialPoolCount);
	}

	static ArrayPoolBackedVector<T> CreateNewVector() => new();

	public ArrayPoolBackedVector<T> Rent() => _objectPool.Rent();

	public void Return(ArrayPoolBackedVector<T> item) {
		if (_zeroMemoryOnReturn) item.Clear();
		else item.ClearWithoutZeroingMemory();
		_objectPool.Return(item);
	}

	public void Dispose() => _objectPool.Dispose();
}

sealed unsafe class MapPool<TKey, TValue> : IDisposable {
	readonly bool _zeroMemoryOnReturn;
	readonly ObjectPool<ArrayPoolBackedMap<TKey, TValue>> _objectPool;

	public MapPool(bool zeroMemoryOnReturn, int initialPoolCount = ArrayPoolBackedVector<MapPool<TKey, TValue>>.DefaultInitialCapacity) : this(zeroMemoryOnReturn, &CreateNewMap, initialPoolCount) { }

	public MapPool(bool zeroMemoryOnReturn, delegate*<ArrayPoolBackedMap<TKey, TValue>> newItemCreationFunc, int initialPoolCount = ArrayPoolBackedVector<MapPool<TKey, TValue>>.DefaultInitialCapacity) {
		_zeroMemoryOnReturn = zeroMemoryOnReturn;
		_objectPool = new(newItemCreationFunc, initialPoolCount);
	}

	static ArrayPoolBackedMap<TKey, TValue> CreateNewMap() => new();

	public ArrayPoolBackedMap<TKey, TValue> Rent() => _objectPool.Rent();

	public void Return(ArrayPoolBackedMap<TKey, TValue> item) {
		if (_zeroMemoryOnReturn) item.Clear();
		else item.ClearWithoutZeroingMemory();
		_objectPool.Return(item);
	}

	public void Dispose() => _objectPool.Dispose();
}