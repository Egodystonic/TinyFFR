// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Represents a resource type (<typeparamref name="TSelf"/>) that is really just a more specific view over an existing resource of type <typeparamref name="TBase"/> (for example, a <c>CanvasTexture</c> is a specialization of a <c>ModelInstance</c>), rather than being backed by its own independent handle/implementation pair.
/// </summary>
/// <typeparam name="TSelf">The specialized resource type.</typeparam>
/// <typeparam name="TBase">The underlying resource type that <typeparamref name="TSelf"/> specializes.</typeparam>
public interface IResourceSpecialization<TSelf, TBase> : IDisposable where TSelf : struct, IResourceSpecialization<TSelf, TBase> where TBase : IResource<TBase> {
	internal static abstract IntPtr SpecializationTypeIdentifier { get; }
	internal int SpecializationDataLength { get; }
	internal static abstract void Smuggle(TSelf resource, Span<byte> specializationDataBuffer, out TBase outBaseResource, out ResourceStub? additionalResourceRef);
	internal static abstract TSelf DeSmuggle(TBase baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef);
	internal static void DisposeViaStub(ResourceStub baseResourceStub, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) => TSelf.DeSmuggle(TBase.CreateFromStub(baseResourceStub), specializationDataBuffer, additionalResourceRef).Dispose();
	internal static object BoxViaStub(ResourceStub baseResourceStub, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) => TSelf.DeSmuggle(TBase.CreateFromStub(baseResourceStub), specializationDataBuffer, additionalResourceRef);
}
