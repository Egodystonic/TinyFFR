#include "pch.h"
#include "scene/native_impl_render.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/SwapChain.h"
#include "filament/View.h"
#include "filament/Viewport.h"
#include "filament/Fence.h"

#include "sdl/SDL_syswm.h"

#ifdef WIN32
#include <windows.h>
extern "C" {
	__declspec(dllexport) DWORD NvOptimusEnablement = 0x00000001;
	__declspec(dllexport) int AmdPowerXpressRequestHighPerformance = 1;
}
#endif

using namespace utils;

void native_impl_render::allocate_renderer_and_swap_chain(WindowHandle window, RendererHandle* outRenderer, SwapChainHandle* outSwapChain) {
	ThrowIfNull(window, "Window was null.");
	ThrowIfNull(outRenderer, "Renderer out pointer was null.");
	ThrowIfNull(outSwapChain, "Swap chain out pointer was null.");

	SDL_SysWMinfo wmInfo;
	SDL_VERSION(&wmInfo.version);
	SDL_GetWindowWMInfo(window, &wmInfo);
	void* hwnd = wmInfo.info.win.window;

	// // We use the createSwapChain overload that does not take a window handle as it creates a "headless" swap chain
	// // See here for more info: https://github.com/google/filament/issues/1921#issuecomment-638165840
	// int32_t windowWidth, windowHeight;
	// native_impl_window::get_window_size(window, &windowWidth, &windowHeight);
	// *outSwapChain = filament_engine->createSwapChain(windowWidth, windowHeight, 0UL);
	*outSwapChain = filament_engine->createSwapChain(hwnd, 0U);
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

