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

#include "sdl/SDL_syswm.h"

#if defined(TFFR_MACOS)

extern "C" void* macos_get_cocoa_view(NSWindow* nsWindow);

#endif

using namespace utils;

void native_impl_render::allocate_renderer_and_swap_chain(WindowHandle window, RendererHandle* outRenderer, SwapChainHandle* outSwapChain) {
	ThrowIfNull(window, "Window was null.");
	ThrowIfNull(outRenderer, "Renderer out pointer was null.");
	ThrowIfNull(outSwapChain, "Swap chain out pointer was null.");

	SDL_SysWMinfo wmInfo;
	SDL_VERSION(&wmInfo.version);
	SDL_GetWindowWMInfo(window, &wmInfo);
#if defined(TFFR_WIN)
	void* hwnd = wmInfo.info.win.window;
	*outSwapChain = filament_engine->createSwapChain(hwnd, 0UL);
#elif defined(TFFR_LINUX)
	switch (wmInfo.subsystem) {
		case SDL_SYSWM_X11: {
			void* windowHandle = reinterpret_cast<void*>(static_cast<uintptr_t>(wmInfo.info.x11.window));
			*outSwapChain = filament_engine->createSwapChain(windowHandle, 0UL);
			break;
		}
		case SDL_SYSWM_WAYLAND: {
			void* surface = static_cast<void*>(wmInfo.info.wl.surface);
			*outSwapChain = filament_engine->createSwapChain(surface, 0UL);
			break;
		}
		default: {
			Throw("Unknown window manager.");
			break;
		}
	}
#elif defined(TFFR_MACOS)
	NSWindow* cocoaWindow = wmInfo.info.cocoa.window;
	*outSwapChain = filament_engine->createSwapChain(macos_get_cocoa_view(cocoaWindow), 0UL);
#else
	Throw("TinyFFR was built with no platform identification directive.")
#endif

	*outRenderer = filament_engine->createRenderer();
	(*outRenderer)->setClearOptions({ { 0.0, 0.0, 0.0, 0.0 }, 0U, true, true });
	ThrowIfNull(*outSwapChain, "Could not create swap chain.");
	ThrowIfNull(*outRenderer, "Could not create renderer.");
}
StartExportedFunc(allocate_renderer_and_swap_chain, WindowHandle window, RendererHandle* outRenderer, SwapChainHandle* outSwapChain) {
	native_impl_render::allocate_renderer_and_swap_chain(window, outRenderer, outSwapChain);
	EndExportedFunc
}

void native_impl_render::allocate_view_descriptor(SceneHandle scene, CameraHandle camera, ViewDescriptorHandle* outViewDescriptor) {
	ThrowIfNull(scene, "Scene was null.");
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(outViewDescriptor, "View descriptor out pointer was null.");

	*outViewDescriptor = filament_engine->createView();
	ThrowIfNull(*outViewDescriptor, "Could not create view descriptor.");
	(*outViewDescriptor)->setCamera(camera);
	(*outViewDescriptor)->setScene(scene);
	(*outViewDescriptor)->setShadowType(ShadowType::PCSS);
}
StartExportedFunc(allocate_view_descriptor, SceneHandle scene, CameraHandle camera, ViewDescriptorHandle* outViewDescriptor) {
	native_impl_render::allocate_view_descriptor(scene, camera, outViewDescriptor);
	EndExportedFunc
}

void native_impl_render::dispose_view_descriptor(ViewDescriptorHandle viewDescriptor) {
	ThrowIfNull(viewDescriptor, "View was null.");
	ThrowIf(!filament_engine->destroy(viewDescriptor), "Could not destroy view descriptor.");
}
StartExportedFunc(dispose_view_descriptor, ViewDescriptorHandle viewDescriptor) {
	native_impl_render::dispose_view_descriptor(viewDescriptor);
	EndExportedFunc
}

void native_impl_render::dispose_renderer_and_swap_chain(RendererHandle renderer, SwapChainHandle swapChain) {
	ThrowIfNull(renderer, "Renderer was null.");
	ThrowIfNull(swapChain, "Swap chain was null.");

	ThrowIf(!filament_engine->destroy(renderer), "Could not destroy renderer.");
	ThrowIf(!filament_engine->destroy(swapChain), "Could not destroy swap chain.");
}
StartExportedFunc(dispose_renderer_and_swap_chain, RendererHandle renderer, SwapChainHandle swapChain) {
	native_impl_render::dispose_renderer_and_swap_chain(renderer, swapChain);
	EndExportedFunc
}

void native_impl_render::set_view_descriptor_size(ViewDescriptorHandle viewDescriptor, uint32_t width, uint32_t height) {
	ThrowIfNull(viewDescriptor, "View was null.");
	viewDescriptor->setViewport({ 0, 0, width, height });
}
StartExportedFunc(set_view_descriptor_size, ViewDescriptorHandle viewDescriptor, uint32_t width, uint32_t height) {
	native_impl_render::set_view_descriptor_size(viewDescriptor, width, height);
	EndExportedFunc
}

void native_impl_render::set_view_shadow_fidelity_level(ViewDescriptorHandle viewDescriptor, int32_t level) {
	ThrowIfNull(viewDescriptor, "View was null.");
	switch (level) {
		case 1:
		case 2:
			// Looks "worse" (less smooth, more dithery) from some aspects but does not suffer from light bleeding which definitely looks "less bad" in the worst case
			viewDescriptor->setShadowType(ShadowType::PCSS);
			break;
		default:
			viewDescriptor->setShadowType(ShadowType::VSM);
			break;
	}
}
StartExportedFunc(set_view_shadow_fidelity_level, ViewDescriptorHandle viewDescriptor, int32_t level) {
	native_impl_render::set_view_shadow_fidelity_level(viewDescriptor, level);
	EndExportedFunc
}


void native_impl_render::allocate_render_target(int32_t width, int32_t height, TextureHandle* outBuffer, RenderTargetHandle* outRenderTarget) {
	ThrowIfNull(outBuffer, "Buffer out pointer was null.");
	ThrowIfNull(outRenderTarget, "Render target out pointer was null.");

	*outBuffer = Texture::Builder()
		.depth(1)
		.format(Texture::InternalFormat::RGBA8)
		.height(height)
		.levels(1)
		.sampler(Texture::Sampler::SAMPLER_2D)
		.usage(Texture::Usage::COLOR_ATTACHMENT | Texture::Usage::SAMPLEABLE)
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

void native_impl_render::dispose_render_target(TextureHandle buffer, RenderTargetHandle renderTarget) {
	ThrowIfNull(buffer, "Buffer was null.");
	ThrowIfNull(renderTarget, "Render target was null.");
	ThrowIf(!filament_engine->destroy(renderTarget), "Could not destroy render target.");
	ThrowIf(!filament_engine->destroy(buffer), "Could not destroy buffer.");
}
StartExportedFunc(dispose_render_target, TextureHandle buffer, RenderTargetHandle renderTarget) {
	native_impl_render::dispose_render_target(buffer, renderTarget);
	EndExportedFunc
}




void native_impl_render::render_scene(RendererHandle renderer, SwapChainHandle swapChain, ViewDescriptorHandle viewDescriptor) {
	ThrowIfNull(renderer, "Renderer was null.");
	ThrowIfNull(swapChain, "Swap chain was null.");
	ThrowIfNull(viewDescriptor, "View was null.");

	if (!renderer->beginFrame(swapChain)) return;
	renderer->render(viewDescriptor);
	renderer->endFrame();
}
StartExportedFunc(render_scene, RendererHandle renderer, SwapChainHandle swapChain, ViewDescriptorHandle viewDescriptor) {
	native_impl_render::render_scene(renderer, swapChain, viewDescriptor);
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
	ThrowIf(filament::Fence::waitAndDestroy(fenceHandle) != filament::backend::FenceStatus::CONDITION_SATISFIED, "Fence did not complete without error.");
}
StartExportedFunc(wait_for_fence, FenceHandle fenceHandle) {
	native_impl_render::wait_for_fence(fenceHandle);
	EndExportedFunc
}

