// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Memory;

public static class TffrMemoryManager {
	static readonly ArrayPool<object> _arrayPool = ArrayPool<object>.Shared;
	static readonly List<object> _trackedObjects = new();

	internal static object[] SwapPooledObjectArray(int minLength, object[]? old) {
		var result = _arrayPool.Rent(minLength);
		if (old == null) return result;

		Array.Copy(old, 0, result, 0, old.Length);
		_arrayPool.Return(old, clearArray: true);
		return result;
	}

	internal static void AddTrackedObject(object o) => _trackedObjects.Add(o);
	internal static void RemoveTrackedObject(object o) => _trackedObjects.Remove(o);
}