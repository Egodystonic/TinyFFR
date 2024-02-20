// Created on 2024-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR;

public class InvalidObjectException : InvalidOperationException {
	public InvalidObjectException() { }
	public InvalidObjectException(string? message) : base(message) { }
	public InvalidObjectException(string? message, Exception? innerException) : base(message, innerException) { }

	internal static void ThrowIfDefault<T>(T obj) where T : IEquatable<T> {
		if (obj.Equals(default)) throw InvalidDefault<T>();
	}

	internal static InvalidObjectException InvalidDefault<T>() => InvalidDefault(typeof(T));
	internal static InvalidObjectException InvalidDefault(Type t) {
		return new InvalidObjectException(
			$"Given object of type {t.Name} is not valid (the 'default' value of this type is not valid)."
		);
	}
}