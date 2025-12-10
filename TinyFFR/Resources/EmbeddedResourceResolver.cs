// Created on 2025-03-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO.Compression;
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
						?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly (this is a bug in TinyFFR).");

		if (resourceName.EndsWith(".zip", StringComparison.Ordinal)) {
			using var za = new ZipArchive(stream);
			// For now we only ever expect 1 entry per zip file
			if (za.Entries.Count != 1) {
				throw new InvalidOperationException($"Embedded assembly resource '{resourceName}' is compressed but contains multiple interior resources (this is a bug in TinyFFR).");
			}
			var entry = za.Entries.Single();
			using var dataStream = entry.Open();
			var dataLen = checked((int) entry.Length);
			var dataPtr = checked(NativeMemory.Alloc((nuint) dataLen));
			dataStream.ReadExactly(new Span<byte>(dataPtr, dataLen));
			_loadedResources.Add(resourceName, new((UIntPtr) dataPtr, dataLen));
		}
		else {
			var dataLen = checked((int) stream.Length);
			var dataPtr = checked(NativeMemory.Alloc((nuint) dataLen));
			stream.ReadExactly(new Span<byte>(dataPtr, dataLen));
			_loadedResources.Add(resourceName, new((UIntPtr) dataPtr, dataLen));
		}
		
		return _loadedResources[resourceName];
	}
}