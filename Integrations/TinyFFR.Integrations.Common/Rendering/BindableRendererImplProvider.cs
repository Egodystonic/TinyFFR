// Created on 2025-08-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

sealed class BindableRendererImplProvider : IRendererImplProvider {
	const string DefaultRendererName = "Unnamed Bindable Renderer";
	static nuint _previousHandleId = 0;
	readonly ResourceHandle<Renderer> _handle;
	readonly string _name;
	readonly ResourceGroup _sceneAndCamera;
	readonly byte[] _serializedConfig;
	Renderer? _actualRenderer = null;
	RenderOutputBuffer? _actualRendererTarget = null;

	public Renderer BindableRendererInstance => new(_handle, this);

	public BindableRendererImplProvider(IResourceAllocator allocator, Scene scene, Camera camera, in RendererCreationConfig config) {
		_handle = ++_previousHandleId;

		_name = config.Name == default ? $"{DefaultRendererName} {_handle.AsInteger:X}" : config.Name.ToString();

		// Adding these to a group adds a dependency meaning users can't dispose the camera or group before this renderer is disposed
		_sceneAndCamera = allocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false, name: config.Name, initialCapacity: 2);
		_sceneAndCamera.Add(scene);
		_sceneAndCamera.Add(camera);
		_sceneAndCamera.Seal();
	}

	public static bool IsBindableRenderer(Renderer r) => r.Implementation is BindableRendererImplProvider;
	
	public static void AllocateOrReallocateOutputBuffer(Renderer r, XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler) {

	}

	public static void DisposeOutputBuffer(Renderer r) {

	}
}