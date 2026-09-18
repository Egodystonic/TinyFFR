// Created on 2024-11-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Thrown when attempting to add a resource to a <see cref="ResourceGroup"/> that has already been sealed (see <see cref="ResourceGroup.Seal"/>).
/// </summary>
public class ResourceGroupSealedException : InvalidOperationException {
	/// <summary>
	/// Constructs a new <see cref="ResourceGroupSealedException"/> with no message.
	/// </summary>
	public ResourceGroupSealedException() { }
	/// <inheritdoc />
	[Obsolete("Obsolete")]
	protected ResourceGroupSealedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	/// <summary>
	/// Constructs a new <see cref="ResourceGroupSealedException"/> with the given <paramref name="message"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	public ResourceGroupSealedException(string? message) : base(message) { }
	/// <summary>
	/// Constructs a new <see cref="ResourceGroupSealedException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	/// <param name="innerException">The exception that caused this exception.</param>
	public ResourceGroupSealedException(string? message, Exception? innerException) : base(message, innerException) { }
}