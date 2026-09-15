// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// A fixed-capacity, pooled key/value cache that automatically evicts its least-recently-used entry whenever it is full and a new key is added.
/// </summary>
/// <remarks>
/// "Used" includes both adding/updating a key and successfully retrieving one via <see cref="TryGet"/>. An optional eviction callback (supplied when the cache is created) can be
/// invoked whenever a value leaves the cache, whether due to capacity-driven eviction, an explicit <see cref="Remove(TKey)"/>, or a <see cref="Clear"/>/<see cref="Dispose(bool)"/> that requests it.
/// </remarks>
/// <typeparam name="TKey">The cache key.</typeparam>
/// <typeparam name="TValue">The cache value.</typeparam>
public interface IArrayPoolBackedLruCache<in TKey, TValue> : IDisposable {
	/// <summary>
	/// Adds <paramref name="value"/> under <paramref name="key"/>, overwriting any existing value for that key, and marking it as the most-recently-used entry.
	/// </summary>
	/// <remarks>
	/// If the cache is already at capacity and <paramref name="key"/> is not already present, the least-recently-used entry is evicted to make room.
	/// </remarks>
	/// <param name="key">The key to add or update.</param>
	/// <param name="value">The value to store.</param>
	void AddOrSet(TKey key, TValue @value);
	/// <summary>
	/// Adds <paramref name="value"/> under <paramref name="key"/>, overwriting and outputting any existing value for that key, and marking it as the most-recently-used entry.
	/// </summary>
	/// <remarks>
	/// If the cache is already at capacity and <paramref name="key"/> is not already present, the least-recently-used entry is evicted to make room.
	/// </remarks>
	/// <param name="key">The key to add or update.</param>
	/// <param name="value">The value to store.</param>
	/// <param name="previousValue">Set to the value previously stored under <paramref name="key"/> if this method returns <see langword="true"/>; otherwise set to <see langword="default"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="key"/> already had a value (now replaced and returned via <paramref name="previousValue"/>); <see langword="false"/> if <paramref name="key"/> is newly added.</returns>
	bool AddOrSet(TKey key, TValue @value, out TValue previousValue);
	/// <summary>
	/// Attempts to retrieve the value stored under <paramref name="key"/>, marking it as the most-recently-used entry if found.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <param name="value">Set to the value stored under <paramref name="key"/> if this method returns <see langword="true"/>; otherwise set to <see langword="default"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="key"/> was found; <see langword="false"/> otherwise.</returns>
	bool TryGet(TKey key, out TValue @value);
	/// <summary>
	/// Removes the entry stored under <paramref name="key"/>, if present.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns><see langword="true"/> if <paramref name="key"/> was found (and removed); <see langword="false"/> otherwise.</returns>
	bool Remove(TKey key);
	/// <summary>
	/// Removes the entry stored under <paramref name="key"/>, if present, and outputs the value that was removed.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <param name="value">Set to the value that was stored under <paramref name="key"/> if this method returns <see langword="true"/>; otherwise set to <see langword="default"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="key"/> was found (and removed); <see langword="false"/> otherwise.</returns>
	bool Remove(TKey key, out TValue @value);
	/// <summary>
	/// Removes every entry from the cache.
	/// </summary>
	/// <param name="invokeCacheEvictionCallbackOnAllContainedValues">Whether to invoke the cache's eviction callback (if any) for every value that was in the cache.</param>
	void Clear(bool invokeCacheEvictionCallbackOnAllContainedValues);
	/// <summary>
	/// Disposes this cache, releasing its pooled storage back to the shared pool.
	/// </summary>
	/// <param name="invokeCacheEvictionCallbackOnAllContainedValues">Whether to invoke the cache's eviction callback (if any) for every value that was still in the cache.</param>
	void Dispose(bool invokeCacheEvictionCallbackOnAllContainedValues);
}
