// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

unsafe abstract class ArrayPoolBackedStringKeyMap {
	protected static readonly ManagedStringPool StringPool = new();
	protected static readonly VectorPool<ManagedStringPool.RentedStringHandle> StringHandleVectorPool = new(zeroMemoryOnReturn: true, &CreateVector);
	
	static ArrayPoolBackedVector<ManagedStringPool.RentedStringHandle> CreateVector() => new(initialCapacity: 1);
}

sealed class ArrayPoolBackedStringKeyMap<TValue> : ArrayPoolBackedStringKeyMap, IDisposable {
	readonly ArrayPoolBackedMap<int, ArrayPoolBackedVector<ManagedStringPool.RentedStringHandle>> _hashCodeToHandlesMap = new();
	readonly ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TValue> _handlesToValuesMap = new();

	static int StringToHashCode(ReadOnlySpan<char> str) {
		var hc = new HashCode();
		hc.AddBytes(MemoryMarshal.AsBytes(str));
		return hc.ToHashCode();
	}
	
	ManagedStringPool.RentedStringHandle? FindMatchingStringHandle(ReadOnlySpan<char> str) {
		if (!_hashCodeToHandlesMap.TryGetValue(StringToHashCode(str), out var handlesVector)) return null;
		else return FindMatchingStringHandle(handlesVector, str);
	}
	
	ManagedStringPool.RentedStringHandle? FindMatchingStringHandle(ArrayPoolBackedVector<ManagedStringPool.RentedStringHandle> handlesVector, ReadOnlySpan<char> str) {
		for (var i = 0; i < handlesVector.Count; ++i) {
			if (handlesVector[i].AsSpan.SequenceEqual(str)) return handlesVector[i];
		}
		return null;
	}
	
	public int Count => _handlesToValuesMap.Count;

	public TValue this[ReadOnlySpan<char> key] {
		get => _handlesToValuesMap[FindMatchingStringHandle(key) ?? throw new KeyNotFoundException()];
		set {
			var matchingHandle = FindMatchingStringHandle(key);
			if (matchingHandle != null) _handlesToValuesMap[matchingHandle.Value] = value;
			else Add(key, value);
		}
	}
	
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TValue>.KeyEnumerator Keys => _handlesToValuesMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TValue>.ValueEnumerator Values => _handlesToValuesMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TValue>.Enumerator GetEnumerator() => _handlesToValuesMap.GetEnumerator();

	public void Add(ReadOnlySpan<char> key, TValue value) {
		ManagedStringPool.RentedStringHandle handle;
		
		var hashCode = StringToHashCode(key);
		if (_hashCodeToHandlesMap.TryGetValue(hashCode, out var handlesVector)) {
			if (FindMatchingStringHandle(handlesVector, key).HasValue) throw new InvalidOperationException($"Key '{key}' already exists.");
			handle = StringPool.RentAndCopy(key);
			handlesVector.Add(handle);
		}
		else {
			handle = StringPool.RentAndCopy(key);
			var vector = StringHandleVectorPool.Rent();
			vector.Add(handle);
			_hashCodeToHandlesMap.Add(hashCode, vector);
		}
		
		_handlesToValuesMap.Add(handle, value);
	}
	
	public void Clear() {
		foreach (var key in _handlesToValuesMap.Keys) StringPool.Return(key);
		foreach (var value in _hashCodeToHandlesMap.Values) StringHandleVectorPool.Return(value);
		_handlesToValuesMap.Clear();
		_hashCodeToHandlesMap.Clear();
	}
	
	public bool ContainsKey(ReadOnlySpan<char> key) => FindMatchingStringHandle(key).HasValue;

	public bool TryGetValue(ReadOnlySpan<char> key, out TValue value) {
		var handle = FindMatchingStringHandle(key);
		
		if (handle.HasValue) {
			value = _handlesToValuesMap[handle.Value];
			return true;
		}
		
		value = default!;
		return false;
	}
	
	public KeyValuePair<ManagedStringPool.RentedStringHandle, TValue> GetPairAtIndex(int index) => _handlesToValuesMap.GetPairAtIndex(index);
	
	public bool Remove(ReadOnlySpan<char> key) {
		var handle = FindMatchingStringHandle(key);
		if (handle is not { } h) return false;
		
		_handlesToValuesMap.Remove(h);
		var hashCode = StringToHashCode(key);
		
		var handlesVector = _hashCodeToHandlesMap[hashCode];
		if (handlesVector.Count == 1) {
			_hashCodeToHandlesMap.Remove(hashCode);
			StringHandleVectorPool.Return(handlesVector);
		}
		else {
			handlesVector.Remove(h);
		}
		return true;
	}
	
	public void Dispose() {
		Clear();
		_hashCodeToHandlesMap.Dispose();
		_handlesToValuesMap.Dispose();
	}
}