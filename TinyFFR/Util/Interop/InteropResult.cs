// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(byte))]
readonly struct InteropResult : IEquatable<InteropResult> {
	public static readonly InteropResult Success = new(Byte.MaxValue);
	public static readonly InteropResult Failure = new(Byte.MinValue);
	
	readonly byte _asByte;
	InteropResult(byte asByte) => _asByte = asByte;

	public void ThrowIfFailure() {
		if (this) return;
		throw new InvalidOperationException(LocalNativeUtils.GetLastError());
	}

	public bool Equals(InteropResult other) => _asByte == other._asByte;
	public override bool Equals(object? obj) => obj is InteropResult other && Equals(other);
	public override int GetHashCode() => _asByte.GetHashCode();
	public static bool operator ==(InteropResult left, InteropResult right) => left.Equals(right);
	public static bool operator !=(InteropResult left, InteropResult right) => !left.Equals(right);

	public static implicit operator InteropResult(bool b) => b ? Success : Failure;
	public static implicit operator bool(InteropResult b) => b != Failure;

	public override string ToString() => this == Failure ? "Failure" : "Success";
}