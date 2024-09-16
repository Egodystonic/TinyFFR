// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Scene;

public unsafe interface ICameraAssetImplProvider {
	public void Dispose(CameraAssetHandle handle);
	public bool IsDisposed(CameraAssetHandle handle);
}