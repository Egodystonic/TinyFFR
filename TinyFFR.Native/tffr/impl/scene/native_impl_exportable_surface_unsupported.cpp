#include "pch.h"
#include "scene/native_impl_exportable_surface_vulkan.h"

#if !defined(TFFR_LINUX) && !defined(TFFR_WIN) && !defined(TFFR_MACOS)

struct exportable_surface_descriptor { };

void exportable_surface_backend::get_support(int32_t handleType, interop_bool* outSupported, int32_t* outSyncKind) { }

void exportable_surface_backend::get_device_ids(uint8_t* outDeviceUuid, uint8_t* outDeviceLuid, interop_bool* outLuidValid) { }

ExportableSurfaceDescriptorHandle exportable_surface_backend::allocate_descriptor(uint32_t width, uint32_t height, int32_t handleType) {
	Throw("Exportable surfaces are not supported on this platform.");
}

void exportable_surface_backend::set_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) { }

void exportable_surface_backend::dispose_descriptor(ExportableSurfaceDescriptorHandle descriptor) { }

void exportable_surface_backend::get_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight) {
	*outGeneration = 0;
	*outIsLive = interop_bool_false;
	*outWidth = 0;
	*outHeight = 0;
}

bool exportable_surface_backend::export_image_handles(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	Throw("Exportable surfaces are not supported on this platform.");
}

void exportable_surface_backend::close_handle(int32_t handleType, int64_t handle) { }

bool exportable_surface_backend::take_latest_presented_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex) {
	return false;
}

void exportable_surface_backend::return_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, bool consumerSignalledRelease) { }

SwapChainHandle exportable_surface_backend::allocate_swap_chain(ExportableSurfaceDescriptorHandle descriptor) {
	Throw("Exportable surfaces are not supported on this platform.");
}

bool exportable_surface_backend::dispose_swap_chain_if_owned(SwapChainHandle swapChain) {
	return false;
}

SwapChainHandle exportable_surface_backend::begin_frame_swap_chain(SwapChainHandle swapChain) {
	return swapChain;
}

void exportable_surface_backend::end_frame(SwapChainHandle swapChain) { }

#endif
