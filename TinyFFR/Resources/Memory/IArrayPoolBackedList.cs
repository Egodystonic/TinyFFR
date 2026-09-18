// Created on 2025-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="IList{T}"/> whose underlying heap storage is pooled, reducing GC churn.
/// </summary>
/// <typeparam name="T">The list element type.</typeparam>
public interface IArrayPoolBackedList<T> : IList<T>, IDisposable;