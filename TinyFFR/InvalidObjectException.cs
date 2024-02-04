// Created on 2024-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR;

public class InvalidObjectException : InvalidOperationException {
	public InvalidObjectException() { }
	public InvalidObjectException(string? message) : base(message) { }
	public InvalidObjectException(string? message, Exception? innerException) : base(message, innerException) { }

	internal static InvalidOperationException InvalidDefault<T>() => InvalidDefault(typeof(T));
	internal static InvalidOperationException InvalidDefault(Type t) {
		return new InvalidOperationException(
			$"Given object of type {t.Name} was not properly constructed (the 'default' value of this type is not valid)- initialize all required members explicitly or use the non-default constructor if available."
		);
	}
}