// Created on 2026-06-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

public readonly unsafe ref struct ScopedSpanLease<T> {
	readonly delegate* managed<object, nuint, void> _disposalCallback;
	readonly object _callbackParam;
	public nuint LeaseId { get; }
	public Span<T> Span { get; }

	public ScopedSpanLease(delegate*<object, UIntPtr, void> disposalCallback, object callbackParam, UIntPtr leaseId, Span<T> span) {
		_callbackParam = callbackParam;
		LeaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}
	
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, LeaseId);
	}
	
	public static implicit operator ScopedReadOnlySpanLease<T>(ScopedSpanLease<T> operand) {
		return new ScopedReadOnlySpanLease<T>(operand._disposalCallback,
			operand._callbackParam, operand.LeaseId, operand.Span);
	}
}

public readonly unsafe ref struct ScopedReadOnlySpanLease<T> {
	readonly delegate* managed<object, nuint, void> _disposalCallback;
	readonly object _callbackParam;
	public nuint LeaseId { get; }
	public ReadOnlySpan<T> Span { get; }

	public ScopedReadOnlySpanLease(delegate*<object, UIntPtr, void> disposalCallback, object callbackParam, UIntPtr leaseId, ReadOnlySpan<T> span) {
		_callbackParam = callbackParam;
		LeaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}
	
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, LeaseId);
	}
}