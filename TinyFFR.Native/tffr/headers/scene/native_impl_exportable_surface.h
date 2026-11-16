#pragma once

#include "utils_and_constants.h"
#include "native_impl_render.h"

#if defined(TFFR_LINUX)
#include "backend/platforms/VulkanPlatformLinux.h"
#include <deque>
#include <mutex>
#include <vector>
#include <unordered_set>
#endif

#define EXPORTABLE_SURFACE_HANDLE_TYPE_NONE 0
#define EXPORTABLE_SURFACE_HANDLE_TYPE_OPAQUE_FD 1

#define EXPORTABLE_SURFACE_SYNC_KIND_NONE 0
#define EXPORTABLE_SURFACE_SYNC_KIND_SEMAPHORES 1

#define EXPORTABLE_SURFACE_IMAGE_COUNT 5
#define EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH 16

struct exportable_surface_descriptor;
typedef exportable_surface_descriptor* ExportableSurfaceDescriptorHandle;

#if defined(TFFR_LINUX)

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

class tffr_vulkan_platform : public filament::backend::VulkanPlatformLinux {
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
};

#endif

class native_impl_exportable_surface {
public:
	static void get_exportable_surface_capabilities(int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid);
	static void allocate_exportable_surface_descriptor(uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor);
	static void set_exportable_surface_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height);
	static void dispose_exportable_surface_descriptor(ExportableSurfaceDescriptorHandle descriptor);
	static void allocate_exportable_swap_chain(ExportableSurfaceDescriptorHandle descriptor, SwapChainHandle* outSwapChain);
	static void get_exportable_surface_generation(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight);
	static void export_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outHandle, uint64_t* outMemorySizeBytes);
	static void export_exportable_surface_semaphores(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outReadyHandle, int64_t* outReleasedHandle);
	static void close_exportable_surface_handle(int64_t handle);
	static void take_latest_presented_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, uint32_t* outIndex, interop_bool* outFound);
	static void return_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease);
};
