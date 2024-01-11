// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Memory;

sealed class TffrObjectPool<T, TInitParams> where T : class, IPoolable<T, TInitParams> where TInitParams : struct {
	const int InitialArrayLength = 4;
	object[] _curArray = TffrMemoryManager.SwapPooledObjectArray(InitialArrayLength, null);
	int _nextReturnIndex = 0;

	public T GetOne(in TInitParams initParams) {
		T result;
		if (_nextReturnIndex == 0) result = T.InstantiateNew();
		else result = (T) _curArray[_nextReturnIndex--];
		if (TffrInitializer.InitOptions.TrackResourceLeaks) TffrMemoryManager.AddTrackedObject(result);
		result.Reinitialize(initParams);
		return result;
	}

	public void ReturnOne(T item) {
		if (_nextReturnIndex == _curArray.Length) _curArray = TffrMemoryManager.SwapPooledObjectArray(_curArray.Length * 2, _curArray);
		_curArray[_nextReturnIndex++] = item;
		if (TffrInitializer.InitOptions.TrackResourceLeaks) TffrMemoryManager.RemoveTrackedObject(item);
	}
}