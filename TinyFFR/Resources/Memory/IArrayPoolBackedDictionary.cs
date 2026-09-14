// Created on 2025-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="IDictionary{TKey, TValue}"/> whose underlying heap storage is pooled, reducing GC churn.
/// </summary>
/// <typeparam name="TKey">The dictionary key.</typeparam>
/// <typeparam name="TValue">The dictionary value.</typeparam>
public interface IArrayPoolBackedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable;