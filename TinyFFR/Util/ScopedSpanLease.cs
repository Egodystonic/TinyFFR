// Created on 2026-06-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a temporary access lease to a mutable <see cref="Span{T}"/> borrowed from some owning resource.
/// </summary>
/// <remarks>
/// <para>
/// You must call <see cref="Dispose"/> once you have finished using <see cref="Span"/> (a <see langword="using"/> declaration/statement is the easiest way to guarantee this).
/// Disposal notifies the owning resource that the borrowed memory is no longer being written to, which may trigger further work on its part (for example, recalculating a bounding box after vertex data has been modified through <see cref="Span"/>).
/// </para>
/// <para>
/// Note that there is an implicit conversion defined from <see cref="ScopedSpanLease{T}"/> to <see cref="ScopedReadOnlySpanLease{T}"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The element type of <see cref="Span"/>.</typeparam>
public readonly unsafe ref struct ScopedSpanLease<T> {
	readonly delegate* managed<object?, nuint, void> _disposalCallback;
	readonly object? _callbackParam;
	/// <summary>
	/// An identifier for this specific lease, supplied by (and only meaningful to) whichever resource created it.
	/// This has no useful meaning to the consumer of this lease and should be ignored by them.
	/// </summary>
	public nuint LeaseId { get; }
	/// <summary>
	/// The borrowed span.
	/// </summary>
	public Span<T> Span { get; }

	/// <summary>
	/// Constructs a new <see cref="ScopedSpanLease{T}"/>.
	/// </summary>
	/// <remarks>
	/// This constructor is intended to be used by the resource lending out <paramref name="span"/>, not by the consumer receiving the lease.
	/// </remarks>
	/// <param name="disposalCallback">The callback to invoke, with <paramref name="callbackParam"/> and <paramref name="leaseId"/>, when this lease is disposed. May be <see langword="null"/> if no action is required on disposal.</param>
	/// <param name="callbackParam">An arbitrary value passed through to <paramref name="disposalCallback"/> unchanged; typically the resource instance that owns <paramref name="span"/>.</param>
	/// <param name="leaseId">The value exposed as <see cref="LeaseId"/>.</param>
	/// <param name="span">The span to lend out, exposed as <see cref="Span"/>.</param>
	public ScopedSpanLease(delegate*<object?, UIntPtr, void> disposalCallback, object? callbackParam, UIntPtr leaseId, Span<T> span) {
		_callbackParam = callbackParam;
		LeaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}

	/// <summary>
	/// Ends this lease, notifying the resource that lent out <see cref="Span"/> that it is no longer being used.
	/// </summary>
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, LeaseId);
	}

	/// <summary>
	/// Converts this lease to an equivalent read-only lease over the same borrowed memory.
	/// </summary>
	/// <param name="operand">The lease to convert.</param>
	public static implicit operator ScopedReadOnlySpanLease<T>(ScopedSpanLease<T> operand) {
		return new ScopedReadOnlySpanLease<T>(operand._disposalCallback,
			operand._callbackParam, operand.LeaseId, operand.Span);
	}
}

/// <summary>
/// Represents a temporary access lease to a read-only <see cref="ReadOnlySpan{T}"/> borrowed from some owning resource.
/// </summary>
/// <remarks>
/// You must call <see cref="Dispose"/> once you have finished using <see cref="Span"/> (a <see langword="using"/> declaration/statement is the easiest way to guarantee this).
/// Disposal notifies the owning resource that the borrowed memory is no longer being read from.
/// </remarks>
/// <typeparam name="T">The element type of <see cref="Span"/>.</typeparam>
public readonly unsafe ref struct ScopedReadOnlySpanLease<T> {
	readonly delegate* managed<object?, nuint, void> _disposalCallback;
	readonly object? _callbackParam;
	/// <summary>
	/// An identifier for this specific lease, supplied by (and only meaningful to) whichever resource created it.
	/// This has no useful meaning to the consumer of this lease and should be ignored by them.
	/// </summary>
	public nuint LeaseId { get; }
	/// <summary>
	/// The borrowed span.
	/// </summary>
	public ReadOnlySpan<T> Span { get; }

	/// <summary>
	/// Constructs a new <see cref="ScopedReadOnlySpanLease{T}"/>.
	/// </summary>
	/// <remarks>
	/// This constructor is intended to be used by the resource lending out <paramref name="span"/>, not by the consumer receiving the lease.
	/// </remarks>
	/// <param name="disposalCallback">The callback to invoke, with <paramref name="callbackParam"/> and <paramref name="leaseId"/>, when this lease is disposed. May be <see langword="null"/> if no action is required on disposal.</param>
	/// <param name="callbackParam">An arbitrary value passed through to <paramref name="disposalCallback"/> unchanged; typically the resource instance that owns <paramref name="span"/>.</param>
	/// <param name="leaseId">The value exposed as <see cref="LeaseId"/>.</param>
	/// <param name="span">The span to lend out, exposed as <see cref="Span"/>.</param>
	public ScopedReadOnlySpanLease(delegate*<object?, UIntPtr, void> disposalCallback, object? callbackParam, UIntPtr leaseId, ReadOnlySpan<T> span) {
		_callbackParam = callbackParam;
		LeaseId = leaseId;
		_disposalCallback = disposalCallback;
		Span = span;
	}

	/// <summary>
	/// Ends this lease, notifying the resource that lent out <see cref="Span"/> that it is no longer being used.
	/// </summary>
	public void Dispose() {
		if (_disposalCallback != null) _disposalCallback(_callbackParam, LeaseId);
	}
}