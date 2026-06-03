// // Created on 2026-05-30 by Ben Bowen
// // (c) Egodystonic / TinyFFR 2026
//
// using Egodystonic.TinyFFR.Assets.Materials.Local;
// using Egodystonic.TinyFFR.Assets.Meshes;
// using Egodystonic.TinyFFR.Factory.Local;
// using Egodystonic.TinyFFR.Interop;
// using Egodystonic.TinyFFR.Rendering;
// using Egodystonic.TinyFFR.Resources;
// using Egodystonic.TinyFFR.Resources.Memory;
//
// namespace Egodystonic.TinyFFR.World;
//
// sealed unsafe class LocalScenePrimitiveImplProvider : IScenePrimitiveImplProvider, IDisposable {
// 	enum PrimitiveType {
// 		Nothing,
// 		Cuboid
// 	}
// 	readonly record struct PrimitiveBufferHandles(UIntPtr VertexBufferHandle, UIntPtr IndexBufferHandle, UIntPtr ModelInstanceHandle);
// 	[StructLayout(LayoutKind.Explicit)]
// 	readonly struct FixedScreenSizePrimitiveDataUnion {
// 		[FieldOffset(0)]
// 		public readonly Cuboid AsCuboid;
// 	}
// 	readonly record struct PrimitiveData(ResourceHandle<Scene> ContainingSceneHandle, PrimitiveType Type, PrimitiveBufferHandles Handles, bool IsFixedScreenSize, FixedScreenSizePrimitiveDataUnion FixedScreenSizeData);
// 	readonly LocalFactoryGlobalObjectGroup _globals;
// 	readonly LocalSceneBuilder _sceneBuilder;
// 	readonly ArrayPoolBackedMap<ResourceHandle<RenderPrimitive>, PrimitiveData> _livePrimitives = new();
// 	nuint _prevHandleId = 0U;
// 	bool _isDisposed = false;
//
// 	public LocalScenePrimitiveImplProvider(LocalFactoryGlobalObjectGroup globals, LocalSceneBuilder sceneBuilder) {
// 		ArgumentNullException.ThrowIfNull(globals);
// 		ArgumentNullException.ThrowIfNull(sceneBuilder);
// 		_globals = globals;
// 		_sceneBuilder = sceneBuilder;
// 		
// 		AllocateShapeBuffers();
// 	}
//
// 	public RenderPrimitive CreateNew(ResourceHandle<Scene> containingSceneHandle) { 
// 		var handle = new ResourceHandle<RenderPrimitive>(++_prevHandleId);
// 		_livePrimitives.Add(handle, new(containingSceneHandle, PrimitiveType.Nothing, UIntPtr.Zero, UIntPtr.Zero, false, new()));
// 		return HandleToInstance(handle);
// 	}
//
// 	public void SetAsNothing(ResourceHandle<RenderPrimitive> handle) {
// 		ThrowIfThisOrHandleIsDisposed(handle);
// 		var data = _livePrimitives[handle];
// 		DisposeAllBuffersIfPresent(ref data);
// 		_livePrimitives[handle] = data;
// 	}
// 	
// 	void NotifyOfSceneRender(ResourceHandle<Scene> handle, XYPair<int> renderTargetDimensions, Camera camera) {
// 		foreach (var kvp in _livePrimitives) {
// 			if (kvp.Value.ContainingSceneHandle != handle || !kvp.Value.IsFixedScreenSize) continue;
// 			switch (kvp.Value.Type) {
// 				case PrimitiveType.Cuboid:
// 					
// 					break;
// 			}
// 		}
// 	}
//
// 	#region Shapes
// 	UIntPtr _cuboidVb, _cuboidIb;
// 	
// 	void AllocateShapeBuffers() {
// 		AllocateCuboidBuffers();
// 	}
// 	
// 	void AllocateCuboidBuffers() {
// 		var cuboidVerts = _globals.CreateGpuHoldingBuffer<MeshVertexPrimitive>(4 * 6);
// 		var cuboidIndices = _globals.CreateGpuHoldingBuffer<VertexTriangle>(2 * 6);
// 		
// 		cuboidVerts.AsSpan<MeshVertexPrimitive>()[0] = new MeshVertexPrimitive(
// 	}
// 	void DisposeShapeBuffers() {
// 		if (_cuboidVb != UIntPtr.Zero) DisposeVertexBuffer(_cuboidVb).ThrowIfFailure();
// 		if (_cuboidIb != UIntPtr.Zero) DisposeVertexBuffer(_cuboidIb).ThrowIfFailure();
// 	}
// 	
// 	public void SetAsShape(ResourceHandle<RenderPrimitive> handle, ColorVect color, Cuboid shape, bool isFixedScreenSize) {
// 		
// 	}
// 	#endregion
// 	
// 	void DisposeAllBuffersIfPresent(ref PrimitiveData data) {
// 		if (data.Handles.VertexBufferHandle != UIntPtr.Zero) {
// 			DisposeVertexBuffer(data.Handles.VertexBufferHandle).ThrowIfFailure();
// 		}
// 		if (data.Handles.IndexBufferHandle != UIntPtr.Zero) {
// 			DisposeIndexBuffer(data.Handles.IndexBufferHandle).ThrowIfFailure();
// 		}
// 		if (data.Handles.ModelInstanceHandle != UIntPtr.Zero) {
// 			DisposeIndexBuffer(data.Handles.IndexBufferHandle).ThrowIfFailure();
// 		}
// 		data = data with { VertexBufferHandle = UIntPtr.Zero, IndexBufferHandle = UIntPtr.Zero };
// 	} 
//
// 	public string GetNameAsNewStringObject(ResourceHandle<RenderPrimitive> handle) {
// 		ThrowIfThisOrHandleIsDisposed(handle);
// 		return _livePrimitives[handle].Type.ToString();
// 	}
// 	public int GetNameLength(ResourceHandle<RenderPrimitive> handle) => GetNameAsNewStringObject(handle).Length;
// 	public void CopyName(ResourceHandle<RenderPrimitive> handle, Span<char> destinationBuffer) => GetNameAsNewStringObject(handle).CopyTo(destinationBuffer);
//
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	RenderPrimitive HandleToInstance(ResourceHandle<RenderPrimitive> h) => new(h, this);
// 	
// 	#region Native Methods
// 	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer_primitive")]
// 	static extern InteropResult AllocateVertexBufferPrimitive(
// 		nuint bufferId,
// 		MeshVertexPrimitive* verticesPtr,
// 		int numVertices,
// 		out UIntPtr outBufferHandle
// 	);
//
// 	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_vertex_buffer")]
// 	static extern InteropResult DisposeVertexBuffer(
// 		UIntPtr bufferHandle
// 	);
// 	
// 	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_vertex_buffer_primitive")]
// 	static extern InteropResult UpdateVertexBufferPrimitive(
// 		UIntPtr bufferHandle,
// 		nuint bufferId,
// 		MeshVertexPrimitive* verticesPtr,
// 		int vertexCount,
// 		int startingIndex
// 	);
// 	#endregion
//
// 	#region Disposal
// 	public void Dispose() {
// 		if (_isDisposed) return;
// 		try {
// 			foreach (var key in _livePrimitives.Keys) Dispose(key, false);
// 			_livePrimitives.Dispose();
// 			DisposeShapeBuffers();
// 		}
// 		finally {
// 			_isDisposed = true;
// 		}
// 	}
// 	
// 	public void NotifyOfSceneDisposal(ResourceHandle<Scene> handle) {
// 		if (_isDisposed) return;
// 		
// 		using var keysToRemove = _globals.HeapPool.Borrow<ResourceHandle<RenderPrimitive>>(_livePrimitives.Count);
// 		var keysAddedForRemoval = 0;
// 		foreach (var kvp in _livePrimitives) {
// 			if (kvp.Value.ContainingSceneHandle != handle) continue;
// 			
// 			Dispose(kvp.Key, false);
// 			keysToRemove.Buffer[keysAddedForRemoval++] = kvp.Key;
// 		}
// 		
// 		for (var i = 0; i < keysAddedForRemoval; ++i) {
// 			_livePrimitives.Remove(keysToRemove.Buffer[i]);
// 		}
// 	}
// 	
// 	public bool IsDisposed(ResourceHandle<RenderPrimitive> handle) => _isDisposed || !_livePrimitives.ContainsKey(handle);
//
// 	public void Dispose(ResourceHandle<RenderPrimitive> handle) => Dispose(handle, true);
// 	void Dispose(ResourceHandle<RenderPrimitive> handle, bool removeFromMap) {
// 		if (IsDisposed(handle)) return;
//
// 		// TODO remove from scene and dispose any buffers in use
// 		
// 		if (removeFromMap) _livePrimitives.Remove(handle);
// 	}
//
// 	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<RenderPrimitive> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(RenderPrimitive));
// 	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
// 	#endregion
// }