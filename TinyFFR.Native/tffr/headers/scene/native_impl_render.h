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
	static void allocate_renderer_and_swap_chain(WindowHandle window, RendererHandle* outRenderer, SwapChainHandle* outSwapChain);
	static void allocate_renderer(RendererHandle* outRenderer);
	static void allocate_view_descriptor(SceneHandle scene, CameraHandle camera, RenderTargetHandle optionalRenderTarget, ViewDescriptorHandle* outViewDescriptor);
	static void dispose_view_descriptor(ViewDescriptorHandle viewDescriptor);
	static void dispose_swap_chain(SwapChainHandle swapChain);
	static void dispose_renderer(RendererHandle renderer);

	static void set_view_descriptor_size(ViewDescriptorHandle viewDescriptor, uint32_t width, uint32_t height);
	static void render_scene(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, SwapChainHandle swapChain);
	static void render_scene_standalone(RendererHandle renderer, ViewDescriptorHandle viewDescriptor, RenderTargetHandle renderTarget, uint8_t* optionalReadbackBuffer, uint32_t readbackBufferLenBytes, uint32_t readbackBufferWidth, uint32_t readbackBufferHeight, BufferIdentity bufferIdentity);

	static void set_view_shadow_fidelity_level(ViewDescriptorHandle viewDescriptor, int32_t level);

	static void allocate_render_target(int32_t width, int32_t height, TextureHandle* outBuffer, RenderTargetHandle* outRenderTarget);
	static void dispose_render_target_buffer(TextureHandle buffer);
	static void dispose_render_target(RenderTargetHandle renderTarget);

	static void create_gpu_fence(FenceHandle* outFence);
	static void wait_for_fence(FenceHandle fenceHandle);
	static void stall_for_pending_callbacks();
};