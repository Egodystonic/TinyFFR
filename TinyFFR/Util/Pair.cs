// Created on 2024-07-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly record struct Pair<TFirst, TSecond>(TFirst First, TSecond Second) {
	public Pair<TSecond, TFirst> Swapped => new(Second, First);

	public static implicit operator Pair<TFirst, TSecond>((TFirst First, TSecond Second) tuple) => new(tuple.First, tuple.Second);
	public static implicit operator (TFirst First, TSecond Second)(Pair<TFirst, TSecond> pair) => (pair.First, pair.Second);
}