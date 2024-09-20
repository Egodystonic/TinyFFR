// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed unsafe class LocalAssetLoader : IAssetLoader, IAssetResourcePoolProvider, IDisposable {
	static readonly ArrayPoolBackedMap<nuint, (LocalAssetLoader Loader, FixedByteBufferPool.FixedByteBuffer Buffer)> _activelyRentedBuffers = new();
	static nuint _nextBufferId = 0;
	readonly FixedByteBufferPool _temporaryCpuBufferPool;
	readonly FixedByteBufferPool _assetNamePool;
	readonly LocalMeshBuilder _meshBuilder;
	bool _isDisposed = false;

	public IMeshBuilder MeshBuilder => _meshBuilder;

	static LocalAssetLoader() {
		SetBufferDeallocationDelegate(&DeallocateRentedBuffer).ThrowIfFailure();
	}

	public LocalAssetLoader(LocalAssetLoaderConfig config) {
		_temporaryCpuBufferPool = new FixedByteBufferPool(config.MaxAssetSizeBytes);
		_assetNamePool = new FixedByteBufferPool(config.MaxAssetNameLength * sizeof(char));
		_meshBuilder = new LocalMeshBuilder(this);
	}

	[UnmanagedCallersOnly]
	static void DeallocateRentedBuffer(nuint bufferId) {
		if (!_activelyRentedBuffers.ContainsKey(bufferId)) throw new InvalidOperationException($"Buffer '{bufferId}' has already been deallocated.");
		var tuple = _activelyRentedBuffers[bufferId];
		_activelyRentedBuffers.Remove(bufferId);
		var loader = tuple.Loader;
		loader._temporaryCpuBufferPool.Return(tuple.Buffer);

		if (loader._isDisposed) loader.DisposeCpuBufferPoolIfSafe();
	}

	IAssetResourcePoolProvider.TemporaryLoadSpaceBuffer IAssetResourcePoolProvider.CopySpanToTemporaryAssetLoadSpace<T>(ReadOnlySpan<T> data) {
		ThrowIfThisIsDisposed();
		var sizeBytes = sizeof(T) * data.Length;
		if (sizeBytes > _temporaryCpuBufferPool.MaxBufferSizeBytes) {
			throw new InvalidOperationException($"Can not load asset because its in-memory size is {sizeBytes} bytes (" +
												$"the maximum asset size configured is {_temporaryCpuBufferPool.MaxBufferSizeBytes} bytes; " +
												$"the limit can be raised by setting the {nameof(LocalAssetLoaderConfig.MaxAssetSizeBytes)} value in " +
												$"the {nameof(LocalAssetLoaderConfig)} passed to the {nameof(LocalRendererFactory)} constructor).");
		}
		var bufferId = _nextBufferId++;
		var buffer = _temporaryCpuBufferPool.Rent<T>(data.Length);
		_activelyRentedBuffers.Add(bufferId, (this, buffer));
		data.CopyTo(buffer.AsSpan<T>(data.Length));
		return new(bufferId, buffer.StartPtr, sizeBytes);
	}

	IAssetResourcePoolProvider.AssetNameBuffer IAssetResourcePoolProvider.CopyAssetNameToFixedBuffer(ReadOnlySpan<char> data) {
		ThrowIfThisIsDisposed();
		var characterCount = Math.Min(data.Length, _assetNamePool.GetMaxBufferSize<char>());
		var result = new IAssetResourcePoolProvider.AssetNameBuffer(_assetNamePool.Rent<char>(characterCount), characterCount);
		data.CopyTo(result.AsSpan);
		return result;
	}

	void IAssetResourcePoolProvider.DeallocateNameBuffer(IAssetResourcePoolProvider.AssetNameBuffer buffer) {
		_assetNamePool.Return(buffer.Buffer);
	}

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_buffer_deallocation_delegate")]
	static extern InteropResult SetBufferDeallocationDelegate(
		delegate* unmanaged<nuint, void> bufferDeallocDelegate
	);
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_meshBuilder.Dispose();
			_assetNamePool.Dispose();
			DisposeCpuBufferPoolIfSafe();
		}
		finally {
			_isDisposed = true;
		}
	}

	void DisposeCpuBufferPoolIfSafe() {
		foreach (var kvp in _activelyRentedBuffers) {
			if (ReferenceEquals(kvp.Value.Loader, this)) return;
		}

		_temporaryCpuBufferPool.Dispose();
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	#endregion
}