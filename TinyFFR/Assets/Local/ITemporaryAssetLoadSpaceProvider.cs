// Created on 2024-08-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Local;

interface ITemporaryAssetLoadSpaceProvider {
	(nuint BufferIdentity, UIntPtr DataPtr, int DataLengthBytes) CreateTemporaryAssetLoadSpace<T>(ReadOnlySpan<T> data) where T : unmanaged;
}