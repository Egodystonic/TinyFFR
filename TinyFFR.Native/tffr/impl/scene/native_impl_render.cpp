#include "pch.h"
#include "scene/native_impl_render.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/SwapChain.h"
#include "filament/View.h"
#include "filament/Viewport.h"

#include "sdl/SDL_syswm.h"

using namespace utils;

void native_impl_render::allocate_renderer_and_swap_chain(WindowHandle window, RendererHandle* outRenderer, SwapChainHandle* outSwapChain) {
	SDL_SysWMinfo wmInfo;
	SDL_VERSION(&wmInfo.version);
	SDL_GetWindowWMInfo(window, &wmInfo);
	HWND hwnd = wmInfo.info.win.window;

	*outSwapChain = filament_engine->createSwapChain(hwnd, 0UL);
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
	ThrowIf(!filament_engine->destroy(viewDescriptor), "Could not destroy view descriptor.");
}
StartExportedFunc(dispose_view_descriptor, ViewDescriptorHandle viewDescriptor) {
	native_impl_render::dispose_view_descriptor(viewDescriptor);
	EndExportedFunc
}

void native_impl_render::dispose_renderer_and_swap_chain(RendererHandle renderer, SwapChainHandle swapChain) {
	ThrowIf(!filament_engine->destroy(renderer), "Could not destroy renderer.");
	ThrowIf(!filament_engine->destroy(swapChain), "Could not destroy swap chain.");
}
StartExportedFunc(dispose_renderer_and_swap_chain, RendererHandle renderer, SwapChainHandle swapChain) {
	native_impl_render::dispose_renderer_and_swap_chain(renderer, swapChain);
	EndExportedFunc
}

void native_impl_render::set_view_descriptor_size(ViewDescriptorHandle viewDescriptor, uint32_t width, uint32_t height) {
	viewDescriptor->setViewport({ 0, 0, width, height });
}
StartExportedFunc(set_view_descriptor_size, ViewDescriptorHandle viewDescriptor, uint32_t width, uint32_t height) {
	native_impl_render::set_view_descriptor_size(viewDescriptor, width, height);
	EndExportedFunc
}


void native_impl_render::render_scene(RendererHandle renderer, SwapChainHandle swapChain, ViewDescriptorHandle viewDescriptor) {
	if (!renderer->beginFrame(swapChain)) return;
	renderer->render(viewDescriptor);
	renderer->endFrame();
}
StartExportedFunc(render_scene, RendererHandle renderer, SwapChainHandle swapChain, ViewDescriptorHandle viewDescriptor) {
	native_impl_render::render_scene(renderer, swapChain, viewDescriptor);
	EndExportedFunc
}
