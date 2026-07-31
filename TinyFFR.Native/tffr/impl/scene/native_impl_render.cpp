#include "pch.h"
#include "scene/native_impl_render.h"
#include "scene/native_impl_render.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/SwapChain.h"
#include "filament/View.h"
#include "filament/Viewport.h"
#include "filament/Fence.h"
#include "filament/RenderTarget.h"
#include "filament/TransformManager.h"

#include "sdl/SDL_syswm.h"

#if defined(TFFR_MACOS)

extern "C" void* macos_get_cocoa_view(NSWindow* nsWindow);

#elif defined(TFFR_WIN)

#undef OPAQUE

#endif

using namespace utils;

static void handle_filament_buffer_ready_callback(void* _, size_t __, BufferIdentity identity) {
	native_impl_init::deallocation_delegate(identity);
}

void native_impl_render::allocate_swap_chain(WindowHandle window, SwapChainHandle* outSwapChain) {
	ThrowIfNull(window, "Window was null.");
	ThrowIfNull(outSwapChain, "Swap chain out pointer was null.");

	SDL_SysWMinfo wmInfo;
	SDL_VERSION(&wmInfo.version);
	SDL_GetWindowWMInfo(window, &wmInfo);
#if defined(TFFR_WIN)
	void* hwnd = wmInfo.info.win.window;
	*outSwapChain = filament_engine->createSwapChain(hwnd, 0UL);
#elif defined(TFFR_LINUX)
	switch (wmInfo.subsystem) {
		case SDL_SYSWM_WAYLAND: {
			int width = 0;
			int height = 0;
			SDL_GetWindowSizeInPixels(window, &width, &height);

			static struct {
				struct wl_display *display;
				struct wl_surface *surface;
				uint32_t width;
				uint32_t height;
			} wayland;
			wayland.display = wmInfo.info.wl.display;
			wayland.surface = wmInfo.info.wl.surface;
			wayland.width = static_cast<uint32_t>(width);
			wayland.height = static_cast<uint32_t>(height);
			*outSwapChain = filament_engine->createSwapChain((void *) &wayland, 0UL);
			break;
		}
		case SDL_SYSWM_X11: { 
			Throw("Unfortunately X11 is no longer supported by TinyFFR. Wayland will be supported going forward.");
		}
		default: {
			Throw("Unknown windowing compositor.");
		}
	}
#elif defined(TFFR_MACOS)
	NSWindow* cocoaWindow = wmInfo.info.cocoa.window;
	*outSwapChain = filament_engine->createSwapChain(macos_get_cocoa_view(cocoaWindow), 0UL);
#else
	Throw("TinyFFR was built with no platform identification directive.")
#endif

	ThrowIfNull(*outSwapChain, "Could not create swap chain.");
}
StartExportedFunc(allocate_swap_chain, WindowHandle window, SwapChainHandle* outSwapChain) {
	native_impl_render::allocate_swap_chain(window, outSwapChain);
	EndExportedFunc
}
void native_impl_render::allocate_renderer(RendererHandle* outRenderer) {
	ThrowIfNull(outRenderer, "Renderer out pointer was null.");

	*outRenderer = filament_engine->createRenderer();
	(*outRenderer)->setClearOptions({ { 0.0, 0.0, 0.0, 0.0 }, 0U, true, true });
	ThrowIfNull(*outRenderer, "Could not create renderer.");
}
StartExportedFunc(allocate_renderer, RendererHandle* outRenderer) {
	native_impl_render::allocate_renderer(outRenderer);
	EndExportedFunc
}

void native_impl_render::allocate_view_descriptor(SceneHandle scene, CameraHandle camera, RenderTargetHandle optionalRenderTarget, ViewDescriptorHandle* outViewDescriptor) {
	ThrowIfNull(scene, "Scene was null.");
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(outViewDescriptor, "View descriptor out pointer was null.");

	*outViewDescriptor = filament_engine->createView();
	ThrowIfNull(*outViewDescriptor, "Could not create view descriptor.");
	(*outViewDescriptor)->setCamera(camera);
	(*outViewDescriptor)->setScene(scene);
	if (optionalRenderTarget != nullptr) {
		(*outViewDescriptor)->setRenderTarget(optionalRenderTarget);
	}
	(*outViewDescriptor)->setFrustumCullingEnabled(true);
	filament_engine->getTransformManager().create((*outViewDescriptor)->getFogEntity());
}
StartExportedFunc(allocate_view_descriptor, SceneHandle scene, CameraHandle camera, RenderTargetHandle optionalRenderTarget, ViewDescriptorHandle* outViewDescriptor) {
	native_impl_render::allocate_view_descriptor(scene, camera, optionalRenderTarget, outViewDescriptor);
	EndExportedFunc
}

void native_impl_render::dispose_view_descriptor(ViewDescriptorHandle viewDescriptor) {
	ThrowIfNull(viewDescriptor, "View was null.");
	auto& tcm = filament_engine->getTransformManager();
	auto fogEntity = viewDescriptor->getFogEntity();
	if (tcm.hasComponent(fogEntity)) tcm.destroy(fogEntity);
	ThrowIf(!filament_engine->destroy(viewDescriptor), "Could not destroy view descriptor.");
}
StartExportedFunc(dispose_view_descriptor, ViewDescriptorHandle viewDescriptor) {
	native_impl_render::dispose_view_descriptor(viewDescriptor);
	EndExportedFunc
}

void native_impl_render::dispose_renderer(RendererHandle renderer) {
	ThrowIfNull(renderer, "Renderer was null.");

	ThrowIf(!filament_engine->destroy(renderer), "Could not destroy renderer.");
}
StartExportedFunc(dispose_renderer, RendererHandle renderer) {
	native_impl_render::dispose_renderer(renderer);
	EndExportedFunc
}
void native_impl_render::dispose_swap_chain(SwapChainHandle swapChain) {
	ThrowIfNull(swapChain, "Swap chain was null.");

	ThrowIf(!filament_engine->destroy(swapChain), "Could not destroy swap chain.");
}
StartExportedFunc(dispose_swap_chain, SwapChainHandle swapChain) {
	native_impl_render::dispose_swap_chain(swapChain);
	EndExportedFunc
}

void native_impl_render::set_view_descriptor_size(ViewDescriptorHandle viewDescriptor, int32_t x, int32_t y, uint32_t width, uint32_t height) {
	ThrowIfNull(viewDescriptor, "View was null.");
	viewDescriptor->setViewport({ x, y, width, height });
}
StartExportedFunc(set_view_descriptor_size, ViewDescriptorHandle viewDescriptor, int32_t x, int32_t y, uint32_t width, uint32_t height) {
	native_impl_render::set_view_descriptor_size(viewDescriptor, x, y, width, height);
	EndExportedFunc
}

void native_impl_render::set_view_compositing_mode(ViewDescriptorHandle viewDescriptor, interop_bool blendTranslucent, interop_bool clearDepth) {
	ThrowIfNull(viewDescriptor, "View was null.");
	viewDescriptor->setBlendMode(blendTranslucent ? filament::BlendMode::TRANSLUCENT : filament::BlendMode::OPAQUE);
	viewDescriptor->setChannelDepthClearEnabled(0, clearDepth);
}
StartExportedFunc(set_view_compositing_mode, ViewDescriptorHandle viewDescriptor, interop_bool blendTranslucent, interop_bool clearDepth) {
	native_impl_render::set_view_compositing_mode(viewDescriptor, blendTranslucent, clearDepth);
	EndExportedFunc
}

// Maps a TinyFFR Quality level (-2 VeryLow .. +2 VeryHigh) onto a Filament QualityLevel, with the given level treated as MEDIUM==Standard.
static QualityLevel quality_level_from_tinyffr(int32_t level) {
	switch (level) {
		case -2: return QualityLevel::LOW;
		case -1: return QualityLevel::LOW;
		case 1:  return QualityLevel::HIGH;
		case 2:  return QualityLevel::ULTRA;
		default: return QualityLevel::MEDIUM;
	}
}

void native_impl_render::set_view_quality_configuration(
	ViewDescriptorHandle viewDescriptor,
	int32_t shadowFidelityLevel,
	int32_t screenSpaceEffectsLevel,
	int32_t antiAliasingMode,
	int32_t ambientOcclusionQuality,
	interop_bool postProcessingEnabled,
	float internalResolutionScalar,
	int32_t hdrColorPrecision,
	interop_bool shadowsEnabled,
	int32_t bloomQuality,
	interop_bool dithering,
	interop_bool guardBand
) {
	ThrowIfNull(viewDescriptor, "View was null.");

	// TODO PCSS doesn't seem very stable, it may be that we need to better configure the frustum/FOV before exposing shadow type selection.
	viewDescriptor->setShadowingEnabled(shadowsEnabled);
	viewDescriptor->setShadowType(ShadowType::VSM);

	switch (screenSpaceEffectsLevel) {
		case 2: {
			ScreenSpaceReflectionsOptions ssro { .maxDistance = 6.0, .stride = 2.0, .enabled = true };
			viewDescriptor->setScreenSpaceReflectionsOptions(ssro);
			break;
		}
		case 1: {
			ScreenSpaceReflectionsOptions ssro { .maxDistance = 3.0, .stride = 8.0, .enabled = true };
			viewDescriptor->setScreenSpaceReflectionsOptions(ssro);
			break;
		}
		default: {
			ScreenSpaceReflectionsOptions ssro { .enabled = false };
			viewDescriptor->setScreenSpaceReflectionsOptions(ssro);
			break;
		}
	}

	{
		TemporalAntiAliasingOptions taa;
		switch (antiAliasingMode) {
			case 0: // None
				viewDescriptor->setAntiAliasing(AntiAliasing::NONE);
				taa.enabled = false;
				break;
			case 2: // Taa
				viewDescriptor->setAntiAliasing(AntiAliasing::NONE);
				taa.enabled = true;
				break;
			default: // Fxaa
				viewDescriptor->setAntiAliasing(AntiAliasing::FXAA);
				taa.enabled = false;
				break;
		}
		viewDescriptor->setTemporalAntiAliasingOptions(taa);
	}

	{
		AmbientOcclusionOptions ao;
		if (ambientOcclusionQuality <= -2) {
			ao.enabled = false;
		}
		else {
			ao.enabled = true;
			ao.quality = quality_level_from_tinyffr(ambientOcclusionQuality);
		}
		viewDescriptor->setAmbientOcclusionOptions(ao);
	}

	{
		BloomOptions bo;
		bo.enabled = true;
		switch (bloomQuality) {
			case -2:
			case -1:  bo.quality = QualityLevel::LOW; break;
			case 1:  bo.quality = QualityLevel::HIGH; break;
			case 2:  bo.quality = QualityLevel::ULTRA; break;
			default: bo.quality = QualityLevel::MEDIUM; break;
		}
		viewDescriptor->setBloomOptions(bo);
	}

	{
		RenderQuality rq;
		switch (hdrColorPrecision) {
			case -2: rq.hdrColorBuffer = QualityLevel::LOW; break;
			case -1: rq.hdrColorBuffer = QualityLevel::MEDIUM; break;
			case 2:  rq.hdrColorBuffer = QualityLevel::ULTRA; break;
			default: rq.hdrColorBuffer = QualityLevel::HIGH; break;
		}
		viewDescriptor->setRenderQuality(rq);
	}

	{
		DynamicResolutionOptions dr;
		if (internalResolutionScalar >= 1.0f) {
			dr.enabled = false;
		}
		else {
			if (internalResolutionScalar < 0.1f) internalResolutionScalar = 0.1f;
			dr.enabled = true;
			dr.minScale = { internalResolutionScalar, internalResolutionScalar };
			dr.maxScale = { internalResolutionScalar, internalResolutionScalar };
			dr.quality = QualityLevel::HIGH;
		}
		viewDescriptor->setDynamicResolutionOptions(dr);
	}

	viewDescriptor->setDithering(dithering ? Dithering::TEMPORAL : Dithering::NONE);

	{
		GuardBandOptions gb;
		gb.enabled = guardBand;
		viewDescriptor->setGuardBandOptions(gb);
	}

	viewDescriptor->setPostProcessingEnabled(postProcessingEnabled);
}
StartExportedFunc(
	set_view_quality_configuration,
	ViewDescriptorHandle viewDescriptor,
	int32_t shadowFidelityLevel,
	int32_t screenSpaceEffectsLevel,
	int32_t antiAliasingMode,
	int32_t ambientOcclusionQuality,
	interop_bool postProcessingEnabled,
	float internalResolutionScalar,
	int32_t hdrColorPrecision,
	interop_bool shadowsEnabled,
	int32_t bloomQuality,
	interop_bool dithering,
	interop_bool guardBand
) {
	native_impl_render::set_view_quality_configuration(
		viewDescriptor,
		shadowFidelityLevel,
		screenSpaceEffectsLevel,
		antiAliasingMode,
		ambientOcclusionQuality,
		postProcessingEnabled,
		internalResolutionScalar,
		hdrColorPrecision,
		shadowsEnabled,
		bloomQuality,
		dithering,
		guardBand
	);
	EndExportedFunc
}

void native_impl_render::set_view_frustum_culling_enabled(ViewDescriptorHandle viewDescriptor, interop_bool enabled) {
	ThrowIfNull(viewDescriptor, "View was null.");
	viewDescriptor->setFrustumCullingEnabled(enabled);
}
StartExportedFunc(set_view_frustum_culling_enabled, ViewDescriptorHandle viewDescriptor, interop_bool enabled) {
	native_impl_render::set_view_frustum_culling_enabled(viewDescriptor, enabled);
	EndExportedFunc
}

void native_impl_render::set_view_fog(
	ViewDescriptorHandle viewDescriptor,
	interop_bool enabled,
	float colorR,
	float colorG,
	float colorB,
	float density,
	float startDistance,
	float height,
	float heightFalloff,
	float maximumOpacity,
	float inScatteringSize,
	float inScatteringStart,
	interop_bool colorFromIbl,
	mat4f* fogTransformMatPtr
) {
	ThrowIfNull(viewDescriptor, "View was null.");
	FogOptions fo;
	fo.enabled = enabled;
	fo.color = { colorR, colorG, colorB };
	fo.density = density;
	fo.distance = startDistance;
	fo.height = height;
	fo.heightFalloff = heightFalloff;
	fo.maximumOpacity = maximumOpacity;
	fo.fogColorFromIbl = colorFromIbl;
	fo.inScatteringStart = inScatteringStart;
	fo.inScatteringSize = inScatteringSize;
	viewDescriptor->setFogOptions(fo);
	auto& manager = filament_engine->getTransformManager();
	manager.setTransform(manager.getInstance(viewDescriptor->getFogEntity()), *fogTransformMatPtr);
}
StartExportedFunc(
	set_view_fog,
	ViewDescriptorHandle viewDescriptor,
	interop_bool enabled,
	float colorR,
	float colorG,
	float colorB,
	float density,
	float startDistance,
	float height,
	float heightFalloff,
	float maximumOpacity,
	float inScatteringSize,
	float inScatteringStart,
	interop_bool colorFromIbl,
	mat4f* fogTransformMatPtr
) {
	native_impl_render::set_view_fog(
		viewDescriptor,
		enabled,
		colorR,
		colorG,
		colorB,
		density,
		startDistance,
		height,
		heightFalloff,
		maximumOpacity,
		inScatteringSize,
		inScatteringStart,
		colorFromIbl,
		fogTransformMatPtr
	);
	EndExportedFunc
}


void native_impl_render::allocate_render_target(int32_t width, int32_t height, TextureHandle* outBuffer, RenderTargetHandle* outRenderTarget) {
	ThrowIfNull(outBuffer, "Buffer out pointer was null.");
	ThrowIfNull(outRenderTarget, "Render target out pointer was null.");

	*outBuffer = Texture::Builder()
		.depth(1)
		.format(Texture::InternalFormat::RGBA8)
		.height(height)
		.sampler(Texture::Sampler::SAMPLER_2D)
		.usage(Texture::Usage::COLOR_ATTACHMENT | Texture::Usage::SAMPLEABLE | Texture::Usage::BLIT_SRC | Texture::Usage::BLIT_DST)
		.width(width)
		.build(*filament_engine);
	ThrowIfNull(*outBuffer, "Could not create buffer tetxure.");

	*outRenderTarget = RenderTarget::Builder()
		.texture(RenderTarget::AttachmentPoint::COLOR, *outBuffer)
		.build(*filament_engine);

	if (*outRenderTarget == nullptr) {
		filament_engine->destroy(*outBuffer);
		Throw("Could not create render target.");
	}
}
StartExportedFunc(allocate_render_target, int32_t width, int32_t height, TextureHandle* outBuffer, RenderTargetHandle* outRenderTarget) {
	native_impl_render::allocate_render_target(width, height, outBuffer, outRenderTarget);
	EndExportedFunc
}

void native_impl_render::dispose_render_target_buffer(TextureHandle buffer) {
	ThrowIfNull(buffer, "Buffer was null.");
	ThrowIf(!filament_engine->destroy(buffer), "Could not destroy buffer.");
}
StartExportedFunc(dispose_render_target_buffer, TextureHandle buffer) {
	native_impl_render::dispose_render_target_buffer(buffer);
	EndExportedFunc
}

void native_impl_render::dispose_render_target(RenderTargetHandle renderTarget) {
	ThrowIfNull(renderTarget, "Render target was null.");
	ThrowIf(!filament_engine->destroy(renderTarget), "Could not destroy render target.");
}
StartExportedFunc(dispose_render_target, RenderTargetHandle renderTarget) {
	native_impl_render::dispose_render_target(renderTarget);
	EndExportedFunc
}




void native_impl_render::render_scene(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, SwapChainHandle swapChain, interop_bool invokeBeginFrame, interop_bool invokeEndFrame) {
	ThrowIfNull(renderer, "Renderer was null.");
	ThrowIfNull(viewDescriptor, "View was null.");
	ThrowIfNull(swapChain, "Swap chain pointer was null.");

	//if (!renderer->beginFrame(swapChain)) return; // We do our own synchronization external to filament so we ignore this
	if (invokeBeginFrame) renderer->beginFrame(swapChain);
	renderer->render(viewDescriptor);
	if (invokeEndFrame) renderer->endFrame();
}
StartExportedFunc(render_scene, RendererHandle renderer, ViewDescriptorHandle viewDescriptor, SwapChainHandle swapChain, interop_bool invokeBeginFrame, interop_bool invokeEndFrame) {
	native_impl_render::render_scene(renderer, viewDescriptor, swapChain, invokeBeginFrame, invokeEndFrame);
	EndExportedFunc
}

void native_impl_render::render_scene_standalone(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, RenderTargetHandle renderTarget, interop_bool clearAndDiscard, uint8_t* optionalReadbackBuffer, uint32_t readbackBufferLenBytes, uint32_t readbackBufferWidth, uint32_t readbackBufferHeight, BufferIdentity bufferIdentity) {
	ThrowIfNull(renderer, "Renderer was null.");
	ThrowIfNull(viewDescriptor, "View was null.");
	ThrowIfNull(renderTarget, "Render target pointer was null.");

	// renderStandaloneView opens its own implicit beginFrame/endFrame, so ClearOptions must be set per call to preserve the buffer across composited sub-renders.
	renderer->setClearOptions({ { 0.0, 0.0, 0.0, 0.0 }, 0U, static_cast<bool>(clearAndDiscard), static_cast<bool>(clearAndDiscard) });
	renderer->renderStandaloneView(viewDescriptor);
	if (optionalReadbackBuffer == nullptr) return;

	renderer->readPixels(
		renderTarget,
		0,
		0,
		readbackBufferWidth,
		readbackBufferHeight,
		backend::PixelBufferDescriptor {
			optionalReadbackBuffer,
			static_cast<size_t>(readbackBufferLenBytes),
			backend::PixelDataFormat::RGB,
			backend::PixelDataType::UBYTE,
			&handle_filament_buffer_ready_callback,
			bufferIdentity
		}
	);
	filament_engine->flushAndWait();
}
StartExportedFunc(render_scene_standalone, RendererHandle renderer, ViewDescriptorHandle viewDescriptor, RenderTargetHandle renderTarget, interop_bool clearAndDiscard, uint8_t* optionalReadbackBuffer, uint32_t readbackBufferLenBytes, uint32_t readbackBufferWidth, uint32_t readbackBufferHeight, BufferIdentity bufferIdentity) {
	native_impl_render::render_scene_standalone(renderer, viewDescriptor, renderTarget, clearAndDiscard, optionalReadbackBuffer, readbackBufferLenBytes, readbackBufferWidth, readbackBufferHeight, bufferIdentity);
	EndExportedFunc
}

void native_impl_render::create_gpu_fence(FenceHandle* outFence) {
	*outFence = filament_engine->createFence();
	ThrowIfNull((*outFence), "Could not create engine fence.");
}
StartExportedFunc(create_gpu_fence, FenceHandle* outFence) {
	native_impl_render::create_gpu_fence(outFence);
	EndExportedFunc
}

void native_impl_render::wait_for_fence(FenceHandle fenceHandle) {
	ThrowIfNull(fenceHandle, "Fence handle was null.");
	ThrowIf(filament::Fence::waitAndDestroy(fenceHandle) != filament::backend::FenceStatus::CONDITION_SATISFIED, "Fence did not complete without error.");
}
StartExportedFunc(wait_for_fence, FenceHandle fenceHandle) {
	native_impl_render::wait_for_fence(fenceHandle);
	EndExportedFunc
}

void native_impl_render::stall_for_pending_callbacks() {
	filament_engine->flushAndWait();
}
StartExportedFunc(stall_for_pending_callbacks) {
	native_impl_render::stall_for_pending_callbacks();
	EndExportedFunc
}


