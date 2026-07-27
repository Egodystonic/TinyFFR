// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

sealed class FakeRendererImplProvider : IRendererImplProvider {
	public ResourceHandle<Renderer> Handle { get; }
	public Scene Scene { get; }
	public Camera Camera { get; }
	public RenderOutputBuffer TargetBuffer { get; }
	public string Name { get; }
	public bool AutoUpdateCameraAspectRatio { get; }
	public RenderQualityConfig Quality { get; private set; }
	public bool Disposed { get; private set; }
	public int RenderCount { get; private set; }
	public int WaitForGpuCount { get; private set; }
	public readonly List<bool> FrustumCullingCalls = new();
	public readonly List<(Orientation2D Anchor, XYPair<float> Offset, XYPair<float> Dimensions)> SubAreaFractionCalls = new();
	public readonly List<(Orientation2D Anchor, XYPair<int> Offset, XYPair<int> Dimensions)> SubAreaPixelCalls = new();

	public FakeRendererImplProvider(ResourceHandle<Renderer> handle, Scene scene, Camera camera, RenderOutputBuffer targetBuffer, in RendererCreationConfig config) {
		Handle = handle;
		Scene = scene;
		Camera = camera;
		TargetBuffer = targetBuffer;
		Name = config.Name.ToString();
		AutoUpdateCameraAspectRatio = config.AutoUpdateCameraAspectRatio;
		Quality = config.Quality;
	}

	public Renderer RendererInstance => MockResourceFactory.Create<Renderer, IRendererImplProvider>(Handle, this);

	public bool IsDisposed(ResourceHandle<Renderer> handle) => Disposed;
	public void Dispose(ResourceHandle<Renderer> handle) => Disposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<Renderer> handle) => Name;
	public int GetNameLength(ResourceHandle<Renderer> handle) => Name.Length;
	public void CopyName(ResourceHandle<Renderer> handle, Span<char> destinationBuffer) => Name.CopyTo(destinationBuffer);

	public void Render(ResourceHandle<Renderer> handle) => RenderCount++;
	public void WaitForGpu(ResourceHandle<Renderer> handle) => WaitForGpuCount++;
	public void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig) => Quality = newConfig;
	public void SetFrustumCullingEnabled(ResourceHandle<Renderer> handle, bool enabled) => FrustumCullingCalls.Add(enabled);

	public void SetTargetViewportDimensionsByFraction(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalDimensions) {
		SubAreaFractionCalls.Add((anchor, fractionalOffset, fractionalDimensions));
	}
	public void SetTargetViewportDimensionsByPixel(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<int> fractionalLocation, XYPair<int> pixelDimensions) {
		SubAreaPixelCalls.Add((anchor, fractionalLocation, pixelDimensions));
	}

	public void CaptureScreenshot(ResourceHandle<Renderer> handle, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig, XYPair<int>? captureResolution) => throw new NotSupportedException();
	public void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) => throw new NotSupportedException();
	public unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) => throw new NotSupportedException();

	public Ray CastRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool yZeroOriginAtBottom, bool disableDpiScalingAdjustment) => default;
	public Ray CastRayFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool yZeroOriginAtBottom, bool disableDpiScalingAdjustment) => default;

	public Scene GetScene(ResourceHandle<Renderer> handle) => Scene;
	public Camera GetCamera(ResourceHandle<Renderer> handle) => Camera;
	public Window? GetWindow(ResourceHandle<Renderer> handle) => null;
	public RenderOutputBuffer? GetBuffer(ResourceHandle<Renderer> handle) => TargetBuffer;
}

sealed class FakeRenderOutputBufferImplProvider : IRenderOutputBufferImplProvider {
	public ResourceHandle<RenderOutputBuffer> Handle { get; }
	public string Name { get; }
	public XYPair<int> TextureDimensions { get; }
	public bool Disposed { get; private set; }
	public Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? CurrentHandler { get; private set; }
	public bool? LastHandlerLowestAddressesRepresentFrameTop { get; private set; }
	public int ClearHandlersCallCount { get; private set; }

	public FakeRenderOutputBufferImplProvider(ResourceHandle<RenderOutputBuffer> handle, in RenderOutputBufferCreationConfig config) {
		Handle = handle;
		Name = config.Name.ToString();
		TextureDimensions = config.TextureDimensions;
	}

	public RenderOutputBuffer BufferInstance => MockResourceFactory.Create<RenderOutputBuffer, IRenderOutputBufferImplProvider>(Handle, this);

	public bool IsDisposed(ResourceHandle<RenderOutputBuffer> handle) => Disposed;
	public void Dispose(ResourceHandle<RenderOutputBuffer> handle) => Disposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<RenderOutputBuffer> handle) => Name;
	public int GetNameLength(ResourceHandle<RenderOutputBuffer> handle) => Name.Length;
	public void CopyName(ResourceHandle<RenderOutputBuffer> handle, Span<char> destinationBuffer) => Name.CopyTo(destinationBuffer);

	public Texture CreateDynamicTexture(ResourceHandle<RenderOutputBuffer> handle) => throw new NotSupportedException();
	public XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle) => TextureDimensions;

	public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) {
		CurrentHandler = handler;
		LastHandlerLowestAddressesRepresentFrameTop = lowestAddressesRepresentFrameTop;
	}
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) => throw new NotSupportedException();
	public void ClearOutputChangeHandlers(ResourceHandle<RenderOutputBuffer> handle, bool cancelQueuedFrames) {
		CurrentHandler = null;
		ClearHandlersCallCount++;
	}
}

sealed class FakeCompositorImplProvider : IRendererCompositorImplProvider {
	public ResourceHandle<RendererCompositor> Handle { get; }
	public RenderOutputBuffer Target { get; }
	public string Name { get; }
	public bool Disposed { get; private set; }
	public bool? DisposedWithContainedRenderers { get; private set; }
	public int RenderAllCount { get; private set; }
	public int WaitForGpuCount { get; private set; }
	public readonly List<(Renderer Renderer, RenderCompositionType CompositionType)> AddCalls = new();
	public readonly List<(Renderer Renderer, bool Enabled)> SetEnabledStateCalls = new();

	public FakeCompositorImplProvider(ResourceHandle<RendererCompositor> handle, RenderOutputBuffer target, string name) {
		Handle = handle;
		Target = target;
		Name = name;
	}

	public RendererCompositor CompositorInstance => MockResourceFactory.Create<RendererCompositor, IRendererCompositorImplProvider>(Handle, this);

	public bool IsDisposed(ResourceHandle<RendererCompositor> handle) => Disposed;
	public void Dispose(ResourceHandle<RendererCompositor> handle) {
		Disposed = true;
		DisposedWithContainedRenderers ??= false;
	}
	public void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers) {
		Disposed = true;
		DisposedWithContainedRenderers = disposeContainedRenderers;
	}

	public string GetNameAsNewStringObject(ResourceHandle<RendererCompositor> handle) => Name;
	public int GetNameLength(ResourceHandle<RendererCompositor> handle) => Name.Length;
	public void CopyName(ResourceHandle<RendererCompositor> handle, Span<char> destinationBuffer) => Name.CopyTo(destinationBuffer);

	public void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType) => AddCalls.Add((renderer, compositionType));
	public void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState) => SetEnabledStateCalls.Add((renderer, newEnabledState));
	public IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle) => throw new NotSupportedException();
	public void RenderAll(ResourceHandle<RendererCompositor> handle) => RenderAllCount++;
	public void WaitForGpu(ResourceHandle<RendererCompositor> handle) => WaitForGpuCount++;
}

sealed class FakeRendererBuilder : IRendererBuilder {
	nuint _previousHandleId = 0;
	public readonly List<FakeRendererImplProvider> CreatedRenderers = new();
	public readonly List<FakeRenderOutputBufferImplProvider> CreatedBuffers = new();
	public readonly List<FakeCompositorImplProvider> CreatedCompositors = new();

	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		var impl = new FakeRendererImplProvider(++_previousHandleId, scene, camera, (RenderOutputBuffer) (object) renderTarget, in config);
		CreatedRenderers.Add(impl);
		return impl.RendererInstance;
	}

	public RenderOutputBuffer CreateRenderOutputBuffer(in RenderOutputBufferCreationConfig config) {
		var impl = new FakeRenderOutputBufferImplProvider(++_previousHandleId, in config);
		CreatedBuffers.Add(impl);
		return impl.BufferInstance;
	}

	public RendererCompositor CreateCompositor<TRenderTarget>(TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		var impl = new FakeCompositorImplProvider(++_previousHandleId, (RenderOutputBuffer) (object) renderTarget, name.ToString());
		CreatedCompositors.Add(impl);
		return impl.CompositorInstance;
	}
}

sealed class FakeResourceGroupImplProvider : IResourceGroupImplProvider {
	readonly List<IResource> _resources = new();
	readonly bool _disposesContainedResources;
	public ResourceHandle<ResourceGroup> Handle { get; }
	public string Name { get; }
	public bool Disposed { get; private set; }
	public bool Sealed { get; private set; }

	public FakeResourceGroupImplProvider(ResourceHandle<ResourceGroup> handle, string name, bool disposesContainedResources) {
		Handle = handle;
		Name = name;
		_disposesContainedResources = disposesContainedResources;
	}

	public ResourceGroup GroupInstance => MockResourceFactory.Create<ResourceGroup, IResourceGroupImplProvider>(Handle, this);

	public bool IsDisposed(ResourceHandle<ResourceGroup> handle) => Disposed;
	public void Dispose(ResourceHandle<ResourceGroup> handle) => Disposed = true;
	public void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources) => Disposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<ResourceGroup> handle) => Name;
	public int GetNameLength(ResourceHandle<ResourceGroup> handle) => Name.Length;
	public void CopyName(ResourceHandle<ResourceGroup> handle, Span<char> destinationBuffer) => Name.CopyTo(destinationBuffer);

	ReadOnlySpan<ResourceStub> IResourceGroupImplProvider.GetResources(ResourceHandle<ResourceGroup> handle) => throw new NotSupportedException();
	public int GetResourceCount(ResourceHandle<ResourceGroup> handle) => _resources.Count;
	public bool IsSealed(ResourceHandle<ResourceGroup> handle) => Sealed;
	public void Seal(ResourceHandle<ResourceGroup> handle) => Sealed = true;
	public void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource => _resources.Add(resource);
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceHandle<ResourceGroup> handle) where TResource : IResource<TResource> => throw new NotSupportedException();
	public TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource> {
		var seen = 0;
		foreach (var resource in _resources) {
			if (resource is not TResource typedResource) continue;
			if (seen++ == index) return typedResource;
		}
		throw new ArgumentOutOfRangeException(nameof(index));
	}
	public bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceHandle<ResourceGroup> handle) => _disposesContainedResources;
}

sealed class FakeResourceAllocator : IResourceAllocator {
	nuint _previousHandleId = 0;
	public readonly List<FakeResourceGroupImplProvider> CreatedGroups = new();

	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed) => CreateResourceGroup(disposeContainedResourcesWhenDisposed, ReadOnlySpan<char>.Empty, 0);
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity) => CreateResourceGroup(disposeContainedResourcesWhenDisposed, ReadOnlySpan<char>.Empty, initialCapacity);
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name) => CreateResourceGroup(disposeContainedResourcesWhenDisposed, name, 0);
	public ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity) {
		var impl = new FakeResourceGroupImplProvider(++_previousHandleId, name.ToString(), disposeContainedResourcesWhenDisposed);
		CreatedGroups.Add(impl);
		return impl.GroupInstance;
	}

	public Memory<T> CreatePooledMemoryBuffer<T>(int numElements) => throw new NotSupportedException();
	public void ReturnPooledMemoryBuffer<T>(Memory<T> buffer) => throw new NotSupportedException();
	public IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null) => throw new NotSupportedException();
	public IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>() => throw new NotSupportedException();
	public IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>() => throw new NotSupportedException();
	public IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache) => throw new NotSupportedException();
	public unsafe IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate* managed<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null) => throw new NotSupportedException();
}

sealed class FakeSceneImplProvider : ISceneImplProvider {
	public bool Disposed { get; private set; }

	public bool IsDisposed(ResourceHandle<Scene> handle) => Disposed;
	public void Dispose(ResourceHandle<Scene> handle) => Disposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<Scene> handle) => "Fake Scene";
	public int GetNameLength(ResourceHandle<Scene> handle) => "Fake Scene".Length;
	public void CopyName(ResourceHandle<Scene> handle, Span<char> destinationBuffer) => "Fake Scene".CopyTo(destinationBuffer);

	public void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance) { }
	public void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance) { }
	public void Add(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup) { }
	public void Remove(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup) { }
	public void Add<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight> { }
	public void Remove<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight> { }
	public void Add(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad) { }
	public void Remove(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad) { }
	public void Add(ResourceHandle<Scene> handle, CameraLockedTextInstance text) { }
	public void Remove(ResourceHandle<Scene> handle, CameraLockedTextInstance text) { }
	public ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle) { throw new NotSupportedException(); }
	public void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, in PrimitivePaintbrush paintbrush) { }
	public void DisposePrimitive(ResourceHandle<Scene> handle, UIntPtr primitiveHandle) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Location point, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, PositionedSphere sphere, bool wireframe) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, BoundedRay ray, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Ray ray, float size, bool constantScreenSize) { }

	public void SetBackdrop(ResourceHandle<Scene> handle, BuiltInSceneBackdrop backdrop, float indirectLightingIntensity, Rotation rotation) { }
	public void SetBackdrop(ResourceHandle<Scene> handle, BackdropTexture backdrop, float indirectLightingIntensity, Rotation rotation) { }
	public void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity) { }
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, BackdropTexture backdrop, float backdropIntensity, Rotation rotation) { }
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color) { }
	public void RemoveBackdrop(ResourceHandle<Scene> handle) { }
	public void RemoveAll(ResourceHandle<Scene> handle, bool includeModelInstances, bool includeLights, bool includePrimitives) { }

	public IndirectEnumerable<Scene, ModelInstance> GetModelInstances(ResourceHandle<Scene> handle) => throw new NotSupportedException();
	public IndirectEnumerable<Scene, Light> GetLights(ResourceHandle<Scene> handle) => throw new NotSupportedException();
}

sealed class FakeCameraImplProvider : ICameraImplProvider {
	public bool Disposed { get; private set; }
	public readonly List<float> AspectRatioCalls = new();

	public bool IsDisposed(ResourceHandle<Camera> handle) => Disposed;
	public void Dispose(ResourceHandle<Camera> handle) => Disposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<Camera> handle) => "Fake Camera";
	public int GetNameLength(ResourceHandle<Camera> handle) => "Fake Camera".Length;
	public void CopyName(ResourceHandle<Camera> handle, Span<char> destinationBuffer) => "Fake Camera".CopyTo(destinationBuffer);

	public Location GetPosition(ResourceHandle<Camera> handle) => default;
	public void SetPosition(ResourceHandle<Camera> handle, Location newPosition) { }
	public Direction GetViewDirection(ResourceHandle<Camera> handle) => default;
	public void SetViewDirection(ResourceHandle<Camera> handle, Direction newDirection) { }
	public Direction GetUpDirection(ResourceHandle<Camera> handle) => default;
	public void SetUpDirection(ResourceHandle<Camera> handle, Direction newDirection) { }
	public void SetViewAndUpDirection(ResourceHandle<Camera> handle, Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality) { }

	public Angle GetHorizontalFieldOfView(ResourceHandle<Camera> handle) => default;
	public void SetHorizontalFieldOfView(ResourceHandle<Camera> handle, Angle newFov) { }
	public Angle GetVerticalFieldOfView(ResourceHandle<Camera> handle) => default;
	public void SetVerticalFieldOfView(ResourceHandle<Camera> handle, Angle newFov) { }
	public float GetOrthographicHeight(ResourceHandle<Camera> handle) => default;
	public void SetOrthographicHeight(ResourceHandle<Camera> handle, float newHeight) { }
	public float GetAspectRatio(ResourceHandle<Camera> handle) => AspectRatioCalls.Count > 0 ? AspectRatioCalls[^1] : default;
	public void SetAspectRatio(ResourceHandle<Camera> handle, float newRatio) => AspectRatioCalls.Add(newRatio);

	public float GetExposure(ResourceHandle<Camera> handle) => default;
	public void SetExposure(ResourceHandle<Camera> handle, float newExposure) { }
	public void SetExposure(ResourceHandle<Camera> handle, float aperture, float shutterSpeed, float sensitivity) { }

	public float GetNearPlaneDistance(ResourceHandle<Camera> handle) => default;
	public void SetNearPlaneDistance(ResourceHandle<Camera> handle, float newDistance) { }
	public float GetFarPlaneDistance(ResourceHandle<Camera> handle) => default;
	public void SetFarPlaneDistance(ResourceHandle<Camera> handle, float newDistance) { }
	public CameraProjectionType GetProjectionType(ResourceHandle<Camera> handle) => default;
	public void SetProjectionType(ResourceHandle<Camera> handle, CameraProjectionType newProjectionType) { }

	public void GetProjectionMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix) => outMatrix = default;
	public void SetProjectionMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix) { }
	public void GetModelMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix) => outMatrix = default;
	public void SetModelMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix) { }
	public void GetViewMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix) => outMatrix = default;
	public void SetViewMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix) { }

	public void Translate(ResourceHandle<Camera> handle, Vect translation) { }
	public void Rotate(ResourceHandle<Camera> handle, Rotation rotation) { }
	public void Rotate(ResourceHandle<Camera> handle, Quaternion rotationQuaternion) { }

	public Ray CastRayFromNearPlane(ResourceHandle<Camera> handle, XYPair<float> normalizedNearPlaneCoord) => default;
}

static class BindableRendererTestScaffold {
	public static Scene CreateScene(nuint handleId = 1) => MockResourceFactory.Create<Scene, ISceneImplProvider>(handleId, new FakeSceneImplProvider());

	public static (Camera Camera, FakeCameraImplProvider Impl) CreateCamera(nuint handleId = 1) {
		var impl = new FakeCameraImplProvider();
		return (MockResourceFactory.Create<Camera, ICameraImplProvider>(handleId, impl), impl);
	}
}
