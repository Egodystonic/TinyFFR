// Created on 2024-01-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Factory;

sealed class FactoryObjectStore<TConfig, T> where TConfig : notnull {
	readonly Dictionary<TConfig, T> _store = new();

	public bool ContainsObjectForConfig(TConfig config) => _store.ContainsKey(config);
	public T GetObjectForConfig(TConfig config) => _store[config];
	public void SetObjectForConfig(TConfig config, T obj) => _store[config] = obj;

	public void DisposeAll() {
		foreach (var obj in _store.Values) {
			if (obj is IDisposable disposable) disposable.Dispose();
		}
	}
}

sealed class FactoryObjectStore<T> {
	readonly HashSet<T> _store = new();

	public bool ContainsObjectType<TDerived>() where TDerived : T => _store.OfType<TDerived>().Any();
	public TDerived GetObjectOfType<TDerived>() => _store.OfType<TDerived>().First();
	public void SetObjectOfType(T obj) => _store.Add(obj);

	public void DisposeAll() {
		foreach (var obj in _store) {
			if (obj is IDisposable disposable) disposable.Dispose();
		}
	}
}