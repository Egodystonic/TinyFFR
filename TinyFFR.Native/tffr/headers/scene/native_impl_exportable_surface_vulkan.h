#pragma once

#include "scene/native_impl_exportable_surface.h"

#if defined(TFFR_LINUX)
#define TFFR_EXPORTABLE_SURFACE_VULKAN
#include "backend/platforms/VulkanPlatformLinux.h"
using tffr_vulkan_platform_base = filament::backend::VulkanPlatformLinux;
constexpr VkExternalMemoryHandleTypeFlagBits ExportableMemoryHandleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT;
constexpr VkExternalSemaphoreHandleTypeFlagBits ExportableSemaphoreHandleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT;
constexpr int32_t ExportableSurfaceHandleTypeId = EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD;
constexpr int32_t ExportableSurfaceSyncKindId = EXPORTABLE_SURFACE_SYNC_KIND_VULKAN_OPAQUE_FD_SEMAPHORES;
#endif

#if defined(TFFR_EXPORTABLE_SURFACE_VULKAN)
#include <deque>
#include <mutex>
#include <vector>
#include <unordered_set>

enum class exportable_image_state : uint8_t {
	free,
	in_flight,
	presented,
	handed,
	released,
	pending_discard
};

struct exportable_image_slot {
	VkImage image = VK_NULL_HANDLE;
	VkDeviceMemory memory = VK_NULL_HANDLE;
	VkDeviceSize memorySize = 0;
	VkSemaphore readySemaphore = VK_NULL_HANDLE;
	VkSemaphore releasedSemaphore = VK_NULL_HANDLE;
	VkSemaphore acquireSemaphore = VK_NULL_HANDLE;
	exportable_image_state state = exportable_image_state::free;
	bool hasBeenUsed = false;
};

struct exportable_surface_descriptor {
	std::mutex mutex;
	uint32_t width;
	uint32_t height;
	uint32_t generation = 0;
	uint32_t liveWidth = 0;
	uint32_t liveHeight = 0;
	bool hasLiveSwapChain = false;
	exportable_image_slot slots[EXPORTABLE_SURFACE_IMAGE_COUNT + 1];
	std::deque<uint32_t> presentedQueue;
	uint32_t nextAcquireStartIndex = 0;
};

struct exportable_swap_chain : public filament::backend::Platform::SwapChain {
	static constexpr uint32_t InitialSubmissionSlotCount = 8;
	static constexpr uint32_t MaxSubmissionSlotCount = 64;

	exportable_surface_descriptor* descriptor;
	VkDevice device;
	VkQueue queue;
	VkImage depthImage = VK_NULL_HANDLE;
	VkDeviceMemory depthMemory = VK_NULL_HANDLE;
	VkFormat depthFormat = VK_FORMAT_UNDEFINED;
	VkCommandPool commandPool = VK_NULL_HANDLE;
	std::vector<VkCommandBuffer> commandBuffers;
	std::vector<VkFence> fences;
	std::vector<uint8_t> fenceSubmitted;
	uint32_t nextSubmissionSlot = 0;
};

struct exportable_submission {
	VkImage image = VK_NULL_HANDLE;
	VkImageLayout oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
	VkImageLayout newLayout = VK_IMAGE_LAYOUT_UNDEFINED;
	VkAccessFlags srcAccess = 0;
	VkAccessFlags dstAccess = 0;
	uint32_t srcQueueFamily = VK_QUEUE_FAMILY_IGNORED;
	uint32_t dstQueueFamily = VK_QUEUE_FAMILY_IGNORED;
	VkSemaphore waitSemaphores[EXPORTABLE_SURFACE_IMAGE_COUNT + 2] = {};
	uint32_t waitSemaphoreCount = 0;
	VkSemaphore signalSemaphore = VK_NULL_HANDLE;
};

class tffr_vulkan_platform : public tffr_vulkan_platform_base {
public:
	static tffr_vulkan_platform* instance;

	bool exportingSupported = false;

	void evaluate_export_support();

	ExtensionSet getRequiredDeviceExtensions() override;
	SwapChainPtr createSwapChain(void* nativeWindow, uint64_t flags, VkExtent2D extent) override;
	SwapChainBundle getSwapChainBundle(SwapChainPtr handle) override;
	VkResult acquire(SwapChainPtr handle, ImageSyncData* outImageSyncData) override;
	VkResult present(SwapChainPtr handle, uint32_t index, VkSemaphore finishedDrawing) override;
	bool hasResized(SwapChainPtr handle) override;
	bool isProtected(SwapChainPtr handle) override;
	VkResult recreate(SwapChainPtr handle) override;
	void destroy(SwapChainPtr handle) override;

	void register_descriptor(exportable_surface_descriptor* descriptor);
	void unregister_descriptor(exportable_surface_descriptor* descriptor);

private:
	std::mutex descriptorRegistryMutex;
	std::unordered_set<exportable_surface_descriptor*> descriptors;
	std::unordered_set<exportable_swap_chain*> swapChains;

	bool is_registered_descriptor(void* nativeWindow);
	exportable_swap_chain* as_exportable(SwapChainPtr handle) const;
	exportable_swap_chain* create_exportable_swap_chain(exportable_surface_descriptor* descriptor);
	void destroy_exportable_swap_chain(exportable_swap_chain* swapChain);
	uint32_t find_memory_type(uint32_t typeBits, VkMemoryPropertyFlags flags);
	VkFormat select_depth_format();
	VkSemaphore create_semaphore(bool exportable);
	void create_exportable_image(exportable_swap_chain* swapChain, exportable_image_slot& slot, bool exportable);
	void add_submission_slot(exportable_swap_chain* swapChain);
	uint32_t acquire_submission_slot(exportable_swap_chain* swapChain);
	void submit(exportable_swap_chain* swapChain, exportable_submission const& submission);

	bool device_supports_handle_export(VkPhysicalDevice physicalDevice);

public:
	VkResult export_memory_handle(VkDeviceMemory memory, int64_t* outHandle);
	VkResult export_semaphore_handle(VkSemaphore semaphore, int64_t* outHandle);
	static void close_exported_handle(int64_t handle);
};

#endif
