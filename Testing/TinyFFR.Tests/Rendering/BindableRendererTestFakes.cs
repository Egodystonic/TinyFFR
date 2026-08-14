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
	public readonly List<(XYPair<int> PixelCoord, DiagonalOrientation2D CoordOrigin, bool DisableDpiScalingAdjustment)> RenderSurfaceRayCalls = new();
	public readonly List<(XYPair<int> PixelCoord, DiagonalOrientation2D CoordOrigin, bool DisableDpiScalingAdjustment)> ViewportSurfaceRayCalls = new();

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
	public void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) => throw new NotSupportedException();
	public unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) => throw new NotSupportedException();

	public Ray CastRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment) {
		RenderSurfaceRayCalls.Add((pixelCoord, coordOrigin, disableDpiScalingAdjustment));
		return default;
	}
	public Ray CastRayFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment) {
		ViewportSurfaceRayCalls.Add((pixelCoord, coordOrigin, disableDpiScalingAdjustment));
		return default;
	}

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
	public Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? CurrentHandler { get; private set; }
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

	public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) {
		CurrentHandler = handler;
		LastHandlerLowestAddressesRepresentFrameTop = lowestAddressesRepresentFrameTop;
	}
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool lowestAddressesRepresentFrameTop, bool handleOnlyNextChange) => throw new NotSupportedException();
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
	public readonly List<(Renderer Renderer, int? MaxFramesPerSecond)> SetFrameRateCapCalls = new();
	public readonly List<(Renderer Renderer, int RenderOnceEveryNFrames)> SetFrameRateRatioCalls = new();

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
	public void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond) => SetFrameRateCapCalls.Add((renderer, maxFramesPerSecond));
	public int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		for (var i = SetFrameRateCapCalls.Count - 1; i >= 0; --i) {
			if (SetFrameRateCapCalls[i].Renderer == renderer) return SetFrameRateCapCalls[i].MaxFramesPerSecond;
		}
		return null;
	}
	public void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int ratioDenominator) => SetFrameRateRatioCalls.Add((renderer, ratioDenominator));
	public int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		for (var i = SetFrameRateRatioCalls.Count - 1; i >= 0; --i) {
			if (SetFrameRateRatioCalls[i].Renderer == renderer) return SetFrameRateRatioCalls[i].RenderOnceEveryNFrames;
		}
		return 1;
	}
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

	public Renderer CreateRenderer<TRenderTarget>(CanvasScene scene, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		throw new NotSupportedException();
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
	public void SetPrimitiveGeometryPoint(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Location point, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometryString(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, PositionedSphere sphere, bool wireframe) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, BoundedRay ray, float size, bool includeEndpoints, bool constantScreenSize) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Ray ray, float size, bool includeStartPoint, bool constantScreenSize) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Line line, float size, bool constantScreenSize) { }
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Plane plane) { }
	public void SetPrimitiveGeometryGrid(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, Location gridCentre, Direction gridNormal, Direction gridX, float gridSize, float majorGridLineSpacing, float minorGridLineSpacing) { }

	public void SetBackdrop(ResourceHandle<Scene> handle, BuiltInSceneBackdrop backdrop, float indirectLightingIntensity, Rotation rotation) { }
	public void SetBackdrop(ResourceHandle<Scene> handle, BackdropTexture backdrop, float indirectLightingIntensity, Rotation rotation) { }
	public void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity) { }
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, BackdropTexture backdrop, float backdropIntensity, Rotation rotation) { }
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color) { }
	public void RemoveBackdrop(ResourceHandle<Scene> handle) { }
	public void AddFog(ResourceHandle<Scene> handle, in FogDescriptor fogDescriptor) { }
	public void RemoveFog(ResourceHandle<Scene> handle) { }
	public void RemoveAll(ResourceHandle<Scene> handle, bool includeModelInstances, bool includeLights, bool includePrimitives) { }

	public IndirectEnumerable<Scene, ModelInstance> GetModelInstances(ResourceHandle<Scene> handle) => throw new NotSupportedException();
	public IndirectEnumerable<Scene, Light> GetLights(ResourceHandle<Scene> handle) => throw new NotSupportedException();

	public CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Texture texture) => throw new NotSupportedException();
	public CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Material material) => throw new NotSupportedException();
	public CanvasText AddCanvasObject(ResourceHandle<Scene> handle, FontString str, FontPen pen) => throw new NotSupportedException();
	public CanvasText AddCanvasObject(ResourceHandle<Scene> handle, ReadOnlySpan<char> str, FontPen pen, TextJustification multiLineJustification) => throw new NotSupportedException();
	public void SetCanvasBlendTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture blendTexture) => throw new NotSupportedException();
	public void SetCanvasBlendTextureDistance(ResourceHandle<Scene> handle, QuadInstance quad, float distance) => throw new NotSupportedException();
	public XYPair<int> GetCanvasPrecisePixelCoord(ResourceHandle<Scene> handle, XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment) => throw new NotSupportedException();
	public XYPair<int> GetCanvasSizePixels(ResourceHandle<Scene> handle) => throw new NotSupportedException();
	public XYPair<int> ConvertCanvasFractionToPixels(ResourceHandle<Scene> handle, XYPair<float> fraction) => throw new NotSupportedException();
	public XYPair<float> ConvertCanvasPixelsToFraction(ResourceHandle<Scene> handle, XYPair<int> pixels) => throw new NotSupportedException();
	public bool CanvasObjectContainsPixelCoord(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> coord, DiagonalOrientation2D coordOrigin) => throw new NotSupportedException();
	public XYPair<int> GetCanvasObjectActualSizePixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public XYPair<float> GetCanvasObjectActualSizeFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectDockParent(ResourceHandle<Scene> handle, ModelInstance modelInstance, ModelInstance? parent) => throw new NotSupportedException();
	public float GetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad) => throw new NotSupportedException();
	public void SetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad, float newValue) => throw new NotSupportedException();
	public Camera GetCanvasCamera(ResourceHandle<Scene> handle) => throw new NotSupportedException();
	public Orientation2D GetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D newValue) => throw new NotSupportedException();
	public Orientation2D? GetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D? newValue) => throw new NotSupportedException();
	public Angle GetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle newValue) => throw new NotSupportedException();
	public int GetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance, int newValue) => throw new NotSupportedException();
	public bool GetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance, bool newValue) => throw new NotSupportedException();
	public XYPair<int> GetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> newValue) => throw new NotSupportedException();
	public XYPair<float> GetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue) => throw new NotSupportedException();
	public int? GetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue) => throw new NotSupportedException();
	public int? GetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue) => throw new NotSupportedException();
	public float? GetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue) => throw new NotSupportedException();
	public float? GetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue) => throw new NotSupportedException();
	public void SetCanvasObjectPlacement(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<int> positionPixels, XYPair<float> positionFraction, int? widthPixels, int? heightPixels, float? widthFraction, float? heightFraction) => throw new NotSupportedException();
	public void MoveCanvasObjectByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> translation) => throw new NotSupportedException();
	public void MoveCanvasObjectByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> translation) => throw new NotSupportedException();
	public void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, float scalar) => throw new NotSupportedException();
	public void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect) => throw new NotSupportedException();
	public void AdjustCanvasObjectScaleByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> vect) => throw new NotSupportedException();
	public void AdjustCanvasObjectScaleByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect) => throw new NotSupportedException();
	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation) => throw new NotSupportedException();
	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<int> pivotPointPixels) => throw new NotSupportedException();
	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<float> pivotPointFraction) => throw new NotSupportedException();
	public Texture GetCanvasObjectTexture(ResourceHandle<Scene> handle, QuadInstance quad) => throw new NotSupportedException();
	public void SetCanvasObjectTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture newValue) => throw new NotSupportedException();
	public XYPair<float> GetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
	public void SetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue) => throw new NotSupportedException();
	public XYPair<int> GetCanvasObjectTextureDimensions(ResourceHandle<Scene> handle, QuadInstance quad) => throw new NotSupportedException();
	public XYPair<float> GetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad) => throw new NotSupportedException();
	public void SetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue) => throw new NotSupportedException();
	public void SetCanvasObjectTextureOffsetPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue) => throw new NotSupportedException();
	public XYPair<float> GetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad) => throw new NotSupportedException();
	public void SetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue) => throw new NotSupportedException();
	public void SetCanvasObjectTextureExtentPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue) => throw new NotSupportedException();
	public void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, FontString newValue) => throw new NotSupportedException();
	public void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, ReadOnlySpan<char> str, TextJustification multiLineJustification) => throw new NotSupportedException();
	public TextLayout GetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text) => throw new NotSupportedException();
	public void SetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text, TextLayout newValue) => throw new NotSupportedException();
	public bool GetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text) => throw new NotSupportedException();
	public void SetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text, bool newValue) => throw new NotSupportedException();
	public void DisposeCanvasObject(ResourceHandle<Scene> handle, ModelInstance modelInstance) => throw new NotSupportedException();
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

	public float? GetFocusDistance(ResourceHandle<Camera> handle) => default;
	public void SetFocusDistance(ResourceHandle<Camera> handle, float? newFocusDistance) { }

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
