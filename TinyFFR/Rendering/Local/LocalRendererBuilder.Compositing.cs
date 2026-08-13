// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Diagnostics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering.Local;

sealed partial class LocalRendererBuilder {
	readonly record struct RateLimitShimResourceData(RenderOutputBuffer PrivateBuffer, Renderer ProxyRenderer, CanvasScene ProxyScene, CanvasTexture ProxyQuad, XYPair<int> PrivateBufferSize);
	readonly record struct CompositedRenderer(Renderer Renderer, RenderCompositionType CompositionType, bool IsEnabled) {
		public CompositorSubRendererRateLimitHelper RateLimitHelper { get; init; } = CompositorSubRendererRateLimitHelper.Unlimited;
		public RateLimitShimResourceData? RateLimitShimResources { get; init; } = null;

		public bool IsRateLimited => RateLimitHelper.CapOrRatioEnabled;
	}
	readonly record struct RendererCompositorData(RenderTargetUnion RenderTarget, ArrayPoolBackedVector<CompositedRenderer> AddedRenderers, int NumRenderersCurrentlyEnabled, int FirstEnabledRendererIndex, int LastEnabledRendererIndex) {
		public int NumEnabledRateLimitedRenderers { get; init; } = 0;
	}
	const string DefaultCompositorName = "Unnamed Renderer Compositor";
	const string RateLimitBufferName = "Frame Rate Limit Buffer";
	const string RateLimitSceneName = "Frame Rate Limit Canvas";
	const string RateLimitProxyRendererName = "Frame Rate Limit Proxy Renderer";
	readonly VectorPool<CompositedRenderer> _compositedRendererVectorPool = new(true);
	readonly ArrayPoolBackedMap<ResourceHandle<RendererCompositor>, RendererCompositorData> _loadedCompositors = new();
	readonly LocalSceneBuilder _sceneBuilder;
	readonly LocalRendererCompositorImplProvider _rendererCompositorImplProvider;

	public RendererCompositor CreateCompositor<TRenderTarget>(TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		ThrowIfThisIsDisposed();

		var rtu = AllocateOrRetrieveRenderTargetUnion(renderTarget);
		
		_previousHandleId++;
		var handle = new ResourceHandle<RendererCompositor>(_previousHandleId);
		_loadedCompositors.Add(handle, new RendererCompositorData(rtu, _compositedRendererVectorPool.Rent(), 0, -1, -1));

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultCompositorName);
		var result = HandleToInstance(handle);
		_globals.DependencyTracker.RegisterDependency(result, renderTarget);
		return result;
	}

	void RecalculateCompositorCounts(ResourceHandle<RendererCompositor> handle) {
		var compositorData = _loadedCompositors[handle];
		var count = 0;
		var first = -1;
		var last = -1;
		var rateLimitedCount = 0;

		for (var i = 0; i < compositorData.AddedRenderers.Count; ++i) {
			var rendererData = compositorData.AddedRenderers[i];
			if (!rendererData.IsEnabled) continue;

			count++;
			if (first == -1) first = i;
			last = i;
			if (rendererData.IsRateLimited) rateLimitedCount++;
		}

		_loadedCompositors[handle] = _loadedCompositors[handle] with {
			NumRenderersCurrentlyEnabled = count,
			FirstEnabledRendererIndex = first,
			LastEnabledRendererIndex = last,
			NumEnabledRateLimitedRenderers = rateLimitedCount
		};
	}

	int GetAddedRendererIndexOrThrow(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		var addedRenderers = _loadedCompositors[handle].AddedRenderers;
		for (var i = 0; i < addedRenderers.Count; ++i) {
			if (addedRenderers[i].Renderer == renderer) return i;
		}
		throw new ArgumentException($"{renderer} has not been added to {HandleToInstance(handle)}.", nameof(renderer));
	}

	
	public void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var compositorData = _loadedCompositors[handle];
		var rendererTarget = _loadedRenderers[renderer.Handle].RenderTarget;
		
		foreach (var rd in compositorData.AddedRenderers) {
			if (rd.Renderer == renderer) {
				throw new ArgumentException($"{renderer} has already been added to {HandleToInstance(handle)}.", nameof(renderer));
			}
		}

		if (compositorData.RenderTarget != rendererTarget) {
			throw new ArgumentException(
				$"{renderer} targets {rendererTarget}, but this compositor was created targeting {compositorData.RenderTarget}.",
				nameof(renderer)
			);
		}

		compositorData.AddedRenderers.Add(new(renderer, compositionType, true));
		RecalculateCompositorCounts(handle);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), renderer);
	}
	
	public void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var compositorData = _loadedCompositors[handle];
		for (var i = 0; i < compositorData.AddedRenderers.Count; ++i) {
			if (compositorData.AddedRenderers[i].Renderer == renderer) {
				compositorData.AddedRenderers[i] = compositorData.AddedRenderers[i] with { IsEnabled = newEnabledState };
				RecalculateCompositorCounts(handle);
				return;
			}
		}
		
		throw new ArgumentException($"{renderer} has not been added to {HandleToInstance(handle)}.", nameof(renderer));
	}
	public unsafe IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var compositorData = _loadedCompositors[handle];
		
		static int GetCount(RendererCompositor rc) => ((LocalRendererCompositorImplProvider) rc.Implementation).Owner._loadedCompositors[rc.Handle].AddedRenderers.Count;
		static int GetVersion(RendererCompositor rc) => ((LocalRendererCompositorImplProvider) rc.Implementation).Owner._loadedCompositors[rc.Handle].AddedRenderers.Version;
		static Renderer GetItem(RendererCompositor rc, int index) => ((LocalRendererCompositorImplProvider) rc.Implementation).Owner._loadedCompositors[rc.Handle].AddedRenderers[index].Renderer;
		
		return new IndirectEnumerable<RendererCompositor, Renderer>(
			HandleToInstance(handle),
			compositorData.AddedRenderers.Version,
			&GetCount,
			&GetVersion,
			&GetItem
		);
	}
	static ResourceHandle<Renderer> GetCompositedRenderHandle(in CompositedRenderer compositedRenderer) {
		return compositedRenderer.RateLimitShimResources is { } resources ? resources.ProxyRenderer.Handle : compositedRenderer.Renderer.Handle;
	}

	public void RenderAll(ResourceHandle<RendererCompositor> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var compositorData = _loadedCompositors[handle];
		if (compositorData.NumRenderersCurrentlyEnabled == 0) return;

		if (compositorData.NumEnabledRateLimitedRenderers > 0) RenderAllRateLimitedRenderers(handle);

		if (compositorData.NumRenderersCurrentlyEnabled == 1) {
			var sole = compositorData.AddedRenderers[compositorData.FirstEnabledRendererIndex];
			RenderInternal(GetCompositedRenderHandle(in sole), RenderOrdering.Standalone, sole.CompositionType, sole.Renderer.Handle);
			return;
		}

		var first = compositorData.AddedRenderers[compositorData.FirstEnabledRendererIndex];
		var last = compositorData.AddedRenderers[compositorData.LastEnabledRendererIndex];
		RenderInternal(GetCompositedRenderHandle(in first), RenderOrdering.First, first.CompositionType, first.Renderer.Handle);
		try {
			for (var i = compositorData.FirstEnabledRendererIndex + 1; i < compositorData.LastEnabledRendererIndex; ++i) {
				var middle = compositorData.AddedRenderers[i];
				if (!middle.IsEnabled) continue;
				RenderInternal(GetCompositedRenderHandle(in middle), RenderOrdering.Middle, middle.CompositionType, middle.Renderer.Handle);
			}
		}
		finally {
			RenderInternal(GetCompositedRenderHandle(in last), RenderOrdering.Last, last.CompositionType, last.Renderer.Handle);
		}
	}

	public void WaitForGpu(ResourceHandle<RendererCompositor> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var compositorData = _loadedCompositors[handle];
		if (compositorData.NumRenderersCurrentlyEnabled == 0) return;
		
		WaitForGpu(compositorData.AddedRenderers[compositorData.LastEnabledRendererIndex].Renderer.Handle);
	}

	#region Rate Limiting
	public void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (maxFramesPerSecond is <= 0) {
			throw new ArgumentOutOfRangeException(nameof(maxFramesPerSecond), maxFramesPerSecond, "Frame rate cap must be positive (or null to remove the cap).");
		}

		var index = GetAddedRendererIndexOrThrow(handle, renderer);
		var addedRenderers = _loadedCompositors[handle].AddedRenderers;
		if (addedRenderers[index].RateLimitHelper.RateCapHz == maxFramesPerSecond) return;

		addedRenderers[index] = addedRenderers[index] with {
			RateLimitHelper = addedRenderers[index].RateLimitHelper.WithFrameCap(maxFramesPerSecond)
		};
		IdempotentlySetUpRateLimitShimResourcesAccordingToConfiguration(handle, index);
		RecalculateCompositorCounts(handle);
	}

	public int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedCompositors[handle].AddedRenderers[GetAddedRendererIndexOrThrow(handle, renderer)].RateLimitHelper.RateCapHz;
	}

	public void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int ratioDenominator) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (ratioDenominator < 1) {
			throw new ArgumentOutOfRangeException(nameof(ratioDenominator), ratioDenominator, "Frame rate ratio must be at least 1 (1 indicates no rate limiting).");
		}

		var index = GetAddedRendererIndexOrThrow(handle, renderer);
		var addedRenderers = _loadedCompositors[handle].AddedRenderers;
		if (addedRenderers[index].RateLimitHelper.RateRatio == ratioDenominator) return;

		addedRenderers[index] = addedRenderers[index] with {
			RateLimitHelper = addedRenderers[index].RateLimitHelper.WithFrameRatio(ratioDenominator)
		};
		IdempotentlySetUpRateLimitShimResourcesAccordingToConfiguration(handle, index);
		RecalculateCompositorCounts(handle);
	}

	public int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedCompositors[handle].AddedRenderers[GetAddedRendererIndexOrThrow(handle, renderer)].RateLimitHelper.RateRatio;
	}

	void IdempotentlySetUpRateLimitShimResourcesAccordingToConfiguration(ResourceHandle<RendererCompositor> handle, int index) {
		var addedRenderers = _loadedCompositors[handle].AddedRenderers;
		var data = addedRenderers[index];

		if (data.IsRateLimited) {
			if (data.RateLimitShimResources != null) return;
			addedRenderers[index] = data with { RateLimitShimResources = AllocateRateLimitShimResources(handle, data.Renderer) };
		}
		else {
			if (data.RateLimitShimResources is not { } resources) return;
			addedRenderers[index] = data with { RateLimitShimResources = null };
			DisposeRateLimitResources(data.Renderer, resources);
		}
	}

	static XYPair<int> GetRateLimitShimBufferSize(RenderTargetUnion renderTarget) {
		return renderTarget.ViewportDimensions.Clamp(
			new XYPair<int>(RenderOutputBufferCreationConfig.MinTextureDimensionXY),
			new XYPair<int>(RenderOutputBufferCreationConfig.MaxTextureDimensionXY)
		);
	}

	RenderOutputBuffer AllocateRateLimitShimBufferAndRenderTargetUnion(XYPair<int> bufferSize) {
		var buffer = CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig {
			TextureDimensions = bufferSize,
			Name = RateLimitBufferName
		});
		AllocateOrRetrieveRenderTargetUnion(buffer);
		return buffer;
	}

	void ReleaseRateLimitShimBuffer(RenderOutputBuffer buffer) {
		ReleaseRenderTargetUnionIfUnused(new RenderTargetUnion(buffer));
		Dispose(buffer.GetHandleWithoutDisposeCheck());
	}

	RateLimitShimResourceData AllocateRateLimitShimResources(ResourceHandle<RendererCompositor> compositorHandle, Renderer renderer) {
		var compositorTarget = _loadedCompositors[compositorHandle].RenderTarget;
		var bufferSize = GetRateLimitShimBufferSize(compositorTarget);
		var privateBuffer = AllocateRateLimitShimBufferAndRenderTargetUnion(bufferSize);

		var proxyScene = _sceneBuilder.CreateCanvasScene(new CanvasSceneCreationConfig { Name = RateLimitSceneName });
		var proxyQuad = proxyScene.Add(privateBuffer.CreateDynamicTexture());
		proxyQuad.CanvasAnchor = Orientation2D.None;
		proxyQuad.WidthFraction = 1f;
		proxyQuad.HeightFraction = 1f;
		proxyQuad.TextureExtentFraction = (1f, -1f);
		proxyQuad.TextureOffsetFraction = (0f, -2f);

		var proxyConfig = new RendererCreationConfig {
			Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Canvas),
			AutoUpdateCameraAspectRatio = false,
			GpuSynchronizationFrameBufferCount = -1,
			Name = RateLimitProxyRendererName
		};
		var proxyRenderer = compositorTarget.IsWindow
			? CreateRenderer(proxyScene, compositorTarget.AsWindow, in proxyConfig)
			: CreateRenderer(proxyScene, compositorTarget.AsBuffer, in proxyConfig);

		RedirectViewToRateLimitBuffer(renderer, privateBuffer);

		return new RateLimitShimResourceData(privateBuffer, proxyRenderer, proxyScene, proxyQuad, bufferSize);
	}

	void RedirectViewToRateLimitBuffer(Renderer renderer, RenderOutputBuffer buffer) {
		SetViewRenderTarget(
			_loadedRenderers[renderer.GetHandleWithoutDisposeCheck()].Viewport.Handle,
			_loadedBuffers[buffer.GetHandleWithoutDisposeCheck()].RenderTargetHandle
		).ThrowIfFailure();
	}

	void DisposeRateLimitResources(Renderer renderer, RateLimitShimResourceData shimResourceData) {
		var rendererHandle = renderer.GetHandleWithoutDisposeCheck();
		if (!IsDisposed(rendererHandle)) {
			SetViewRenderTarget(_loadedRenderers[rendererHandle].Viewport.Handle, UIntPtr.Zero).ThrowIfFailure();
			_loadedRenderers[rendererHandle] = _loadedRenderers[rendererHandle] with { LastPushedCompositingMode = null };
		}

		Dispose(shimResourceData.ProxyRenderer.GetHandleWithoutDisposeCheck());
		shimResourceData.ProxyScene.Dispose();
		ReleaseRateLimitShimBuffer(shimResourceData.PrivateBuffer);
	}

	void RenderAllRateLimitedRenderers(ResourceHandle<RendererCompositor> handle) {
		RateLimitShimResourceData ResizeRateLimitShimResourcesIfNecessary(ResourceHandle<RendererCompositor> compositorHandle, Renderer renderer, RateLimitShimResourceData shimResourceData) {
			var requiredSize = GetRateLimitShimBufferSize(_loadedCompositors[compositorHandle].RenderTarget);
			if (requiredSize == shimResourceData.PrivateBufferSize) return shimResourceData;

			var newBuffer = AllocateRateLimitShimBufferAndRenderTargetUnion(requiredSize);
			shimResourceData.ProxyQuad.SetTexture(newBuffer.CreateDynamicTexture());
			RedirectViewToRateLimitBuffer(renderer, newBuffer);
			ReleaseRateLimitShimBuffer(shimResourceData.PrivateBuffer);

			return shimResourceData with { PrivateBuffer = newBuffer, PrivateBufferSize = requiredSize };
		}
		
		var currentTimestamp = Stopwatch.GetTimestamp();
		var addedRenderers = _loadedCompositors[handle].AddedRenderers;

		for (var i = 0; i < addedRenderers.Count; ++i) {
			var entry = addedRenderers[i];
			if (!entry.IsEnabled || !entry.IsRateLimited) continue;
			if (entry.RateLimitShimResources is not { } resources) {
				throw new InvalidOperationException($"{entry} is part of {HandleToInstance(handle)} and is rate limited but no shim resources are present (this is a bug in TinyFFR).");
			}

			var rateLimitHelper = entry.RateLimitHelper;
			var shouldRender = CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref rateLimitHelper, currentTimestamp);
			entry = entry with { RateLimitHelper = rateLimitHelper };
			if (!shouldRender) {
				addedRenderers[i] = entry;
				continue;
			}

			resources = ResizeRateLimitShimResourcesIfNecessary(handle, entry.Renderer, resources);
			addedRenderers[i] = entry with { RateLimitShimResources = resources };
			RenderRateLimitedShim(entry.Renderer.Handle, resources);
		}
	}

	unsafe void RenderRateLimitedShim(ResourceHandle<Renderer> originalHandle, RateLimitShimResourceData shimResourceData) {
		ThrowIfThisOrHandleIsDisposed(originalHandle);

		SetUpSceneForRender(originalHandle);

		var rendererData = _loadedRenderers[originalHandle];
		var viewportData = rendererData.Viewport;
		RefreshViewportDimensionsIfRenderTargetSizeChanged(originalHandle, ref rendererData, ref viewportData);

		var desiredCompositing = (Translucent: true, ClearDepth: true);
		if (rendererData.LastPushedCompositingMode != desiredCompositing) {
			SetViewCompositingMode(viewportData.Handle, desiredCompositing.Translucent, desiredCompositing.ClearDepth).ThrowIfFailure();
			_loadedRenderers[originalHandle] = rendererData with { LastPushedCompositingMode = desiredCompositing };
		}

		TargetSpecificData offscreenTargetData;
		lock (_loadedTargetDataMutationLock) {
			offscreenTargetData = _loadedTargets[new RenderTargetUnion(shimResourceData.PrivateBuffer)];
		}

		RenderScene(
			offscreenTargetData.RendererPtr,
			viewportData.Handle,
			_loadedBuffers[shimResourceData.PrivateBuffer.GetHandleWithoutDisposeCheck()].RenderTargetHandle,
			true,
			null,
			0U,
			0U,
			0U,
			0U,
			false
		).ThrowIfFailure();
	}
	#endregion
	
	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_view_render_target")]
	static extern InteropResult SetViewRenderTarget(
		UIntPtr viewDescriptorHandle,
		UIntPtr optionalRenderTargetHandle
	);
	#endregion

	#region Resource Directory & Naming
	public string GetNameAsNewStringObject(ResourceHandle<RendererCompositor> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultCompositorName));
	}
	public int GetNameLength(ResourceHandle<RendererCompositor> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultCompositorName).Length;
	}
	public void CopyName(ResourceHandle<RendererCompositor> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultCompositorName, destinationBuffer);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	RendererCompositor HandleToInstance(ResourceHandle<RendererCompositor> h) => new(h, _rendererCompositorImplProvider);
	unsafe IndirectEnumerable<object, RendererCompositor> IResourceDirectory<RendererCompositor>.AllActiveInstances {
		get {
			static LocalRendererBuilder CastSelf(object self) => self as LocalRendererBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._loadedCompositors.Count;
			static int GetVersion(object self) => CastSelf(self)._loadedCompositors.Version;
			static RendererCompositor GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._loadedCompositors.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	bool IResourceDirectory<RendererCompositor>.ResourceNameMatchIsMatching(RendererCompositor resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultCompositorName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultCompositorName).Equals(name, comparisonType);
	}
	#endregion

	#region Disposal
	public bool IsDisposed(ResourceHandle<RendererCompositor> handle) => _isDisposed || !_loadedCompositors.ContainsKey(handle);
	public void Dispose(ResourceHandle<RendererCompositor> handle) => Dispose(handle, false);
	public void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));

		var data = _loadedCompositors[handle];
		for (var i = data.AddedRenderers.Count - 1; i >= 0; --i) {
			var addedRenderer = data.AddedRenderers[i];
			if (addedRenderer.RateLimitShimResources is { } resources) {
				data.AddedRenderers[i] = addedRenderer with { RateLimitShimResources = null };
				DisposeRateLimitResources(addedRenderer.Renderer, resources);
			}
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), addedRenderer.Renderer);
			if (disposeContainedRenderers) Dispose(addedRenderer.Renderer.GetHandleWithoutDisposeCheck());
		}

		if (data.RenderTarget.IsWindow) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.RenderTarget.AsWindow);
		if (data.RenderTarget.IsBuffer) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.RenderTarget.AsBuffer);

		_compositedRendererVectorPool.Return(data.AddedRenderers);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		_loadedCompositors.Remove(handle);
		ReleaseRenderTargetUnionIfUnused(data.RenderTarget);
	}
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<RendererCompositor> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(RendererCompositor));
	#endregion
}
