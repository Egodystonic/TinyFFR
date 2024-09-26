// Created on 2024-08-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe interface IAssetResourcePoolProvider {
	readonly record struct TemporaryLoadSpaceBuffer(nuint BufferIdentity, UIntPtr DataPtr, int DataLengthBytes) {
		public Span<T> AsSpan<T>() where T : unmanaged => MemoryMarshal.Cast<byte, T>(new Span<byte>((void*) DataPtr, DataLengthBytes));
	}
	TemporaryLoadSpaceBuffer CopySpanToTemporaryAssetLoadSpace<T>(ReadOnlySpan<T> data) where T : unmanaged;
}