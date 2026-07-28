// Created on 2026-07-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class ArrayPoolBackedLruCache<TKey, TValue> : IArrayPoolBackedLruCache<TKey, TValue> {
	const int NullIndex = -1;

	struct Node {
		public TKey Key;
		public TValue Value;
		public int NextMoreRecentNodeIndex;
		public int NextLessRecentNodeIndex;
	}

	readonly Node[] _nodes;
	readonly ArrayPoolBackedMap<TKey, int> _liveNodeIndexLookupMap = new();
	readonly int _maxValuesInCache;
	readonly delegate* managed<object?, TKey, TValue, void> _cacheEvictionCallback;
	readonly object? _cacheEvictionCallbackArg;
	int _count;
	int _leastRecentNodeIndex = NullIndex;
	int _mostRecentNodeIndex = NullIndex;

	public ArrayPoolBackedLruCache(int maxValuesInCache) : this(maxValuesInCache, null) { }
	
	public ArrayPoolBackedLruCache(int maxValuesInCache, delegate* managed<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null) {
		if (maxValuesInCache <= 0) throw new ArgumentOutOfRangeException(nameof(maxValuesInCache), maxValuesInCache, "Requires positive value.");
		_maxValuesInCache = maxValuesInCache;
		_nodes = ArrayPool<Node>.Shared.Rent(maxValuesInCache);
		_cacheEvictionCallback = cacheEvictionCallback;
		_cacheEvictionCallbackArg = cacheEvictionCallbackArg;
	}

	public void AddOrSet(TKey key, TValue @value) {
		AddOrSet(key, value, _liveNodeIndexLookupMap.TryGetValue(key, out var liveIndex) ? liveIndex : null);
	}
	
	public bool AddOrSet(TKey key, TValue @value, out TValue previousValue) {
		if (_liveNodeIndexLookupMap.TryGetValue(key, out var liveIndex)) {
			previousValue = _nodes[liveIndex].Value;
			AddOrSet(key, value, liveIndex);
			return true;
		}
		else {
			previousValue = default!;
			AddOrSet(key, value, null);
			return false;
		}
	}
	
	void AddOrSet(TKey key, TValue @value, int? preLookedUpIndex) {
		if (preLookedUpIndex is { } liveIndex) {
			_nodes[liveIndex].Value = @value;
			SetNodeMostRecent(liveIndex);
			return;
		}

		int targetIndex;
		if (_count < _maxValuesInCache) {
			targetIndex = _count++;
			_nodes[targetIndex].NextMoreRecentNodeIndex = NullIndex;
			_nodes[targetIndex].NextLessRecentNodeIndex = NullIndex;
		}
		else {
			targetIndex = _leastRecentNodeIndex;
			if (_cacheEvictionCallback != null) _cacheEvictionCallback(_cacheEvictionCallbackArg, _nodes[targetIndex].Key, _nodes[targetIndex].Value);
			_liveNodeIndexLookupMap.Remove(_nodes[targetIndex].Key);
		}

		ref var node = ref _nodes[targetIndex];
		node.Key = key;
		node.Value = @value;
		_liveNodeIndexLookupMap[key] = targetIndex;
		SetNodeMostRecent(targetIndex);
	}

	public bool TryGet(TKey key, out TValue @value) {
		if (!_liveNodeIndexLookupMap.TryGetValue(key, out var nodeIndex)) {
			@value = default!;
			return false;
		}

		SetNodeMostRecent(nodeIndex);
		@value = _nodes[nodeIndex].Value;
		return true;
	}

	void SetNodeMostRecent(int index) {
		if (index == _mostRecentNodeIndex) return;

		ref var node = ref _nodes[index];

		if (node.NextMoreRecentNodeIndex != NullIndex) _nodes[node.NextMoreRecentNodeIndex].NextLessRecentNodeIndex = node.NextLessRecentNodeIndex;
		if (node.NextLessRecentNodeIndex != NullIndex) _nodes[node.NextLessRecentNodeIndex].NextMoreRecentNodeIndex = node.NextMoreRecentNodeIndex;
		if (index == _leastRecentNodeIndex) _leastRecentNodeIndex = node.NextMoreRecentNodeIndex;

		node.NextMoreRecentNodeIndex = NullIndex;
		node.NextLessRecentNodeIndex = _mostRecentNodeIndex;
		if (_mostRecentNodeIndex != NullIndex) _nodes[_mostRecentNodeIndex].NextMoreRecentNodeIndex = index;
		_mostRecentNodeIndex = index;
		if (_leastRecentNodeIndex == NullIndex) _leastRecentNodeIndex = index;
	}

	void IDisposable.Dispose() => Dispose(invokeCacheEvictionCallbackOnAllContainedValues: true);
	
	public void Clear(bool invokeCacheEvictionCallbackOnAllContainedValues) {
		if (invokeCacheEvictionCallbackOnAllContainedValues && _cacheEvictionCallback != null) {
			for (var i = 0; i < _count; ++i) {
				_cacheEvictionCallback(_cacheEvictionCallbackArg, _nodes[i].Key, _nodes[i].Value);
			}
		}
		
		_count = 0;
		_leastRecentNodeIndex = NullIndex;
		_mostRecentNodeIndex = NullIndex;
		_liveNodeIndexLookupMap.Clear();
	}
	
	public void Dispose(bool invokeCacheEvictionCallbackOnAllContainedValues) {
		if (invokeCacheEvictionCallbackOnAllContainedValues && _cacheEvictionCallback != null) {
			for (var i = 0; i < _count; ++i) {
				_cacheEvictionCallback(_cacheEvictionCallbackArg, _nodes[i].Key, _nodes[i].Value);
			}
		}
		
		ArrayPool<Node>.Shared.Return(_nodes, clearArray: true);
		_liveNodeIndexLookupMap.Dispose();
	}
}
