// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="ISet{T}"/> whose underlying heap storage is pooled, reducing GC churn.
/// </summary>
/// <typeparam name="T">The set element type.</typeparam>
public interface IArrayPoolBackedSet<T> : ISet<T>, IDisposable;
