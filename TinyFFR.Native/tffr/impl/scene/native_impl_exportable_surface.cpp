#include "pch.h"
#include "scene/native_impl_exportable_surface.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/Engine.h"

void native_impl_exportable_surface::get_exportable_surface_capabilities(int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid) {
	ThrowIfNull(outHandleType, "Handle type out pointer was null.");
	ThrowIfNull(outSyncKind, "Sync kind out pointer was null.");
	ThrowIfNull(outDeviceUuid, "Device UUID out pointer was null.");
	*outHandleType = EXPORTABLE_SURFACE_HANDLE_TYPE_NONE;
	*outSyncKind = EXPORTABLE_SURFACE_SYNC_KIND_NONE;
	exportable_surface_backend::get_capabilities(outHandleType, outSyncKind, outDeviceUuid);
}
StartExportedFunc(get_exportable_surface_capabilities, int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid) {
	native_impl_exportable_surface::get_exportable_surface_capabilities(outHandleType, outSyncKind, outDeviceUuid);
	EndExportedFunc
}

void native_impl_exportable_surface::allocate_exportable_surface_descriptor(uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor) {
	ThrowIfNull(outDescriptor, "Descriptor out pointer was null.");
	*outDescriptor = exportable_surface_backend::allocate_descriptor(width, height);
}
StartExportedFunc(allocate_exportable_surface_descriptor, uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor) {
	native_impl_exportable_surface::allocate_exportable_surface_descriptor(width, height, outDescriptor);
	EndExportedFunc
}

void native_impl_exportable_surface::set_exportable_surface_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIf(width == 0 || height == 0, "Exportable surface dimensions must be positive.");
	exportable_surface_backend::set_descriptor_size(descriptor, width, height);
}
StartExportedFunc(set_exportable_surface_descriptor_size, ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	native_impl_exportable_surface::set_exportable_surface_descriptor_size(descriptor, width, height);
	EndExportedFunc
}

void native_impl_exportable_surface::dispose_exportable_surface_descriptor(ExportableSurfaceDescriptorHandle descriptor) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	exportable_surface_backend::dispose_descriptor(descriptor);
}
StartExportedFunc(dispose_exportable_surface_descriptor, ExportableSurfaceDescriptorHandle descriptor) {
	native_impl_exportable_surface::dispose_exportable_surface_descriptor(descriptor);
	EndExportedFunc
}

void native_impl_exportable_surface::allocate_exportable_swap_chain(ExportableSurfaceDescriptorHandle descriptor, SwapChainHandle* outSwapChain) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outSwapChain, "Swap chain out pointer was null.");
	*outSwapChain = filament_engine->createSwapChain(static_cast<void*>(descriptor), 0UL);
	ThrowIfNull(*outSwapChain, "Could not create exportable swap chain.");
	filament_engine->flushAndWait();
}
StartExportedFunc(allocate_exportable_swap_chain, ExportableSurfaceDescriptorHandle descriptor, SwapChainHandle* outSwapChain) {
	native_impl_exportable_surface::allocate_exportable_swap_chain(descriptor, outSwapChain);
	EndExportedFunc
}

void native_impl_exportable_surface::get_exportable_surface_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outGeneration, "Generation out pointer was null.");
	ThrowIfNull(outIsLive, "Is live out pointer was null.");
	ThrowIfNull(outWidth, "Width out pointer was null.");
	ThrowIfNull(outHeight, "Height out pointer was null.");
	exportable_surface_backend::get_generation(descriptor, outGeneration, outIsLive, outWidth, outHeight);
}
StartExportedFunc(get_exportable_surface_generation, ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight) {
	native_impl_exportable_surface::get_exportable_surface_generation(descriptor, outGeneration, outIsLive, outWidth, outHeight);
	EndExportedFunc
}

void native_impl_exportable_surface::export_exportable_surface_image_handles(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle, interop_bool* outFound) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outImageHandle, "Image handle out pointer was null.");
	ThrowIfNull(outImageSizeBytes, "Image size out pointer was null.");
	ThrowIfNull(outReadyHandle, "Ready handle out pointer was null.");
	ThrowIfNull(outReleasedHandle, "Released handle out pointer was null.");
	ThrowIfNull(outFound, "Found out pointer was null.");
	ThrowIf(index >= EXPORTABLE_SURFACE_IMAGE_COUNT, "Image index out of range.");
	*outFound = interop_bool_false;
	if (exportable_surface_backend::export_image_handles(descriptor, generation, index, outImageHandle, outImageSizeBytes, outReadyHandle, outReleasedHandle)) *outFound = interop_bool_true;
}
StartExportedFunc(export_exportable_surface_image_handles, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle, interop_bool* outFound) {
	native_impl_exportable_surface::export_exportable_surface_image_handles(descriptor, generation, index, outImageHandle, outImageSizeBytes, outReadyHandle, outReleasedHandle, outFound);
	EndExportedFunc
}

void native_impl_exportable_surface::close_exportable_surface_handle(int64_t handle) {
	exportable_surface_backend::close_handle(handle);
}
StartExportedFunc(close_exportable_surface_handle, int64_t handle) {
	native_impl_exportable_surface::close_exportable_surface_handle(handle);
	EndExportedFunc
}

void native_impl_exportable_surface::take_latest_presented_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex, interop_bool* outFound) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outIndex, "Index out pointer was null.");
	ThrowIfNull(outFound, "Found out pointer was null.");
	*outFound = interop_bool_false;
	if (exportable_surface_backend::take_latest_presented_image(descriptor, generation, outIndex)) *outFound = interop_bool_true;
}
StartExportedFunc(take_latest_presented_exportable_surface_image, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex, interop_bool* outFound) {
	native_impl_exportable_surface::take_latest_presented_exportable_surface_image(descriptor, generation, outIndex, outFound);
	EndExportedFunc
}

void native_impl_exportable_surface::return_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIf(index >= EXPORTABLE_SURFACE_IMAGE_COUNT, "Image index out of range.");
	exportable_surface_backend::return_image(descriptor, generation, index, consumerSignalledRelease != interop_bool_false);
}
StartExportedFunc(return_exportable_surface_image, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease) {
	native_impl_exportable_surface::return_exportable_surface_image(descriptor, generation, index, consumerSignalledRelease);
	EndExportedFunc
}
