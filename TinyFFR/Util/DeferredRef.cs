// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Threading;

namespace Egodystonic.TinyFFR;

sealed class DeferredRef<T> where T : class {
	T? _resolvedValue;

	public T Value => _resolvedValue ?? throw new InvalidOperationException("Reference is not yet resolved.");
	public bool IsResolved => _resolvedValue != null;

	public void Resolve(T reference) {
		ArgumentNullException.ThrowIfNull(reference);

		if (_resolvedValue != null) throw new InvalidOperationException($"Reference has already been resolved.");
		_resolvedValue = reference;
	}

	public static implicit operator T(DeferredRef<T> @this) => @this.Value;
}