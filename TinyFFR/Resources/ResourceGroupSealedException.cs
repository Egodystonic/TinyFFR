// Created on 2024-11-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Resources;

public class ResourceGroupSealedException : InvalidOperationException {
	public ResourceGroupSealedException() { }
	[Obsolete("Obsolete")]
	protected ResourceGroupSealedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	public ResourceGroupSealedException(string? message) : base(message) { }
	public ResourceGroupSealedException(string? message, Exception? innerException) : base(message, innerException) { }
}