// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

public interface IArrayPoolBackedLruCache<in TKey, TValue> : IDisposable {
	void AddOrSet(TKey key, TValue @value);
	bool AddOrSet(TKey key, TValue @value, out TValue previousValue);
	bool TryGet(TKey key, out TValue @value);
	void Clear(bool invokeCacheEvictionCallbackOnAllContainedValues);
	void Dispose(bool invokeCacheEvictionCallbackOnAllContainedValues);
}
