#pragma once

#include "utils_and_constants.h"
#include "filament/Renderer.h"
#include "native_impl_scene.h"
#include "camera/native_impl_camera.h"
#include "environment/native_impl_window.h"

using namespace filament;

typedef Renderer* RendererHandle;
typedef SwapChain* SwapChainHandle;
typedef View* ViewDescriptorHandle;
typedef filament::Fence* FenceHandle;
typedef RenderTarget* RenderTargetHandle;

class native_impl_render {
public:
	static void allocate_swap_chain(WindowHandle window, SwapChainHandle* outSwapChain);
	static void allocate_renderer(RendererHandle* outRenderer);
	static void allocate_view_descriptor(SceneHandle scene, CameraHandle camera, RenderTargetHandle optionalRenderTarget, ViewDescriptorHandle* outViewDescriptor);
	static void dispose_view_descriptor(ViewDescriptorHandle viewDescriptor);
	static void dispose_swap_chain(SwapChainHandle swapChain);
	static void dispose_renderer(RendererHandle renderer);

	static void set_view_descriptor_size(ViewDescriptorHandle viewDescriptor, int32_t x, int32_t y, uint32_t width, uint32_t height);
	static void set_view_compositing_mode(ViewDescriptorHandle viewDescriptor, interop_bool blendTranslucent, interop_bool clearDepth);
	static void set_view_render_target(ViewDescriptorHandle viewDescriptor, RenderTargetHandle optionalRenderTarget);
	static void render_scene(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, SwapChainHandle swapChain, interop_bool invokeBeginFrame, interop_bool invokeEndFrame);
	static void render_scene_standalone(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, RenderTargetHandle renderTarget, interop_bool clearAndDiscard, uint8_t* optionalReadbackBuffer, uint32_t readbackBufferLenBytes, uint32_t readbackBufferWidth, uint32_t readbackBufferHeight, BufferIdentity bufferIdentity, interop_bool waitForReadbackCompletion);

	static void set_view_quality_configuration(
		ViewDescriptorHandle viewDescriptor,
		int32_t shadowFidelityLevel,
		int32_t screenSpaceEffectsLevel,
		int32_t antiAliasingMode,
		int32_t ambientOcclusionQuality,
		float_t ambientOcclusionStrength,
		interop_bool postProcessingEnabled,
		float_t internalResolutionScalar,
		int32_t hdrColorPrecision,
		interop_bool shadowsEnabled,
		int32_t bloomQuality,
		float_t bloomStrength,
		int32_t depthOfFieldQuality,
		float_t depthOfFieldStrength,
		interop_bool dithering,
		interop_bool guardBand
	);
	static void set_view_frustum_culling_enabled(ViewDescriptorHandle viewDescriptor, interop_bool enabled);
	static void set_view_depth_of_field_enabled(ViewDescriptorHandle viewDescriptor, interop_bool enabled);
	static void set_view_fog(
		ViewDescriptorHandle viewDescriptor,
		interop_bool enabled,
		float_t colorR,
		float_t colorG,
		float_t colorB,
		float_t density,
		float_t startDistance,
		float_t height,
		float_t heightFalloff,
		float_t maximumOpacity,
		float_t inScatteringSize,
		float_t inScatteringStart,
		interop_bool colorFromIbl,
		mat4f* fogTransformMatPtr
	);

	static void allocate_render_target(int32_t width, int32_t height, TextureHandle* outBuffer, RenderTargetHandle* outRenderTarget);
	static void dispose_render_target_buffer(TextureHandle buffer);
	static void dispose_render_target(RenderTargetHandle renderTarget);

	static void create_gpu_fence(FenceHandle* outFence);
	static void wait_for_fence(FenceHandle fenceHandle);
	static void stall_for_pending_callbacks();
};