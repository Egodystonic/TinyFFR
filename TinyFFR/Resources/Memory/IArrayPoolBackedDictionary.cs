// Created on 2025-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources.Memory;

public interface IArrayPoolBackedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable;