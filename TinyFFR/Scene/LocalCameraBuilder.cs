// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalCameraBuilder : ICameraBuilder, ICameraAssetImplProvider, IDisposable {
	public LocalCameraBuilder() {
		
	}

	#region Native Methods
	
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

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new ObjectDisposedException(nameof(LocalAssetLoader));
	}
	#endregion
}