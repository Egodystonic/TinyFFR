#include "pch.h"
#include "scene/native_impl_exportable_surface.h"

#if defined(TFFR_MACOS)
#include "native_impl_init.h"
#include "filament/Engine.h"
#include "filament/Fence.h"
#include "filament/SwapChain.h"
#include <CoreFoundation/CoreFoundation.h>
#include <CoreVideo/CoreVideo.h>
#include <IOSurface/IOSurface.h>
#include <deque>
#include <mutex>
#include <unordered_set>

namespace {
	constexpr uint32_t ScratchSlotIndex = EXPORTABLE_SURFACE_IMAGE_COUNT;
	constexpr uint32_t BytesPerPixel = 4U;

	enum class metal_image_state : uint8_t {
		free,
		in_flight,
		presented,
		handed,
		released,
		pending_discard
	};

	struct metal_image_slot {
		IOSurfaceRef surface = nullptr;
		CVPixelBufferRef pixelBuffer = nullptr;
		filament::SwapChain* swapChain = nullptr;
		filament::Fence* completionFence = nullptr;
		metal_image_state state = metal_image_state::free;
	};

	struct metal_swap_chain_proxy {
		exportable_surface_descriptor* descriptor;
	};

	std::mutex proxy_registry_mutex;
	std::unordered_set<metal_swap_chain_proxy*> proxy_registry;

	metal_swap_chain_proxy* as_proxy(SwapChainHandle swapChain) {
		auto candidate = reinterpret_cast<metal_swap_chain_proxy*>(swapChain);
		std::lock_guard lock { proxy_registry_mutex };
		return proxy_registry.contains(candidate) ? candidate : nullptr;
	}

	bool metal_backend_is_active() {
		return filament_engine != nullptr && filament_engine->getBackend() == filament::Engine::Backend::METAL;
	}

	void set_dictionary_int(CFMutableDictionaryRef dictionary, CFStringRef key, int64_t value) {
		auto number = CFNumberCreate(kCFAllocatorDefault, kCFNumberSInt64Type, &value);
		CFDictionarySetValue(dictionary, key, number);
		CFRelease(number);
	}

	bool fence_has_completed(filament::Fence* fence) {
		if (fence == nullptr) return true;
		return fence->wait(filament::Fence::Mode::FLUSH, 0) == filament::backend::FenceStatus::CONDITION_SATISFIED;
	}
}

struct exportable_surface_descriptor {
	std::mutex mutex;
	int32_t handleType;
	uint32_t width;
	uint32_t height;
	uint32_t generation = 0;
	uint32_t liveWidth = 0;
	uint32_t liveHeight = 0;
	bool hasLiveSwapChain = false;
	metal_image_slot slots[EXPORTABLE_SURFACE_IMAGE_COUNT + 1];
	std::deque<uint32_t> presentedQueue;
	uint32_t nextAcquireStartIndex = 0;
	uint32_t currentFrameSlot = ScratchSlotIndex;
	metal_swap_chain_proxy* proxy = nullptr;
};

namespace {
	void create_slot(exportable_surface_descriptor const* descriptor, metal_image_slot& slot) {
		auto surfaceProperties = CFDictionaryCreateMutable(kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
		set_dictionary_int(surfaceProperties, kIOSurfaceWidth, descriptor->liveWidth);
		set_dictionary_int(surfaceProperties, kIOSurfaceHeight, descriptor->liveHeight);
		set_dictionary_int(surfaceProperties, kIOSurfaceBytesPerElement, BytesPerPixel);
		set_dictionary_int(surfaceProperties, kIOSurfaceBytesPerRow, static_cast<int64_t>(IOSurfaceAlignProperty(kIOSurfaceBytesPerRow, descriptor->liveWidth * BytesPerPixel)));
		set_dictionary_int(surfaceProperties, kIOSurfacePixelFormat, kCVPixelFormatType_32BGRA);
		slot.surface = IOSurfaceCreate(surfaceProperties);
		CFRelease(surfaceProperties);
		ThrowIfNull(slot.surface, "Could not create IOSurface for exportable surface image.");

		auto pixelBufferAttributes = CFDictionaryCreateMutable(kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
		CFDictionarySetValue(pixelBufferAttributes, kCVPixelBufferMetalCompatibilityKey, kCFBooleanTrue);
		auto const pixelBufferResult = CVPixelBufferCreateWithIOSurface(kCFAllocatorDefault, slot.surface, pixelBufferAttributes, &slot.pixelBuffer);
		CFRelease(pixelBufferAttributes);
		ThrowIf(pixelBufferResult != kCVReturnSuccess || slot.pixelBuffer == nullptr, "Could not create CVPixelBuffer for exportable surface image.");

		slot.swapChain = filament_engine->createSwapChain(slot.pixelBuffer, filament::SwapChain::CONFIG_APPLE_CVPIXELBUFFER);
		ThrowIfNull(slot.swapChain, "Could not create CVPixelBuffer swap chain for exportable surface image.");
		slot.state = metal_image_state::free;
	}

	void destroy_slot(metal_image_slot& slot) {
		if (slot.completionFence != nullptr) filament_engine->destroy(slot.completionFence);
		if (slot.swapChain != nullptr) filament_engine->destroy(slot.swapChain);
		if (slot.pixelBuffer != nullptr) CVPixelBufferRelease(slot.pixelBuffer);
		if (slot.surface != nullptr) CFRelease(slot.surface);
		slot = {};
	}

	void destroy_all_slots(exportable_surface_descriptor* descriptor) {
		filament_engine->flushAndWait();
		std::lock_guard lock { descriptor->mutex };
		for (auto& slot : descriptor->slots) destroy_slot(slot);
		descriptor->presentedQueue.clear();
		descriptor->hasLiveSwapChain = false;
		descriptor->currentFrameSlot = ScratchSlotIndex;
	}
}

void exportable_surface_backend::get_support(int32_t handleType, interop_bool* outSupported, int32_t* outSyncKind) {
	if (handleType != EXPORTABLE_SURFACE_HANDLE_TYPE_IOSURFACE_REF || !metal_backend_is_active()) return;
	*outSupported = interop_bool_true;
	*outSyncKind = EXPORTABLE_SURFACE_SYNC_KIND_COMPLETED_BEFORE_TAKE;
}

void exportable_surface_backend::get_device_ids(uint8_t* outDeviceUuid, uint8_t* outDeviceLuid, interop_bool* outLuidValid) { }

ExportableSurfaceDescriptorHandle exportable_surface_backend::allocate_descriptor(uint32_t width, uint32_t height, int32_t handleType) {
	ThrowIf(handleType != EXPORTABLE_SURFACE_HANDLE_TYPE_IOSURFACE_REF || !metal_backend_is_active(), "Exportable surfaces with this handle type are not supported on this device.");
	ThrowIf(width == 0 || height == 0, "Exportable surface dimensions must be positive.");
	auto descriptor = new exportable_surface_descriptor();
	descriptor->handleType = handleType;
	descriptor->width = width;
	descriptor->height = height;
	return descriptor;
}

void exportable_surface_backend::set_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	std::lock_guard lock { descriptor->mutex };
	descriptor->width = width;
	descriptor->height = height;
}

void exportable_surface_backend::dispose_descriptor(ExportableSurfaceDescriptorHandle descriptor) {
	if (descriptor->proxy != nullptr) {
		std::lock_guard lock { proxy_registry_mutex };
		proxy_registry.erase(descriptor->proxy);
		delete descriptor->proxy;
		descriptor->proxy = nullptr;
	}
	destroy_all_slots(descriptor);
	delete descriptor;
}

void exportable_surface_backend::get_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight) {
	std::lock_guard lock { descriptor->mutex };
	*outGeneration = descriptor->generation;
	*outIsLive = descriptor->hasLiveSwapChain ? interop_bool_true : interop_bool_false;
	*outWidth = descriptor->liveWidth;
	*outHeight = descriptor->liveHeight;
}

bool exportable_surface_backend::export_image_handles(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outImageHandle, uint64_t* outImageSizeBytes, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	std::lock_guard lock { descriptor->mutex };
	if (!descriptor->hasLiveSwapChain || descriptor->generation != generation) return false;
	auto const& slot = descriptor->slots[index];
	CFRetain(slot.surface);
	*outImageHandle = reinterpret_cast<int64_t>(slot.surface);
	*outImageSizeBytes = IOSurfaceGetAllocSize(slot.surface);
	*outReadyHandle = 0;
	*outReleasedHandle = 0;
	return true;
}

void exportable_surface_backend::close_handle(int32_t handleType, int64_t handle) {
	if (handleType != EXPORTABLE_SURFACE_HANDLE_TYPE_IOSURFACE_REF || handle == 0) return;
	CFRelease(reinterpret_cast<IOSurfaceRef>(handle));
}

bool exportable_surface_backend::take_latest_presented_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex) {
	std::lock_guard lock { descriptor->mutex };
	if (!descriptor->hasLiveSwapChain || descriptor->generation != generation || descriptor->presentedQueue.empty()) return false;

	auto found = false;
	size_t latestPosition = 0;
	for (auto position = descriptor->presentedQueue.size(); position > 0; --position) {
		if (!fence_has_completed(descriptor->slots[descriptor->presentedQueue[position - 1U]].completionFence)) continue;
		latestPosition = position - 1U;
		found = true;
		break;
	}
	if (!found) return false;

	auto latest = descriptor->presentedQueue[latestPosition];
	for (size_t position = 0; position < latestPosition; ++position) {
		descriptor->slots[descriptor->presentedQueue[position]].state = metal_image_state::pending_discard;
	}
	descriptor->presentedQueue.erase(descriptor->presentedQueue.begin(), descriptor->presentedQueue.begin() + static_cast<std::ptrdiff_t>(latestPosition) + 1);
	descriptor->slots[latest].state = metal_image_state::handed;
	*outIndex = latest;
	return true;
}

void exportable_surface_backend::return_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, bool consumerSignalledRelease) {
	std::lock_guard lock { descriptor->mutex };
	if (descriptor->generation != generation || !descriptor->hasLiveSwapChain) return;
	auto& slot = descriptor->slots[index];
	if (slot.state != metal_image_state::handed) return;
	slot.state = metal_image_state::released;
}

SwapChainHandle exportable_surface_backend::allocate_swap_chain(ExportableSurfaceDescriptorHandle descriptor) {
	{
		std::lock_guard lock { descriptor->mutex };
		descriptor->liveWidth = descriptor->width;
		descriptor->liveHeight = descriptor->height;
		for (auto& slot : descriptor->slots) create_slot(descriptor, slot);
		descriptor->presentedQueue.clear();
		descriptor->nextAcquireStartIndex = 0;
		descriptor->currentFrameSlot = ScratchSlotIndex;
		descriptor->generation++;
		descriptor->hasLiveSwapChain = true;
	}
	if (descriptor->proxy == nullptr) {
		descriptor->proxy = new metal_swap_chain_proxy { descriptor };
		std::lock_guard lock { proxy_registry_mutex };
		proxy_registry.insert(descriptor->proxy);
	}
	return reinterpret_cast<SwapChainHandle>(descriptor->proxy);
}

bool exportable_surface_backend::dispose_swap_chain_if_owned(SwapChainHandle swapChain) {
	auto proxy = as_proxy(swapChain);
	if (proxy == nullptr) return false;
	destroy_all_slots(proxy->descriptor);
	return true;
}

SwapChainHandle exportable_surface_backend::begin_frame_swap_chain(SwapChainHandle swapChain) {
	auto proxy = as_proxy(swapChain);
	if (proxy == nullptr) return swapChain;

	auto descriptor = proxy->descriptor;
	filament::Fence* fenceToDestroy = nullptr;
	SwapChainHandle result;
	{
		std::lock_guard lock { descriptor->mutex };
		ThrowIf(!descriptor->hasLiveSwapChain, "Exportable surface has no live swap chain.");
		for (uint32_t i = 0; i < ScratchSlotIndex; ++i) {
			if (descriptor->slots[i].state == metal_image_state::pending_discard) descriptor->slots[i].state = metal_image_state::free;
		}

		auto chosenIndex = ScratchSlotIndex;
		for (uint32_t offset = 0; offset < ScratchSlotIndex; ++offset) {
			auto candidate = (descriptor->nextAcquireStartIndex + offset) % ScratchSlotIndex;
			auto state = descriptor->slots[candidate].state;
			if (state != metal_image_state::free && state != metal_image_state::released) continue;
			chosenIndex = candidate;
			descriptor->nextAcquireStartIndex = (candidate + 1U) % ScratchSlotIndex;
			break;
		}

		auto& chosen = descriptor->slots[chosenIndex];
		fenceToDestroy = chosen.completionFence;
		chosen.completionFence = nullptr;
		if (chosenIndex != ScratchSlotIndex) chosen.state = metal_image_state::in_flight;
		descriptor->currentFrameSlot = chosenIndex;
		result = chosen.swapChain;
	}
	if (fenceToDestroy != nullptr) filament_engine->destroy(fenceToDestroy);
	return result;
}

void exportable_surface_backend::end_frame(SwapChainHandle swapChain) {
	auto proxy = as_proxy(swapChain);
	if (proxy == nullptr) return;

	auto descriptor = proxy->descriptor;
	auto fence = filament_engine->createFence();
	std::lock_guard lock { descriptor->mutex };
	auto const index = descriptor->currentFrameSlot;
	auto& slot = descriptor->slots[index];
	if (slot.completionFence != nullptr) filament_engine->destroy(slot.completionFence);
	slot.completionFence = fence;
	if (index == ScratchSlotIndex) return;
	slot.state = metal_image_state::presented;
	descriptor->presentedQueue.push_back(index);
}

#endif
