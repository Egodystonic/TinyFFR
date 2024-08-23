// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IDisposable {
	readonly ITemporaryAssetLoadSpaceProvider _loadSpaceProvider;

	public LocalMeshBuilder(ITemporaryAssetLoadSpaceProvider assetLoadSpaceProvider) {
		ArgumentNullException.ThrowIfNull(assetLoadSpaceProvider);
		_loadSpaceProvider = assetLoadSpaceProvider;
	}



	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		UIntPtr verticesPtr,
		int numVertices,
		out AssetHandle outAssetHandle
	);
	#endregion

	#region Disposal
	// TODO when disposing vbs/ibs/meshes, simply remove them from the collections until they're the last, and then dispose them
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_liveInstance = null;
			foreach (var kvp in _detectedControllerStateObjectMap) kvp.Value.Dispose();

			_controllerEventBuffer.Dispose();
			_detectedControllerStateObjectVector.Dispose();
			_detectedControllerStateObjectMap.Dispose();
			_combinedControllerState.Dispose();
			_kbmStateObject.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
	#endregion
}