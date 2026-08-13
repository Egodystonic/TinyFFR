// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceSpecialization<TSelf, TBase> where TSelf : IResourceSpecialization<TSelf, TBase> where TBase : IResource<TBase> {
	internal static abstract nuint SpecializationTypeIdentifier { get; }
	internal int SpecializationDataLength { get; }
	internal static abstract void Smuggle(TSelf resource, Span<byte> specializationDataBuffer, out TBase outBaseResource);
	internal static abstract TSelf DeSmuggle(TBase baseResource, ReadOnlySpan<byte> specializationDataBuffer);
}