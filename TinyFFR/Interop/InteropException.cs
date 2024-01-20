// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Interop;

public sealed class InteropException : ApplicationException {
	public InteropException() { }
	public InteropException(string? message) : base(message) { }
	public InteropException(string? message, Exception? innerException) : base(message, innerException) { }
}