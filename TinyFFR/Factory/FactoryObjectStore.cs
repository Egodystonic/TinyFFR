// Created on 2024-01-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Factory;

sealed class FactoryObjectStore<T, TConfig> where TConfig : notnull {
	readonly Dictionary<TConfig, T> _store = new();

	public bool ContainsObjectForConfig(TConfig config) => _store.ContainsKey(config);
	public T GetObject(TConfig config) => _store[config];
	public void SetObject(TConfig config, T obj) => _store[config] = obj;
}

sealed class FactoryObjectStore<T> {
	readonly HashSet<T> _store = new();

	public bool ContainsObject<TDerived>() where TDerived : T => _store.OfType<TDerived>().Any();
	public TDerived GetObject<TDerived>() => _store.OfType<TDerived>().First();
	public void SetObject(T obj) => _store.Add(obj);
}