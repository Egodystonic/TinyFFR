// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class ObjectPool<T> {
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
}

sealed unsafe class ObjectPool<T, TArg> {
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
}