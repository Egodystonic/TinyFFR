// Created on 2025-03-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

static unsafe class EmbeddedResourceResolver {
	const string RootResourceNamespace = "Egodystonic.TinyFFR.";

	public readonly record struct ResourceDataRef(UIntPtr DataPtr, int DataLenBytes) {
		public Span<byte> AsSpan => new((void*) DataPtr, DataLenBytes);
	}

	static readonly ArrayPoolBackedMap<string, ResourceDataRef> _loadedResources = new();

	public static ResourceDataRef GetResource(string resourceName) {
		if (_loadedResources.TryGetValue(resourceName, out var cachedResource)) return cachedResource;

		using var stream = typeof(EmbeddedResourceResolver).Assembly.GetManifestResourceStream(RootResourceNamespace + resourceName)
						?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");

		var dataLen = checked((int) stream.Length);
		var dataPtr = checked(NativeMemory.Alloc((nuint) dataLen));
		stream.ReadExactly(new Span<byte>(dataPtr, dataLen));
		_loadedResources.Add(resourceName, new((UIntPtr) dataPtr, dataLen));
		return _loadedResources[resourceName];
	}
}