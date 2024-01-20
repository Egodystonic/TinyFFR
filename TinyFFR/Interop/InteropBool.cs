// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(byte))]
readonly struct InteropBool : IEquatable<InteropBool> {
	public static readonly InteropBool True = new(Byte.MaxValue);
	public static readonly InteropBool False = new(Byte.MinValue);
	
	readonly byte _asByte;
	InteropBool(byte asByte) => _asByte = asByte;

	public void ThrowIfFalse() {
		if (this) return;
		throw new InteropException(NativeUtils.GetLastError());
	}

	public bool Equals(InteropBool other) => _asByte == other._asByte;
	public override bool Equals(object? obj) => obj is InteropBool other && Equals(other);
	public override int GetHashCode() => _asByte.GetHashCode();
	public static bool operator ==(InteropBool left, InteropBool right) => left.Equals(right);
	public static bool operator !=(InteropBool left, InteropBool right) => !left.Equals(right);

	public static implicit operator InteropBool(bool b) => b ? True : False;
	public static implicit operator bool(InteropBool b) => b != False;

	public override string ToString() => this == False ? "False" : "True";
}