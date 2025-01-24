// Created on 2023-09-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface IToleranceEquatable<T> : IEquatable<T> where T : allows ref struct {
	bool Equals(T? other, float tolerance);
}