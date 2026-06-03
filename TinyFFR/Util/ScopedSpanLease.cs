// Created on 2026-06-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

public readonly unsafe ref struct ScopedSpanLease<T> {
	readonly object _callbackParam;
	readonly nuint _leaseId;
	readonly delegate* managed<object, nuint, void> _disposalCallback;
	public Span<T> Span { get; }

	public ScopedSpanLease(object callbackParam, nuint leaseId, delegate*<object, nuint, void> disposalCallback, Span<T> span) {
		_callbackParam = callbackParam;
		_leaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}
	
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, _leaseId);
	}
}

public readonly unsafe ref struct ScopedReadOnlySpanLease<T> {
	readonly object _callbackParam;
	readonly nuint _leaseId;
	readonly delegate* managed<object, nuint, void> _disposalCallback;
	public ReadOnlySpan<T> Span { get; }

	public ScopedReadOnlySpanLease(object callbackParam, nuint leaseId, delegate*<object, nuint, void> disposalCallback, ReadOnlySpan<T> span) {
		_callbackParam = callbackParam;
		_leaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}
	
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, _leaseId);
	}
}