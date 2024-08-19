// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalAssetFactory : IAssetFactory, IDisposable {
	readonly FixedByteBufferPool _temporaryCpuBufferPool = new();
	readonly ArrayPoolBackedMap<nuint, FixedByteBufferPool.PooledByteBufferHandle> _activelyRentedBuffers = new();

	(UIntPtr BufferPtr, nuint BufferIdentifier) CreateTemporaryCpuBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged {

	}
}