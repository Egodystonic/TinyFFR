// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalObjectBuilder : IObjectBuilder, IModelInstanceImplProvider, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<ModelInstanceHandle, (Mesh Mesh, Material Material)> _activeInstances = new();

	public LocalObjectBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ModelInstance CreateModelInstance(Mesh mesh, Material material) => CreateModelInstance(mesh, material, new());
	public ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config) {
		var meshBufferData = mesh.BufferData;
		AllocateModelInstance(
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount,
			material.Handle,
			out var handle
		).ThrowIfFailure();
		var result = new ModelInstance(handle, this);
		_activeInstances.Add(handle, (mesh, material));
		_globals.DependencyTracker.RegisterDependency(result, mesh);
		_globals.DependencyTracker.RegisterDependency(result, material);
		return result;
	}

	public Mesh GetMesh(ModelInstanceHandle handle) {
		return _globals.DependencyTracker.RegisterDependency(
	}
	public void SetMesh(ModelInstanceHandle handle, Mesh newMesh) {
		
	}

	public Material GetMaterial(ModelInstanceHandle handle) {

	}
	public void SetMaterial(ModelInstanceHandle handle, Material newMaterial) {

	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_model_instance")]
	static extern InteropResult AllocateModelInstance(
		UIntPtr vertexBufferHandle,
		UIntPtr indexBufferHandle,
		int indexBufferStartIndex,
		int indexBufferCount,
		UIntPtr materialHandle,
		out UIntPtr outModelInstanceHandle
	);
	#endregion

	#region Disposal
	public void Dispose() {
		_modelInstanceImplProvider.Dispose();
	}
	#endregion
}