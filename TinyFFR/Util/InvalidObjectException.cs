// Created on 2024-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Thrown when an operation is given an object that is not in a valid state to be used for that operation — most commonly, an uninitialized (<see langword="default"/>) value of a type whose default value is not usable.
/// </summary>
public class InvalidObjectException : InvalidOperationException {
	/// <summary>
	/// Constructs a new <see cref="InvalidObjectException"/> with no message.
	/// </summary>
	public InvalidObjectException() { }
	/// <summary>
	/// Constructs a new <see cref="InvalidObjectException"/> with the given <paramref name="message"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	public InvalidObjectException(string? message) : base(message) { }
	/// <summary>
	/// Constructs a new <see cref="InvalidObjectException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	/// <param name="innerException">The exception that caused this exception.</param>
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

	internal static InvalidObjectException InvalidDefault<T>(string objectName) => InvalidDefault(typeof(T), objectName);
	internal static InvalidObjectException InvalidDefault(Type t, string objectName) {
		return new InvalidObjectException(
			$"Given object '{objectName}' of type {t.Name} is not valid (the 'default' value of this type is not valid in this context)."
		);
	}
}