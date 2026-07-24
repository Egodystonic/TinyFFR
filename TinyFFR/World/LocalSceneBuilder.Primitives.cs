// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	readonly record struct PrimitiveData(CameraLockedQuadInstance? Quad, CameraLockedTextInstance? Text, PrimitivePaintbrush Paintbrush);
	
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<nuint, PrimitiveData>> _primitiveMap = new();
	readonly MapPool<nuint, PrimitiveData> _primitiveMapPool;
	nuint _prevPrimitiveHandleId = 0U;

	public ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var primitiveHandle = ++_prevPrimitiveHandleId;
		_primitiveMap[handle].Add(primitiveHandle, new(null, null, new()));
		return new ScenePrimitive(HandleToInstance(handle), primitiveHandle);
	}
	
	PrimitivePaintbrush DisposeExistingPrimitiveAndGetPaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].Remove(primitiveHandle, out var data)) return new();
		
		data.Quad?.Dispose();
		data.Text?.Dispose();
		
		return data.Paintbrush;
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var geometry = 
	}

	public void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		_ = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
	}
}
