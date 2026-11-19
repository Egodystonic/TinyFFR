#include "pch.h"
#include "scene/native_impl_exportable_surface_vulkan.h"

#if defined(TFFR_LINUX) || defined(TFFR_WIN)
#include "native_impl_init.h"
#include "filament/Engine.h"
#include <cstring>

using namespace bluevk;

namespace {
	constexpr VkImageUsageFlags ExportableColorUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_SAMPLED_BIT;
	constexpr uint32_t ScratchSlotIndex = EXPORTABLE_SURFACE_IMAGE_COUNT;

	int32_t sync_kind_for(int32_t handleType) {
		switch (handleType) {
			case EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD: return EXPORTABLE_SURFACE_SYNC_KIND_VULKAN_OPAQUE_FD_SEMAPHORES;
			case EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE: return EXPORTABLE_SURFACE_SYNC_KIND_DXGI_KEYED_MUTEX;
			case EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_LEGACY_SHARED_HANDLE: return EXPORTABLE_SURFACE_SYNC_KIND_COMPLETED_BEFORE_TAKE;
			default: return EXPORTABLE_SURFACE_SYNC_KIND_NONE;
		}
	}

	bool uses_semaphores(exportable_surface_descriptor const* descriptor) {
		return descriptor->handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD;
	}
	bool uses_keyed_mutex(exportable_surface_descriptor const* descriptor) {
		return descriptor->handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE;
	}
	bool uses_completion_fence(exportable_surface_descriptor const* descriptor) {
		return descriptor->handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_LEGACY_SHARED_HANDLE;
	}

	int device_type_rank(VkPhysicalDeviceType deviceType) {
		switch (deviceType) {
			case VK_PHYSICAL_DEVICE_TYPE_OTHER: return 0;
			case VK_PHYSICAL_DEVICE_TYPE_VIRTUAL_GPU: return 1;
			case VK_PHYSICAL_DEVICE_TYPE_CPU: return 2;
			case VK_PHYSICAL_DEVICE_TYPE_INTEGRATED_GPU: return 3;
			case VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU: return 4;
			default: return -1;
		}
	}

	bool has_graphics_queue(VkPhysicalDevice physicalDevice) {
		uint32_t count = 0;
		vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, nullptr);
		std::vector<VkQueueFamilyProperties> families(count);
		vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, families.data());
		for (auto const& family : families) {
			if (family.queueCount != 0 && (family.queueFlags & VK_QUEUE_GRAPHICS_BIT) != 0) return true;
		}
		return false;
	}

	bool is_translation_layer_driver(VkPhysicalDevice physicalDevice, uint32_t apiVersion) {
		if (vkGetPhysicalDeviceProperties2 == nullptr) return false;
		auto const hasDriverProperties = VK_API_VERSION_MAJOR(apiVersion) > 1 || VK_API_VERSION_MINOR(apiVersion) >= 2 || exportable_device_supports_extension(physicalDevice, VK_KHR_DRIVER_PROPERTIES_EXTENSION_NAME);
		if (!hasDriverProperties) return false;
		VkPhysicalDeviceDriverProperties driverProperties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES };
		VkPhysicalDeviceProperties2 properties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2, .pNext = &driverProperties };
		vkGetPhysicalDeviceProperties2(physicalDevice, &properties);
		return driverProperties.driverID == VK_DRIVER_ID_MESA_DOZEN;
	}

	int8_t select_preferred_gpu_index(VkInstance instance) {
		if (instance == VK_NULL_HANDLE) return -1;
		uint32_t count = 0;
		if (vkEnumeratePhysicalDevices(instance, &count, nullptr) != VK_SUCCESS || count == 0) return -1;
		std::vector<VkPhysicalDevice> physicalDevices(count);
		if (vkEnumeratePhysicalDevices(instance, &count, physicalDevices.data()) != VK_SUCCESS) return -1;

		int8_t bestIndex = -1;
		auto bestTypeRank = -1;
		auto bestIsTranslationLayer = false;
		auto anyCandidateIsTranslationLayer = false;
		for (uint32_t i = 0; i < count && i <= static_cast<uint32_t>(INT8_MAX); ++i) {
			auto const physicalDevice = physicalDevices[i];
			VkPhysicalDeviceProperties properties;
			vkGetPhysicalDeviceProperties(physicalDevice, &properties);
			auto const major = VK_API_VERSION_MAJOR(properties.apiVersion);
			auto const minor = VK_API_VERSION_MINOR(properties.apiVersion);
			if (major < 1 || (major == 1 && minor < 1)) continue;
			if (!has_graphics_queue(physicalDevice)) continue;
			if (!exportable_device_supports_extension(physicalDevice, VK_KHR_SWAPCHAIN_EXTENSION_NAME)) continue;

			auto const typeRank = device_type_rank(properties.deviceType);
			auto const isTranslationLayer = is_translation_layer_driver(physicalDevice, properties.apiVersion);
			anyCandidateIsTranslationLayer |= isTranslationLayer;
			auto const isBetter = bestIndex < 0
				|| typeRank > bestTypeRank
				|| (typeRank == bestTypeRank && bestIsTranslationLayer && !isTranslationLayer);
			if (!isBetter) continue;
			bestIndex = static_cast<int8_t>(i);
			bestTypeRank = typeRank;
			bestIsTranslationLayer = isTranslationLayer;
		}
		return anyCandidateIsTranslationLayer ? bestIndex : static_cast<int8_t>(-1);
	}
}

void exportable_check_vk(VkResult result, const char* operation) {
	if (result == VK_SUCCESS) return;
	char resultStr[16];
	interop_utils::int_str(resultStr, sizeof(resultStr), static_cast<int>(result));
	Throw(operation, " failed with VkResult ", resultStr, ".");
}

bool exportable_device_supports_extension(VkPhysicalDevice physicalDevice, const char* extensionName) {
	uint32_t count = 0;
	if (vkEnumerateDeviceExtensionProperties(physicalDevice, nullptr, &count, nullptr) != VK_SUCCESS) return false;
	std::vector<VkExtensionProperties> properties(count);
	if (vkEnumerateDeviceExtensionProperties(physicalDevice, nullptr, &count, properties.data()) != VK_SUCCESS) return false;
	for (auto const& p : properties) {
		if (std::strcmp(p.extensionName, extensionName) == 0) return true;
	}
	return false;
}

tffr_vulkan_platform* tffr_vulkan_platform::instance = nullptr;

filament::backend::VulkanPlatform::Customization tffr_vulkan_platform::getCustomization() const noexcept {
	auto result = tffr_vulkan_platform_base::getCustomization();
	if (result.gpu.index >= 0 || !result.gpu.deviceName.empty()) return result;
	if (!preferredGpuIndexResolved) {
		preferredGpuIndex = select_preferred_gpu_index(getInstance());
		preferredGpuIndexResolved = true;
	}
	result.gpu.index = preferredGpuIndex;
	return result;
}

void tffr_vulkan_platform::evaluate_export_support() {
	supportedHandleTypeMask = 0;
	deviceLuidValid = false;
	auto physicalDevice = getPhysicalDevice();
	if (physicalDevice == VK_NULL_HANDLE || getDevice() == VK_NULL_HANDLE) return;
	if (vkGetPhysicalDeviceImageFormatProperties2 == nullptr || vkGetPhysicalDeviceExternalSemaphoreProperties == nullptr || vkGetPhysicalDeviceProperties2 == nullptr || vkGetImageMemoryRequirements2 == nullptr) return;

	VkPhysicalDeviceIDProperties idProperties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES };
	VkPhysicalDeviceProperties2 deviceProperties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2, .pNext = &idProperties };
	vkGetPhysicalDeviceProperties2(physicalDevice, &deviceProperties);
	std::memcpy(deviceUuid, idProperties.deviceUUID, EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH);
	if (idProperties.deviceLUIDValid == VK_TRUE) {
		std::memcpy(deviceLuid, idProperties.deviceLUID, EXPORTABLE_SURFACE_DEVICE_LUID_LENGTH);
		deviceLuidValid = true;
	}

	supportedHandleTypeMask = evaluate_os_supported_handle_types(physicalDevice);
}

bool tffr_vulkan_platform::supports_handle_type(int32_t handleType) const {
	if (handleType <= 0 || handleType >= 32) return false;
	return (supportedHandleTypeMask & (1U << handleType)) != 0;
}

void tffr_vulkan_platform::register_descriptor(exportable_surface_descriptor* descriptor) {
	std::lock_guard lock { descriptorRegistryMutex };
	descriptors.insert(descriptor);
}
void tffr_vulkan_platform::unregister_descriptor(exportable_surface_descriptor* descriptor) {
	std::lock_guard lock { descriptorRegistryMutex };
	descriptors.erase(descriptor);
}
bool tffr_vulkan_platform::is_registered_descriptor(void* nativeWindow) {
	if (nativeWindow == nullptr) return false;
	std::lock_guard lock { descriptorRegistryMutex };
	return descriptors.contains(static_cast<exportable_surface_descriptor*>(nativeWindow));
}

exportable_swap_chain* tffr_vulkan_platform::as_exportable(SwapChainPtr handle) const {
	auto candidate = static_cast<exportable_swap_chain*>(handle);
	return swapChains.contains(candidate) ? candidate : nullptr;
}

uint32_t tffr_vulkan_platform::find_memory_type(uint32_t typeBits, VkMemoryPropertyFlags flags) {
	VkPhysicalDeviceMemoryProperties memoryProperties;
	vkGetPhysicalDeviceMemoryProperties(getPhysicalDevice(), &memoryProperties);
	for (uint32_t i = 0; i < memoryProperties.memoryTypeCount; ++i) {
		if ((typeBits & (1U << i)) != 0 && (memoryProperties.memoryTypes[i].propertyFlags & flags) == flags) return i;
	}
	Throw("Could not find a suitable memory type for exportable surface image.");
}

VkFormat tffr_vulkan_platform::select_depth_format() {
	for (auto candidate : { VK_FORMAT_D32_SFLOAT, VK_FORMAT_X8_D24_UNORM_PACK32, VK_FORMAT_D16_UNORM }) {
		VkFormatProperties properties;
		vkGetPhysicalDeviceFormatProperties(getPhysicalDevice(), candidate, &properties);
		if ((properties.optimalTilingFeatures & VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT) != 0) return candidate;
	}
	Throw("Could not find a suitable depth format for exportable surface.");
}

void tffr_vulkan_platform::create_local_image(exportable_surface_descriptor const* descriptor, exportable_image_slot& slot) {
	auto device = getDevice();
	VkImageCreateInfo imageInfo {
		.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
		.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT,
		.imageType = VK_IMAGE_TYPE_2D,
		.format = descriptor->colorFormat,
		.extent = { descriptor->liveWidth, descriptor->liveHeight, 1 },
		.mipLevels = 1,
		.arrayLayers = 1,
		.samples = VK_SAMPLE_COUNT_1_BIT,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = ExportableColorUsage,
		.sharingMode = VK_SHARING_MODE_EXCLUSIVE,
		.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED
	};
	exportable_check_vk(vkCreateImage(device, &imageInfo, nullptr, &slot.image), "vkCreateImage");
	VkMemoryRequirements requirements;
	vkGetImageMemoryRequirements(device, slot.image, &requirements);
	VkMemoryAllocateInfo allocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.allocationSize = requirements.size,
		.memoryTypeIndex = find_memory_type(requirements.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
	};
	exportable_check_vk(vkAllocateMemory(device, &allocateInfo, nullptr, &slot.memory), "vkAllocateMemory");
	exportable_check_vk(vkBindImageMemory(device, slot.image, slot.memory, 0), "vkBindImageMemory");
	slot.memorySize = requirements.size;
}

VkSemaphore tffr_vulkan_platform::create_semaphore(VkExternalSemaphoreHandleTypeFlags exportHandleTypes) {
	VkExportSemaphoreCreateInfo exportInfo {
		.sType = VK_STRUCTURE_TYPE_EXPORT_SEMAPHORE_CREATE_INFO,
		.handleTypes = exportHandleTypes
	};
	VkSemaphoreCreateInfo createInfo {
		.sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
		.pNext = exportHandleTypes != 0 ? &exportInfo : nullptr
	};
	VkSemaphore result;
	exportable_check_vk(vkCreateSemaphore(getDevice(), &createInfo, nullptr, &result), "vkCreateSemaphore");
	return result;
}

exportable_swap_chain* tffr_vulkan_platform::create_exportable_swap_chain(exportable_surface_descriptor* descriptor) {
	auto device = getDevice();
	auto swapChain = new exportable_swap_chain();
	swapChain->descriptor = descriptor;
	swapChain->device = device;
	swapChain->queue = getGraphicsQueue();

	VkCommandPoolCreateInfo poolInfo {
		.sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
		.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
		.queueFamilyIndex = getGraphicsQueueFamilyIndex()
	};
	exportable_check_vk(vkCreateCommandPool(device, &poolInfo, nullptr, &swapChain->commandPool), "vkCreateCommandPool");
	for (uint32_t i = 0; i < exportable_swap_chain::InitialSubmissionSlotCount; ++i) add_submission_slot(swapChain);

	{
		std::lock_guard lock { descriptor->mutex };
		descriptor->liveWidth = descriptor->width;
		descriptor->liveHeight = descriptor->height;
		for (uint32_t i = 0; i <= ScratchSlotIndex; ++i) {
			auto& slot = descriptor->slots[i];
			slot = {};
			if (i == ScratchSlotIndex) create_local_image(descriptor, slot);
			else create_shared_image(descriptor, slot);
			slot.acquireSemaphore = create_semaphore(0);
			if (i != ScratchSlotIndex && uses_completion_fence(descriptor)) {
				VkFenceCreateInfo fenceInfo { .sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
				exportable_check_vk(vkCreateFence(device, &fenceInfo, nullptr, &slot.completionFence), "vkCreateFence");
			}
			slot.state = exportable_image_state::free;
			slot.hasBeenUsed = false;
		}
		descriptor->presentedQueue.clear();
		descriptor->nextAcquireStartIndex = 0;
		descriptor->generation++;
		descriptor->hasLiveSwapChain = true;
	}

	swapChain->depthFormat = select_depth_format();
	VkImageCreateInfo depthInfo {
		.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
		.imageType = VK_IMAGE_TYPE_2D,
		.format = swapChain->depthFormat,
		.extent = { descriptor->liveWidth, descriptor->liveHeight, 1 },
		.mipLevels = 1,
		.arrayLayers = 1,
		.samples = VK_SAMPLE_COUNT_1_BIT,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT,
		.sharingMode = VK_SHARING_MODE_EXCLUSIVE,
		.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED
	};
	exportable_check_vk(vkCreateImage(device, &depthInfo, nullptr, &swapChain->depthImage), "vkCreateImage");
	VkMemoryRequirements depthRequirements;
	vkGetImageMemoryRequirements(device, swapChain->depthImage, &depthRequirements);
	VkMemoryAllocateInfo depthAllocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.allocationSize = depthRequirements.size,
		.memoryTypeIndex = find_memory_type(depthRequirements.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
	};
	exportable_check_vk(vkAllocateMemory(device, &depthAllocateInfo, nullptr, &swapChain->depthMemory), "vkAllocateMemory");
	exportable_check_vk(vkBindImageMemory(device, swapChain->depthImage, swapChain->depthMemory, 0), "vkBindImageMemory");

	swapChains.insert(swapChain);
	return swapChain;
}

void tffr_vulkan_platform::destroy_exportable_swap_chain(exportable_swap_chain* swapChain) {
	auto device = swapChain->device;
	for (size_t i = 0; i < swapChain->fences.size(); ++i) {
		if (swapChain->fenceSubmitted[i]) vkWaitForFences(device, 1, &swapChain->fences[i], VK_TRUE, UINT64_MAX);
	}

	{
		auto descriptor = swapChain->descriptor;
		std::lock_guard lock { descriptor->mutex };
		for (auto& slot : descriptor->slots) {
			if (slot.completionFence != VK_NULL_HANDLE) {
				if (slot.completionFencePending) vkWaitForFences(device, 1, &slot.completionFence, VK_TRUE, UINT64_MAX);
				vkDestroyFence(device, slot.completionFence, nullptr);
			}
			if (slot.image != VK_NULL_HANDLE) vkDestroyImage(device, slot.image, nullptr);
			if (slot.memory != VK_NULL_HANDLE) vkFreeMemory(device, slot.memory, nullptr);
			if (slot.readySemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.readySemaphore, nullptr);
			if (slot.releasedSemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.releasedSemaphore, nullptr);
			if (slot.acquireSemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.acquireSemaphore, nullptr);
			release_shared_image_os_resources(slot);
			slot = {};
		}
		descriptor->presentedQueue.clear();
		descriptor->hasLiveSwapChain = false;
	}

	if (swapChain->depthImage != VK_NULL_HANDLE) vkDestroyImage(device, swapChain->depthImage, nullptr);
	if (swapChain->depthMemory != VK_NULL_HANDLE) vkFreeMemory(device, swapChain->depthMemory, nullptr);
	for (auto fence : swapChain->fences) vkDestroyFence(device, fence, nullptr);
	vkDestroyCommandPool(device, swapChain->commandPool, nullptr);

	swapChains.erase(swapChain);
	delete swapChain;
}

void tffr_vulkan_platform::add_submission_slot(exportable_swap_chain* swapChain) {
	VkCommandBufferAllocateInfo bufferInfo {
		.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
		.commandPool = swapChain->commandPool,
		.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY,
		.commandBufferCount = 1
	};
	VkCommandBuffer commandBuffer;
	exportable_check_vk(vkAllocateCommandBuffers(swapChain->device, &bufferInfo, &commandBuffer), "vkAllocateCommandBuffers");
	VkFenceCreateInfo fenceInfo { .sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
	VkFence fence;
	exportable_check_vk(vkCreateFence(swapChain->device, &fenceInfo, nullptr, &fence), "vkCreateFence");
	swapChain->commandBuffers.push_back(commandBuffer);
	swapChain->fences.push_back(fence);
	swapChain->fenceSubmitted.push_back(0);
}

uint32_t tffr_vulkan_platform::acquire_submission_slot(exportable_swap_chain* swapChain) {
	auto const device = swapChain->device;
	auto const slotCount = static_cast<uint32_t>(swapChain->fences.size());
	for (uint32_t offset = 0; offset < slotCount; ++offset) {
		auto const slot = (swapChain->nextSubmissionSlot + offset) % slotCount;
		if (swapChain->fenceSubmitted[slot] && vkGetFenceStatus(device, swapChain->fences[slot]) != VK_SUCCESS) continue;
		if (swapChain->fenceSubmitted[slot]) {
			exportable_check_vk(vkResetFences(device, 1, &swapChain->fences[slot]), "vkResetFences");
			swapChain->fenceSubmitted[slot] = 0;
		}
		swapChain->nextSubmissionSlot = (slot + 1U) % slotCount;
		return slot;
	}

	if (slotCount < exportable_swap_chain::MaxSubmissionSlotCount) {
		add_submission_slot(swapChain);
		return slotCount;
	}

	auto const slot = swapChain->nextSubmissionSlot % slotCount;
	exportable_check_vk(vkWaitForFences(device, 1, &swapChain->fences[slot], VK_TRUE, UINT64_MAX), "vkWaitForFences");
	exportable_check_vk(vkResetFences(device, 1, &swapChain->fences[slot]), "vkResetFences");
	swapChain->fenceSubmitted[slot] = 0;
	swapChain->nextSubmissionSlot = (slot + 1U) % slotCount;
	return slot;
}

void tffr_vulkan_platform::submit(exportable_swap_chain* swapChain, exportable_submission const& submission) {
	auto const slotIndex = acquire_submission_slot(swapChain);
	auto fence = swapChain->fences[slotIndex];
	auto commandBuffer = swapChain->commandBuffers[slotIndex];

	exportable_check_vk(vkResetCommandBuffer(commandBuffer, 0), "vkResetCommandBuffer");
	VkCommandBufferBeginInfo beginInfo {
		.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
		.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
	};
	exportable_check_vk(vkBeginCommandBuffer(commandBuffer, &beginInfo), "vkBeginCommandBuffer");
	VkImageMemoryBarrier barrier {
		.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
		.srcAccessMask = submission.srcAccess,
		.dstAccessMask = submission.dstAccess,
		.oldLayout = submission.oldLayout,
		.newLayout = submission.newLayout,
		.srcQueueFamilyIndex = submission.srcQueueFamily,
		.dstQueueFamilyIndex = submission.dstQueueFamily,
		.image = submission.image,
		.subresourceRange = { VK_IMAGE_ASPECT_COLOR_BIT, 0, 1, 0, 1 }
	};
	vkCmdPipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 0, nullptr, 0, nullptr, 1, &barrier);
	exportable_check_vk(vkEndCommandBuffer(commandBuffer), "vkEndCommandBuffer");

	exportable_submit_extension_storage extensionStorage;
	VkPipelineStageFlags waitStages[EXPORTABLE_SURFACE_IMAGE_COUNT + 2];
	for (uint32_t i = 0; i < submission.waitSemaphoreCount; ++i) waitStages[i] = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
	VkSubmitInfo submitInfo {
		.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO,
		.pNext = build_submit_extension_chain(submission, extensionStorage),
		.waitSemaphoreCount = submission.waitSemaphoreCount,
		.pWaitSemaphores = submission.waitSemaphores,
		.pWaitDstStageMask = waitStages,
		.commandBufferCount = 1,
		.pCommandBuffers = &commandBuffer,
		.signalSemaphoreCount = submission.signalSemaphore != VK_NULL_HANDLE ? 1U : 0U,
		.pSignalSemaphores = submission.signalSemaphore != VK_NULL_HANDLE ? &submission.signalSemaphore : nullptr
	};
	exportable_check_vk(vkQueueSubmit(swapChain->queue, 1, &submitInfo, fence), "vkQueueSubmit");
	swapChain->fenceSubmitted[slotIndex] = 1;

	if (submission.completionFence != VK_NULL_HANDLE) {
		exportable_check_vk(vkQueueSubmit(swapChain->queue, 0, nullptr, submission.completionFence), "vkQueueSubmit");
	}
}

filament::backend::VulkanPlatform::SwapChainPtr tffr_vulkan_platform::createSwapChain(void* nativeWindow, uint64_t flags, VkExtent2D extent) {
	if (!is_registered_descriptor(nativeWindow)) return tffr_vulkan_platform_base::createSwapChain(nativeWindow, flags, extent);
	return create_exportable_swap_chain(static_cast<exportable_surface_descriptor*>(nativeWindow));
}

filament::backend::VulkanPlatform::SwapChainBundle tffr_vulkan_platform::getSwapChainBundle(SwapChainPtr handle) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) return tffr_vulkan_platform_base::getSwapChainBundle(handle);

	auto descriptor = swapChain->descriptor;
	SwapChainBundle result;
	std::lock_guard lock { descriptor->mutex };
	result.colors = utils::FixedCapacityVector<VkImage>::with_capacity(ScratchSlotIndex + 1U);
	for (uint32_t i = 0; i <= ScratchSlotIndex; ++i) result.colors.push_back(descriptor->slots[i].image);
	result.depth = swapChain->depthImage;
	result.colorFormat = descriptor->colorFormat;
	result.depthFormat = swapChain->depthFormat;
	result.extent = { descriptor->liveWidth, descriptor->liveHeight };
	result.layerCount = 1;
	result.isProtected = false;
	return result;
}

VkResult tffr_vulkan_platform::acquire(SwapChainPtr handle, ImageSyncData* outImageSyncData) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) return tffr_vulkan_platform_base::acquire(handle, outImageSyncData);

	auto descriptor = swapChain->descriptor;
	exportable_submission submission;
	auto needsSubmit = false;
	uint32_t chosenIndex;
	VkSemaphore acquireSemaphore;
	VkFence completionFenceToRecycle = VK_NULL_HANDLE;
	{
		std::lock_guard lock { descriptor->mutex };

		for (uint32_t i = 0; i < ScratchSlotIndex; ++i) {
			auto& slot = descriptor->slots[i];
			if (slot.state != exportable_image_state::pending_discard) continue;
			if (uses_semaphores(descriptor)) submission.waitSemaphores[submission.waitSemaphoreCount++] = slot.readySemaphore;
			slot.state = exportable_image_state::free;
		}

		chosenIndex = ScratchSlotIndex;
		for (uint32_t offset = 0; offset < ScratchSlotIndex; ++offset) {
			auto candidate = (descriptor->nextAcquireStartIndex + offset) % ScratchSlotIndex;
			auto state = descriptor->slots[candidate].state;
			if (state != exportable_image_state::free && state != exportable_image_state::released) continue;
			chosenIndex = candidate;
			descriptor->nextAcquireStartIndex = (candidate + 1U) % ScratchSlotIndex;
			break;
		}

		auto& chosen = descriptor->slots[chosenIndex];
		auto const isExported = chosenIndex != ScratchSlotIndex;
		if (chosen.state == exportable_image_state::released && uses_semaphores(descriptor)) {
			submission.waitSemaphores[submission.waitSemaphoreCount++] = chosen.releasedSemaphore;
		}
		if (isExported && uses_keyed_mutex(descriptor)) {
			submission.keyedMutexMemory = chosen.memory;
			submission.acquireKeyedMutex = true;
			submission.keyedMutexAcquireKey = chosen.keyedMutexKey;
		}
		if (isExported && chosen.completionFencePending) {
			completionFenceToRecycle = chosen.completionFence;
			chosen.completionFencePending = false;
		}
		if (isExported) chosen.state = exportable_image_state::in_flight;

		needsSubmit = chosen.hasBeenUsed || submission.waitSemaphoreCount > 0 || submission.acquireKeyedMutex;
		auto const acquiresFromExternal = chosen.hasBeenUsed && isExported;
		submission.image = chosen.image;
		submission.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
		submission.newLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
		submission.srcAccess = 0;
		submission.dstAccess = VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
		submission.srcQueueFamily = acquiresFromExternal ? VK_QUEUE_FAMILY_EXTERNAL : VK_QUEUE_FAMILY_IGNORED;
		submission.dstQueueFamily = acquiresFromExternal ? getGraphicsQueueFamilyIndex() : VK_QUEUE_FAMILY_IGNORED;
		submission.signalSemaphore = chosen.acquireSemaphore;
		acquireSemaphore = chosen.acquireSemaphore;
		chosen.hasBeenUsed = true;
	}

	if (completionFenceToRecycle != VK_NULL_HANDLE) {
		exportable_check_vk(vkWaitForFences(swapChain->device, 1, &completionFenceToRecycle, VK_TRUE, UINT64_MAX), "vkWaitForFences");
		exportable_check_vk(vkResetFences(swapChain->device, 1, &completionFenceToRecycle), "vkResetFences");
	}

	outImageSyncData->imageIndex = chosenIndex;
	outImageSyncData->imageReadySemaphore = VK_NULL_HANDLE;
	if (needsSubmit) {
		submit(swapChain, submission);
		outImageSyncData->imageReadySemaphore = acquireSemaphore;
	}
	return VK_SUCCESS;
}

VkResult tffr_vulkan_platform::present(SwapChainPtr handle, uint32_t index, VkSemaphore finishedDrawing) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) return tffr_vulkan_platform_base::present(handle, index, finishedDrawing);

	auto descriptor = swapChain->descriptor;
	auto const isExported = index != ScratchSlotIndex;
	exportable_submission submission;
	{
		std::lock_guard lock { descriptor->mutex };
		auto& slot = descriptor->slots[index];
		submission.image = slot.image;
		if (isExported && uses_semaphores(descriptor)) submission.signalSemaphore = slot.readySemaphore;
		if (isExported && uses_keyed_mutex(descriptor)) {
			submission.keyedMutexMemory = slot.memory;
			submission.releaseKeyedMutex = true;
			submission.keyedMutexReleaseKey = 1;
			slot.keyedMutexKey = 1;
		}
		if (isExported && uses_completion_fence(descriptor)) {
			submission.completionFence = slot.completionFence;
			slot.completionFencePending = true;
		}
	}
	submission.oldLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
	submission.newLayout = isExported ? descriptor->exportedLayout : VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
	submission.srcAccess = VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
	submission.dstAccess = 0;
	submission.srcQueueFamily = isExported ? getGraphicsQueueFamilyIndex() : VK_QUEUE_FAMILY_IGNORED;
	submission.dstQueueFamily = isExported ? VK_QUEUE_FAMILY_EXTERNAL : VK_QUEUE_FAMILY_IGNORED;
	if (finishedDrawing != VK_NULL_HANDLE) submission.waitSemaphores[submission.waitSemaphoreCount++] = finishedDrawing;

	submit(swapChain, submission);

	if (isExported) {
		std::lock_guard lock { descriptor->mutex };
		auto& slot = descriptor->slots[index];
		slot.state = exportable_image_state::presented;
		descriptor->presentedQueue.push_back(index);
	}
	return VK_SUCCESS;
}

bool tffr_vulkan_platform::hasResized(SwapChainPtr handle) {
	if (as_exportable(handle) == nullptr) return tffr_vulkan_platform_base::hasResized(handle);
	return false;
}
bool tffr_vulkan_platform::isProtected(SwapChainPtr handle) {
	if (as_exportable(handle) == nullptr) return tffr_vulkan_platform_base::isProtected(handle);
	return false;
}
VkResult tffr_vulkan_platform::recreate(SwapChainPtr handle) {
	if (as_exportable(handle) == nullptr) return tffr_vulkan_platform_base::recreate(handle);
	return VK_SUCCESS;
}
void tffr_vulkan_platform::destroy(SwapChainPtr handle) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) {
		tffr_vulkan_platform_base::destroy(handle);
		return;
	}
	destroy_exportable_swap_chain(swapChain);
}

void exportable_surface_backend::get_support(int32_t handleType, interop_bool* outSupported, int32_t* outSyncKind) {
	if (tffr_vulkan_platform::instance == nullptr || !tffr_vulkan_platform::instance->supports_handle_type(handleType)) return;
	*outSupported = interop_bool_true;
	*outSyncKind = sync_kind_for(handleType);
}

void exportable_surface_backend::get_device_ids(uint8_t* outDeviceUuid, uint8_t* outDeviceLuid, interop_bool* outLuidValid) {
	auto platform = tffr_vulkan_platform::instance;
	if (platform == nullptr) return;
	std::memcpy(outDeviceUuid, platform->deviceUuid, EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH);
	std::memcpy(outDeviceLuid, platform->deviceLuid, EXPORTABLE_SURFACE_DEVICE_LUID_LENGTH);
	*outLuidValid = platform->deviceLuidValid ? interop_bool_true : interop_bool_false;
}

ExportableSurfaceDescriptorHandle exportable_surface_backend::allocate_descriptor(uint32_t width, uint32_t height, int32_t handleType) {
	ThrowIf(tffr_vulkan_platform::instance == nullptr || !tffr_vulkan_platform::instance->supports_handle_type(handleType), "Exportable surfaces with this handle type are not supported on this device.");
	ThrowIf(width == 0 || height == 0, "Exportable surface dimensions must be positive.");
	auto descriptor = new exportable_surface_descriptor();
	descriptor->handleType = handleType;
	if (handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD) {
		descriptor->colorFormat = VK_FORMAT_R8G8B8A8_UNORM;
		descriptor->exportedLayout = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
	}
	else {
		descriptor->colorFormat = VK_FORMAT_B8G8R8A8_UNORM;
		descriptor->exportedLayout = VK_IMAGE_LAYOUT_GENERAL;
	}
	descriptor->width = width;
	descriptor->height = height;
	tffr_vulkan_platform::instance->register_descriptor(descriptor);
	return descriptor;
}

void exportable_surface_backend::set_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	std::lock_guard lock { descriptor->mutex };
	descriptor->width = width;
	descriptor->height = height;
}

void exportable_surface_backend::dispose_descriptor(ExportableSurfaceDescriptorHandle descriptor) {
	if (tffr_vulkan_platform::instance != nullptr) tffr_vulkan_platform::instance->unregister_descriptor(descriptor);
	filament_engine->flushAndWait();
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
	int64_t imageHandle = 0;
	int64_t readyHandle = 0;
	int64_t releasedHandle = 0;
	exportable_check_vk(tffr_vulkan_platform::instance->export_image_handles(descriptor, slot, &imageHandle, &readyHandle, &releasedHandle), "export_image_handles");
	*outImageHandle = imageHandle;
	*outImageSizeBytes = slot.memorySize;
	*outReadyHandle = readyHandle;
	*outReleasedHandle = releasedHandle;
	return true;
}

void exportable_surface_backend::close_handle(int32_t handleType, int64_t handle) {
	tffr_vulkan_platform::close_exported_handle(handleType, handle);
}

bool exportable_surface_backend::take_latest_presented_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t* outIndex) {
	std::lock_guard lock { descriptor->mutex };
	if (!descriptor->hasLiveSwapChain || descriptor->generation != generation || descriptor->presentedQueue.empty()) return false;

	auto latestPosition = descriptor->presentedQueue.size() - 1U;
	if (uses_completion_fence(descriptor)) {
		auto device = tffr_vulkan_platform::instance->getDevice();
		auto found = false;
		for (auto position = descriptor->presentedQueue.size(); position > 0; --position) {
			auto const& slot = descriptor->slots[descriptor->presentedQueue[position - 1U]];
			if (vkGetFenceStatus(device, slot.completionFence) != VK_SUCCESS) continue;
			latestPosition = position - 1U;
			found = true;
			break;
		}
		if (!found) return false;
	}

	auto latest = descriptor->presentedQueue[latestPosition];
	for (size_t position = 0; position < latestPosition; ++position) {
		descriptor->slots[descriptor->presentedQueue[position]].state = exportable_image_state::pending_discard;
	}
	descriptor->presentedQueue.erase(descriptor->presentedQueue.begin(), descriptor->presentedQueue.begin() + static_cast<std::ptrdiff_t>(latestPosition) + 1);
	descriptor->slots[latest].state = exportable_image_state::handed;
	*outIndex = latest;
	return true;
}

void exportable_surface_backend::return_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, bool consumerSignalledRelease) {
	std::lock_guard lock { descriptor->mutex };
	if (descriptor->generation != generation || !descriptor->hasLiveSwapChain) return;
	auto& slot = descriptor->slots[index];
	if (slot.state != exportable_image_state::handed) return;
	if (uses_completion_fence(descriptor)) {
		slot.state = exportable_image_state::released;
		return;
	}
	if (consumerSignalledRelease && uses_keyed_mutex(descriptor)) slot.keyedMutexKey = 0;
	slot.state = consumerSignalledRelease ? exportable_image_state::released : exportable_image_state::pending_discard;
}

#endif
