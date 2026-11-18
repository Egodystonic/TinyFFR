#pragma once

#include "utils_and_constants.h"
#include "native_impl_render.h"

#define EXPORTABLE_SURFACE_HANDLE_TYPE_NONE 0
#define EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD 1

#define EXPORTABLE_SURFACE_SYNC_KIND_NONE 0
#define EXPORTABLE_SURFACE_SYNC_KIND_VULKAN_OPAQUE_FD_SEMAPHORES 1

#define EXPORTABLE_SURFACE_IMAGE_COUNT 5
#define EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH 16

struct exportable_surface_descriptor;
typedef exportable_surface_descriptor* ExportableSurfaceDescriptorHandle;

namespace exportable_surface_backend {
	void get_capabilities(int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid);
	ExportableSurfaceDescriptorHandle allocate_descriptor(uint32_t width, uint32_t height);
	void set_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height);
	void dispose_descriptor(ExportableSurfaceDescriptorHandle descriptor);
	void get_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight);
	bool export_image_handles(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle);
	void close_handle(int64_t handle);
	bool take_latest_presented_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex);
	void return_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, bool consumerSignalledRelease);
}

class native_impl_exportable_surface {
public:
	static void get_exportable_surface_capabilities(int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid);
	static void allocate_exportable_surface_descriptor(uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor);
	static void set_exportable_surface_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height);
	static void dispose_exportable_surface_descriptor(ExportableSurfaceDescriptorHandle descriptor);
	static void allocate_exportable_swap_chain(ExportableSurfaceDescriptorHandle descriptor, SwapChainHandle* outSwapChain);
	static void get_exportable_surface_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight);
	static void export_exportable_surface_image_handles(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle, interop_bool* outFound);
	static void close_exportable_surface_handle(int64_t handle);
	static void take_latest_presented_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex, interop_bool* outFound);
	static void return_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease);
};
