#include "pch.h"
#include "scene/native_impl_exportable_surface.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/Engine.h"

#if defined(TFFR_LINUX)
#include <unistd.h>
#include <cstring>

using namespace bluevk;

namespace {
	uint8_t exportable_device_uuid[EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH] = {};

	constexpr VkFormat ExportableColorFormat = VK_FORMAT_R8G8B8A8_UNORM;
	constexpr VkImageUsageFlags ExportableColorUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_SAMPLED_BIT;
	constexpr uint32_t ScratchSlotIndex = EXPORTABLE_SURFACE_IMAGE_COUNT;

	bool device_supports_extension(VkPhysicalDevice physicalDevice, const char* extensionName) {
		uint32_t count = 0;
		if (vkEnumerateDeviceExtensionProperties(physicalDevice, nullptr, &count, nullptr) != VK_SUCCESS) return false;
		std::vector<VkExtensionProperties> properties(count);
		if (vkEnumerateDeviceExtensionProperties(physicalDevice, nullptr, &count, properties.data()) != VK_SUCCESS) return false;
		for (auto const& p : properties) {
			if (std::strcmp(p.extensionName, extensionName) == 0) return true;
		}
		return false;
	}

	void check_vk(VkResult result, const char* operation) {
		if (result == VK_SUCCESS) return;
		char resultStr[16];
		interop_utils::int_str(resultStr, sizeof(resultStr), static_cast<int>(result));
		Throw(operation, " failed with VkResult ", resultStr, ".");
	}
}

tffr_vulkan_platform* tffr_vulkan_platform::instance = nullptr;

filament::backend::VulkanPlatform::ExtensionSet tffr_vulkan_platform::getRequiredDeviceExtensions() {
	return {
		VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME,
		VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME
	};
}

void tffr_vulkan_platform::evaluate_export_support() {
	exportingSupported = false;
	auto physicalDevice = getPhysicalDevice();
	if (physicalDevice == VK_NULL_HANDLE || getDevice() == VK_NULL_HANDLE) return;
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME)) return;
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME)) return;
	if (vkGetMemoryFdKHR == nullptr || vkGetSemaphoreFdKHR == nullptr) return;
	if (vkGetPhysicalDeviceImageFormatProperties2 == nullptr || vkGetPhysicalDeviceExternalSemaphoreProperties == nullptr || vkGetPhysicalDeviceProperties2 == nullptr || vkGetImageMemoryRequirements2 == nullptr) return;

	VkPhysicalDeviceExternalImageFormatInfo externalImageInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_IMAGE_FORMAT_INFO,
		.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkPhysicalDeviceImageFormatInfo2 imageFormatInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2,
		.pNext = &externalImageInfo,
		.format = ExportableColorFormat,
		.type = VK_IMAGE_TYPE_2D,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = ExportableColorUsage,
		.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT
	};
	VkExternalImageFormatProperties externalImageProperties { .sType = VK_STRUCTURE_TYPE_EXTERNAL_IMAGE_FORMAT_PROPERTIES };
	VkImageFormatProperties2 imageFormatProperties { .sType = VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2, .pNext = &externalImageProperties };
	if (vkGetPhysicalDeviceImageFormatProperties2(physicalDevice, &imageFormatInfo, &imageFormatProperties) != VK_SUCCESS) return;
	if ((externalImageProperties.externalMemoryProperties.externalMemoryFeatures & VK_EXTERNAL_MEMORY_FEATURE_EXPORTABLE_BIT) == 0) return;

	VkPhysicalDeviceExternalSemaphoreInfo semaphoreInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_SEMAPHORE_INFO,
		.handleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkExternalSemaphoreProperties semaphoreProperties { .sType = VK_STRUCTURE_TYPE_EXTERNAL_SEMAPHORE_PROPERTIES };
	vkGetPhysicalDeviceExternalSemaphoreProperties(physicalDevice, &semaphoreInfo, &semaphoreProperties);
	if ((semaphoreProperties.externalSemaphoreFeatures & VK_EXTERNAL_SEMAPHORE_FEATURE_EXPORTABLE_BIT) == 0) return;

	VkPhysicalDeviceIDProperties idProperties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES };
	VkPhysicalDeviceProperties2 deviceProperties { .sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2, .pNext = &idProperties };
	vkGetPhysicalDeviceProperties2(physicalDevice, &deviceProperties);
	std::memcpy(exportable_device_uuid, idProperties.deviceUUID, EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH);

	exportingSupported = true;
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

VkSemaphore tffr_vulkan_platform::create_semaphore(bool exportable) {
	VkExportSemaphoreCreateInfo exportInfo {
		.sType = VK_STRUCTURE_TYPE_EXPORT_SEMAPHORE_CREATE_INFO,
		.handleTypes = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkSemaphoreCreateInfo createInfo {
		.sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
		.pNext = exportable ? &exportInfo : nullptr
	};
	VkSemaphore result;
	check_vk(vkCreateSemaphore(getDevice(), &createInfo, nullptr, &result), "vkCreateSemaphore");
	return result;
}

void tffr_vulkan_platform::create_exportable_image(exportable_swap_chain* swapChain, exportable_image_slot& slot, bool exportable) {
	auto device = getDevice();
	auto descriptor = swapChain->descriptor;

	VkExternalMemoryImageCreateInfo externalInfo {
		.sType = VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO,
		.handleTypes = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkImageCreateInfo imageInfo {
		.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
		.pNext = exportable ? &externalInfo : nullptr,
		.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT,
		.imageType = VK_IMAGE_TYPE_2D,
		.format = ExportableColorFormat,
		.extent = { descriptor->liveWidth, descriptor->liveHeight, 1 },
		.mipLevels = 1,
		.arrayLayers = 1,
		.samples = VK_SAMPLE_COUNT_1_BIT,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = ExportableColorUsage,
		.sharingMode = VK_SHARING_MODE_EXCLUSIVE,
		.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED
	};
	check_vk(vkCreateImage(device, &imageInfo, nullptr, &slot.image), "vkCreateImage");

	VkMemoryDedicatedRequirements dedicatedRequirements { .sType = VK_STRUCTURE_TYPE_MEMORY_DEDICATED_REQUIREMENTS };
	VkMemoryRequirements2 requirements2 { .sType = VK_STRUCTURE_TYPE_MEMORY_REQUIREMENTS_2, .pNext = &dedicatedRequirements };
	VkImageMemoryRequirementsInfo2 requirementsInfo { .sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_REQUIREMENTS_INFO_2, .image = slot.image };
	vkGetImageMemoryRequirements2(device, &requirementsInfo, &requirements2);
	auto const& requirements = requirements2.memoryRequirements;
	auto const useDedicatedAllocation = dedicatedRequirements.requiresDedicatedAllocation == VK_TRUE;

	VkExportMemoryAllocateInfo exportInfo {
		.sType = VK_STRUCTURE_TYPE_EXPORT_MEMORY_ALLOCATE_INFO,
		.handleTypes = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkMemoryDedicatedAllocateInfo dedicatedInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
		.pNext = exportable ? &exportInfo : nullptr,
		.image = slot.image
	};
	VkMemoryAllocateInfo allocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.pNext = useDedicatedAllocation ? static_cast<void const*>(&dedicatedInfo) : (exportable ? static_cast<void const*>(&exportInfo) : nullptr),
		.allocationSize = requirements.size,
		.memoryTypeIndex = find_memory_type(requirements.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
	};
	check_vk(vkAllocateMemory(device, &allocateInfo, nullptr, &slot.memory), "vkAllocateMemory");
	check_vk(vkBindImageMemory(device, slot.image, slot.memory, 0), "vkBindImageMemory");
	slot.memorySize = requirements.size;

	slot.acquireSemaphore = create_semaphore(false);
	if (exportable) {
		slot.readySemaphore = create_semaphore(true);
		slot.releasedSemaphore = create_semaphore(true);
	}
	slot.state = exportable_image_state::free;
	slot.hasBeenUsed = false;
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
	check_vk(vkCreateCommandPool(device, &poolInfo, nullptr, &swapChain->commandPool), "vkCreateCommandPool");
	for (uint32_t i = 0; i < exportable_swap_chain::InitialSubmissionSlotCount; ++i) add_submission_slot(swapChain);

	{
		std::lock_guard lock { descriptor->mutex };
		descriptor->liveWidth = descriptor->width;
		descriptor->liveHeight = descriptor->height;
		for (uint32_t i = 0; i <= ScratchSlotIndex; ++i) {
			descriptor->slots[i] = {};
			create_exportable_image(swapChain, descriptor->slots[i], i != ScratchSlotIndex);
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
	check_vk(vkCreateImage(device, &depthInfo, nullptr, &swapChain->depthImage), "vkCreateImage");
	VkMemoryRequirements depthRequirements;
	vkGetImageMemoryRequirements(device, swapChain->depthImage, &depthRequirements);
	VkMemoryAllocateInfo depthAllocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.allocationSize = depthRequirements.size,
		.memoryTypeIndex = find_memory_type(depthRequirements.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
	};
	check_vk(vkAllocateMemory(device, &depthAllocateInfo, nullptr, &swapChain->depthMemory), "vkAllocateMemory");
	check_vk(vkBindImageMemory(device, swapChain->depthImage, swapChain->depthMemory, 0), "vkBindImageMemory");

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
			if (slot.image != VK_NULL_HANDLE) vkDestroyImage(device, slot.image, nullptr);
			if (slot.memory != VK_NULL_HANDLE) vkFreeMemory(device, slot.memory, nullptr);
			if (slot.readySemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.readySemaphore, nullptr);
			if (slot.releasedSemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.releasedSemaphore, nullptr);
			if (slot.acquireSemaphore != VK_NULL_HANDLE) vkDestroySemaphore(device, slot.acquireSemaphore, nullptr);
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
	check_vk(vkAllocateCommandBuffers(swapChain->device, &bufferInfo, &commandBuffer), "vkAllocateCommandBuffers");
	VkFenceCreateInfo fenceInfo { .sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
	VkFence fence;
	check_vk(vkCreateFence(swapChain->device, &fenceInfo, nullptr, &fence), "vkCreateFence");
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
			check_vk(vkResetFences(device, 1, &swapChain->fences[slot]), "vkResetFences");
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
	check_vk(vkWaitForFences(device, 1, &swapChain->fences[slot], VK_TRUE, UINT64_MAX), "vkWaitForFences");
	check_vk(vkResetFences(device, 1, &swapChain->fences[slot]), "vkResetFences");
	swapChain->fenceSubmitted[slot] = 0;
	swapChain->nextSubmissionSlot = (slot + 1U) % slotCount;
	return slot;
}

void tffr_vulkan_platform::submit(exportable_swap_chain* swapChain, exportable_submission const& submission) {
	auto const slotIndex = acquire_submission_slot(swapChain);
	auto fence = swapChain->fences[slotIndex];
	auto commandBuffer = swapChain->commandBuffers[slotIndex];

	check_vk(vkResetCommandBuffer(commandBuffer, 0), "vkResetCommandBuffer");
	VkCommandBufferBeginInfo beginInfo {
		.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
		.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
	};
	check_vk(vkBeginCommandBuffer(commandBuffer, &beginInfo), "vkBeginCommandBuffer");
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
	check_vk(vkEndCommandBuffer(commandBuffer), "vkEndCommandBuffer");

	VkPipelineStageFlags waitStages[EXPORTABLE_SURFACE_IMAGE_COUNT + 2];
	for (uint32_t i = 0; i < submission.waitSemaphoreCount; ++i) waitStages[i] = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
	VkSubmitInfo submitInfo {
		.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO,
		.waitSemaphoreCount = submission.waitSemaphoreCount,
		.pWaitSemaphores = submission.waitSemaphores,
		.pWaitDstStageMask = waitStages,
		.commandBufferCount = 1,
		.pCommandBuffers = &commandBuffer,
		.signalSemaphoreCount = submission.signalSemaphore != VK_NULL_HANDLE ? 1U : 0U,
		.pSignalSemaphores = submission.signalSemaphore != VK_NULL_HANDLE ? &submission.signalSemaphore : nullptr
	};
	check_vk(vkQueueSubmit(swapChain->queue, 1, &submitInfo, fence), "vkQueueSubmit");
	swapChain->fenceSubmitted[slotIndex] = 1;
}

filament::backend::VulkanPlatform::SwapChainPtr tffr_vulkan_platform::createSwapChain(void* nativeWindow, uint64_t flags, VkExtent2D extent) {
	if (!is_registered_descriptor(nativeWindow)) return VulkanPlatformLinux::createSwapChain(nativeWindow, flags, extent);
	return create_exportable_swap_chain(static_cast<exportable_surface_descriptor*>(nativeWindow));
}

filament::backend::VulkanPlatform::SwapChainBundle tffr_vulkan_platform::getSwapChainBundle(SwapChainPtr handle) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) return VulkanPlatformLinux::getSwapChainBundle(handle);

	auto descriptor = swapChain->descriptor;
	SwapChainBundle result;
	std::lock_guard lock { descriptor->mutex };
	result.colors = utils::FixedCapacityVector<VkImage>::with_capacity(ScratchSlotIndex + 1U);
	for (uint32_t i = 0; i <= ScratchSlotIndex; ++i) result.colors.push_back(descriptor->slots[i].image);
	result.depth = swapChain->depthImage;
	result.colorFormat = ExportableColorFormat;
	result.depthFormat = swapChain->depthFormat;
	result.extent = { descriptor->liveWidth, descriptor->liveHeight };
	result.layerCount = 1;
	result.isProtected = false;
	return result;
}

VkResult tffr_vulkan_platform::acquire(SwapChainPtr handle, ImageSyncData* outImageSyncData) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) return VulkanPlatformLinux::acquire(handle, outImageSyncData);

	auto descriptor = swapChain->descriptor;
	exportable_submission submission;
	auto needsSubmit = false;
	uint32_t chosenIndex;
	VkSemaphore acquireSemaphore;
	{
		std::lock_guard lock { descriptor->mutex };

		for (uint32_t i = 0; i < ScratchSlotIndex; ++i) {
			auto& slot = descriptor->slots[i];
			if (slot.state != exportable_image_state::pending_discard) continue;
			submission.waitSemaphores[submission.waitSemaphoreCount++] = slot.readySemaphore;
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
		if (chosen.state == exportable_image_state::released) {
			submission.waitSemaphores[submission.waitSemaphoreCount++] = chosen.releasedSemaphore;
		}
		if (chosenIndex != ScratchSlotIndex) chosen.state = exportable_image_state::in_flight;

		needsSubmit = chosen.hasBeenUsed || submission.waitSemaphoreCount > 0;
		auto const acquiresFromExternal = chosen.hasBeenUsed && chosenIndex != ScratchSlotIndex;
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
	if (swapChain == nullptr) return VulkanPlatformLinux::present(handle, index, finishedDrawing);

	auto descriptor = swapChain->descriptor;
	auto const isExported = index != ScratchSlotIndex;
	exportable_submission submission;
	{
		std::lock_guard lock { descriptor->mutex };
		auto const& slot = descriptor->slots[index];
		submission.image = slot.image;
		submission.signalSemaphore = isExported ? slot.readySemaphore : VK_NULL_HANDLE;
	}
	submission.oldLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
	submission.newLayout = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
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
	if (as_exportable(handle) == nullptr) return VulkanPlatformLinux::hasResized(handle);
	return false;
}
bool tffr_vulkan_platform::isProtected(SwapChainPtr handle) {
	if (as_exportable(handle) == nullptr) return VulkanPlatformLinux::isProtected(handle);
	return false;
}
VkResult tffr_vulkan_platform::recreate(SwapChainPtr handle) {
	if (as_exportable(handle) == nullptr) return VulkanPlatformLinux::recreate(handle);
	return VK_SUCCESS;
}
void tffr_vulkan_platform::destroy(SwapChainPtr handle) {
	auto swapChain = as_exportable(handle);
	if (swapChain == nullptr) {
		VulkanPlatformLinux::destroy(handle);
		return;
	}
	destroy_exportable_swap_chain(swapChain);
}

#else

struct exportable_surface_descriptor { };

#endif

void native_impl_exportable_surface::get_exportable_surface_capabilities(int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid) {
	ThrowIfNull(outHandleType, "Handle type out pointer was null.");
	ThrowIfNull(outSyncKind, "Sync kind out pointer was null.");
	ThrowIfNull(outDeviceUuid, "Device UUID out pointer was null.");
	*outHandleType = EXPORTABLE_SURFACE_HANDLE_TYPE_NONE;
	*outSyncKind = EXPORTABLE_SURFACE_SYNC_KIND_NONE;
#if defined(TFFR_LINUX)
	if (tffr_vulkan_platform::instance == nullptr || !tffr_vulkan_platform::instance->exportingSupported) return;
	*outHandleType = EXPORTABLE_SURFACE_HANDLE_TYPE_OPAQUE_FD;
	*outSyncKind = EXPORTABLE_SURFACE_SYNC_KIND_SEMAPHORES;
	std::memcpy(outDeviceUuid, exportable_device_uuid, EXPORTABLE_SURFACE_DEVICE_UUID_LENGTH);
#endif
}
StartExportedFunc(get_exportable_surface_capabilities, int32_t* outHandleType, int32_t* outSyncKind, uint8_t* outDeviceUuid) {
	native_impl_exportable_surface::get_exportable_surface_capabilities(outHandleType, outSyncKind, outDeviceUuid);
	EndExportedFunc
}

void native_impl_exportable_surface::allocate_exportable_surface_descriptor(uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor) {
	ThrowIfNull(outDescriptor, "Descriptor out pointer was null.");
#if defined(TFFR_LINUX)
	ThrowIf(tffr_vulkan_platform::instance == nullptr || !tffr_vulkan_platform::instance->exportingSupported, "Exportable surfaces are not supported on this device.");
	ThrowIf(width == 0 || height == 0, "Exportable surface dimensions must be positive.");
	auto descriptor = new exportable_surface_descriptor();
	descriptor->width = width;
	descriptor->height = height;
	tffr_vulkan_platform::instance->register_descriptor(descriptor);
	*outDescriptor = descriptor;
#else
	Throw("Exportable surfaces are not supported on this platform.");
#endif
}
StartExportedFunc(allocate_exportable_surface_descriptor, uint32_t width, uint32_t height, ExportableSurfaceDescriptorHandle* outDescriptor) {
	native_impl_exportable_surface::allocate_exportable_surface_descriptor(width, height, outDescriptor);
	EndExportedFunc
}

void native_impl_exportable_surface::set_exportable_surface_descriptor_size(ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIf(width == 0 || height == 0, "Exportable surface dimensions must be positive.");
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	descriptor->width = width;
	descriptor->height = height;
#endif
}
StartExportedFunc(set_exportable_surface_descriptor_size, ExportableSurfaceDescriptorHandle descriptor, uint32_t width, uint32_t height) {
	native_impl_exportable_surface::set_exportable_surface_descriptor_size(descriptor, width, height);
	EndExportedFunc
}

void native_impl_exportable_surface::dispose_exportable_surface_descriptor(ExportableSurfaceDescriptorHandle descriptor) {
	ThrowIfNull(descriptor, "Descriptor was null.");
#if defined(TFFR_LINUX)
	if (tffr_vulkan_platform::instance != nullptr) tffr_vulkan_platform::instance->unregister_descriptor(descriptor);
	filament_engine->flushAndWait();
	delete descriptor;
#endif
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
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	*outGeneration = descriptor->generation;
	*outIsLive = descriptor->hasLiveSwapChain ? interop_bool_true : interop_bool_false;
	*outWidth = descriptor->liveWidth;
	*outHeight = descriptor->liveHeight;
#else
	*outGeneration = 0;
	*outIsLive = interop_bool_false;
	*outWidth = 0;
	*outHeight = 0;
#endif
}
StartExportedFunc(get_exportable_surface_generation, ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, interop_bool* outIsLive, uint32_t* outWidth, uint32_t* outHeight) {
	native_impl_exportable_surface::get_exportable_surface_generation(descriptor, outGeneration, outIsLive, outWidth, outHeight);
	EndExportedFunc
}

void native_impl_exportable_surface::export_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outHandle, uint64_t* outMemorySizeBytes) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outHandle, "Handle out pointer was null.");
	ThrowIfNull(outMemorySizeBytes, "Memory size out pointer was null.");
	ThrowIf(index >= EXPORTABLE_SURFACE_IMAGE_COUNT, "Image index out of range.");
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	ThrowIf(!descriptor->hasLiveSwapChain || descriptor->generation != generation, "Exportable surface generation is stale.");
	auto const& slot = descriptor->slots[index];
	VkMemoryGetFdInfoKHR info {
		.sType = VK_STRUCTURE_TYPE_MEMORY_GET_FD_INFO_KHR,
		.memory = slot.memory,
		.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	int fd = -1;
	check_vk(vkGetMemoryFdKHR(tffr_vulkan_platform::instance->getDevice(), &info, &fd), "vkGetMemoryFdKHR");
	*outHandle = fd;
	*outMemorySizeBytes = slot.memorySize;
#else
	Throw("Exportable surfaces are not supported on this platform.");
#endif
}
StartExportedFunc(export_exportable_surface_image, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outHandle, uint64_t* outMemorySizeBytes) {
	native_impl_exportable_surface::export_exportable_surface_image(descriptor, generation, index, outHandle, outMemorySizeBytes);
	EndExportedFunc
}

void native_impl_exportable_surface::export_exportable_surface_semaphores(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outReadyHandle, "Ready handle out pointer was null.");
	ThrowIfNull(outReleasedHandle, "Released handle out pointer was null.");
	ThrowIf(index >= EXPORTABLE_SURFACE_IMAGE_COUNT, "Image index out of range.");
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	ThrowIf(!descriptor->hasLiveSwapChain || descriptor->generation != generation, "Exportable surface generation is stale.");
	auto const& slot = descriptor->slots[index];
	auto device = tffr_vulkan_platform::instance->getDevice();
	VkSemaphoreGetFdInfoKHR readyInfo {
		.sType = VK_STRUCTURE_TYPE_SEMAPHORE_GET_FD_INFO_KHR,
		.semaphore = slot.readySemaphore,
		.handleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkSemaphoreGetFdInfoKHR releasedInfo {
		.sType = VK_STRUCTURE_TYPE_SEMAPHORE_GET_FD_INFO_KHR,
		.semaphore = slot.releasedSemaphore,
		.handleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	int readyFd = -1;
	int releasedFd = -1;
	check_vk(vkGetSemaphoreFdKHR(device, &readyInfo, &readyFd), "vkGetSemaphoreFdKHR");
	auto releasedResult = vkGetSemaphoreFdKHR(device, &releasedInfo, &releasedFd);
	if (releasedResult != VK_SUCCESS) {
		::close(readyFd);
		check_vk(releasedResult, "vkGetSemaphoreFdKHR");
	}
	*outReadyHandle = readyFd;
	*outReleasedHandle = releasedFd;
#else
	Throw("Exportable surfaces are not supported on this platform.");
#endif
}
StartExportedFunc(export_exportable_surface_semaphores, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	native_impl_exportable_surface::export_exportable_surface_semaphores(descriptor, generation, index, outReadyHandle, outReleasedHandle);
	EndExportedFunc
}

void native_impl_exportable_surface::close_exportable_surface_handle(int64_t handle) {
#if defined(TFFR_LINUX)
	if (handle >= 0) ::close(static_cast<int>(handle));
#endif
}
StartExportedFunc(close_exportable_surface_handle, int64_t handle) {
	native_impl_exportable_surface::close_exportable_surface_handle(handle);
	EndExportedFunc
}

void native_impl_exportable_surface::take_latest_presented_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, uint32_t* outIndex, interop_bool* outFound) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIfNull(outGeneration, "Generation out pointer was null.");
	ThrowIfNull(outIndex, "Index out pointer was null.");
	ThrowIfNull(outFound, "Found out pointer was null.");
	*outFound = interop_bool_false;
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	if (descriptor->presentedQueue.empty()) return;
	auto latest = descriptor->presentedQueue.back();
	descriptor->presentedQueue.pop_back();
	for (auto superseded : descriptor->presentedQueue) {
		descriptor->slots[superseded].state = exportable_image_state::pending_discard;
	}
	descriptor->presentedQueue.clear();
	descriptor->slots[latest].state = exportable_image_state::handed;
	*outGeneration = descriptor->generation;
	*outIndex = latest;
	*outFound = interop_bool_true;
#endif
}
StartExportedFunc(take_latest_presented_exportable_surface_image, ExportableSurfaceDescriptorHandle descriptor, uint32_t* outGeneration, uint32_t* outIndex, interop_bool* outFound) {
	native_impl_exportable_surface::take_latest_presented_exportable_surface_image(descriptor, outGeneration, outIndex, outFound);
	EndExportedFunc
}

void native_impl_exportable_surface::return_exportable_surface_image(ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease) {
	ThrowIfNull(descriptor, "Descriptor was null.");
	ThrowIf(index >= EXPORTABLE_SURFACE_IMAGE_COUNT, "Image index out of range.");
#if defined(TFFR_LINUX)
	std::lock_guard lock { descriptor->mutex };
	if (descriptor->generation != generation || !descriptor->hasLiveSwapChain) return;
	auto& slot = descriptor->slots[index];
	if (slot.state != exportable_image_state::handed) return;
	slot.state = consumerSignalledRelease ? exportable_image_state::released : exportable_image_state::pending_discard;
#endif
}
StartExportedFunc(return_exportable_surface_image, ExportableSurfaceDescriptorHandle descriptor, uint32_t generation, uint32_t index, interop_bool consumerSignalledRelease) {
	native_impl_exportable_surface::return_exportable_surface_image(descriptor, generation, index, consumerSignalledRelease);
	EndExportedFunc
}
