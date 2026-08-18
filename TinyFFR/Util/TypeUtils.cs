using System;
using System.Reflection.Metadata;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

static class TypeUtils {
	public static InvalidCastException InvalidCast(string? operand, string? expectedType, string? actualType) {
		return new InvalidCastException($"Can not cast {operand ?? "null"} to {expectedType ?? "null"}: Actual type is {actualType ?? "null"}.");
	}
	public static InvalidCastException InvalidCast(object? operand, object? expectedType, object? actualType) {
		return InvalidCast(operand?.ToString(), expectedType?.ToString(), actualType?.ToString());
	}
	public static InvalidCastException InvalidCast(object? operand, object? expectedType) {
		return InvalidCast(operand?.ToString(), expectedType?.ToString(), operand?.GetType().ToString());
	}
	public static InvalidCastException InvalidCast(object? operand, Type? expectedType, Type? actualType) {
		return InvalidCast(operand?.ToString(), expectedType?.Name, actualType?.Name);
	}
	public static InvalidCastException InvalidCast(object? operand, Type? expectedType) {
		return InvalidCast(operand?.ToString(), expectedType?.Name, operand?.GetType().Name);
	}
	
	public static ResourceStub ResourceToStub<TResource>(TResource resource) where TResource : IResource => resource.AsStub;
	public static TResource StubToResource<TResource>(ResourceStub stub) where TResource : IResource<TResource> => TResource.CreateFromStub(stub);
}

#pragma warning disable CS0169 
readonly struct Unused { readonly nint _; }
#pragma warning restore CS0169 
