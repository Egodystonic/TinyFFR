// Created on 2023-09-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface IToleranceEquatable<T> : IEquatable<T> {
	bool Equals(T? other, float tolerance);
}