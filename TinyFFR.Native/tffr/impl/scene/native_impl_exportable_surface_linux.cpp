#include "pch.h"
#include "scene/native_impl_exportable_surface_vulkan.h"

#if defined(TFFR_LINUX)
#include <unistd.h>
#include <cstring>

using namespace bluevk;

namespace {
	constexpr VkImageUsageFlags SharedColorUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_SAMPLED_BIT;

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
}

filament::backend::VulkanPlatform::ExtensionSet tffr_vulkan_platform::getRequiredDeviceExtensions() {
	return {
		VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME,
		VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME
	};
}

uint32_t tffr_vulkan_platform::evaluate_os_supported_handle_types(VkPhysicalDevice physicalDevice) {
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME)) return 0;
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME)) return 0;
	if (vkGetMemoryFdKHR == nullptr || vkGetSemaphoreFdKHR == nullptr) return 0;

	VkPhysicalDeviceExternalImageFormatInfo externalImageInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_IMAGE_FORMAT_INFO,
		.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkPhysicalDeviceImageFormatInfo2 imageFormatInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2,
		.pNext = &externalImageInfo,
		.format = VK_FORMAT_R8G8B8A8_UNORM,
		.type = VK_IMAGE_TYPE_2D,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = SharedColorUsage,
		.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT
	};
	VkExternalImageFormatProperties externalImageProperties { .sType = VK_STRUCTURE_TYPE_EXTERNAL_IMAGE_FORMAT_PROPERTIES };
	VkImageFormatProperties2 imageFormatProperties { .sType = VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2, .pNext = &externalImageProperties };
	if (vkGetPhysicalDeviceImageFormatProperties2(physicalDevice, &imageFormatInfo, &imageFormatProperties) != VK_SUCCESS) return 0;
	if ((externalImageProperties.externalMemoryProperties.externalMemoryFeatures & VK_EXTERNAL_MEMORY_FEATURE_EXPORTABLE_BIT) == 0) return 0;

	VkPhysicalDeviceExternalSemaphoreInfo semaphoreInfo {
		.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_SEMAPHORE_INFO,
		.handleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkExternalSemaphoreProperties semaphoreProperties { .sType = VK_STRUCTURE_TYPE_EXTERNAL_SEMAPHORE_PROPERTIES };
	vkGetPhysicalDeviceExternalSemaphoreProperties(physicalDevice, &semaphoreInfo, &semaphoreProperties);
	if ((semaphoreProperties.externalSemaphoreFeatures & VK_EXTERNAL_SEMAPHORE_FEATURE_EXPORTABLE_BIT) == 0) return 0;

	return 1U << EXPORTABLE_SURFACE_HANDLE_TYPE_VULKAN_OPAQUE_FD;
}

void tffr_vulkan_platform::create_shared_image(exportable_surface_descriptor const* descriptor, exportable_image_slot& slot) {
	auto device = getDevice();

	VkExternalMemoryImageCreateInfo externalInfo {
		.sType = VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO,
		.handleTypes = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	VkImageCreateInfo imageInfo {
		.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
		.pNext = &externalInfo,
		.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT,
		.imageType = VK_IMAGE_TYPE_2D,
		.format = descriptor->colorFormat,
		.extent = { descriptor->liveWidth, descriptor->liveHeight, 1 },
		.mipLevels = 1,
		.arrayLayers = 1,
		.samples = VK_SAMPLE_COUNT_1_BIT,
		.tiling = VK_IMAGE_TILING_OPTIMAL,
		.usage = SharedColorUsage,
		.sharingMode = VK_SHARING_MODE_EXCLUSIVE,
		.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED
	};
	exportable_check_vk(vkCreateImage(device, &imageInfo, nullptr, &slot.image), "vkCreateImage");

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
		.pNext = &exportInfo,
		.image = slot.image
	};
	VkMemoryAllocateInfo allocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.pNext = useDedicatedAllocation ? static_cast<void const*>(&dedicatedInfo) : static_cast<void const*>(&exportInfo),
		.allocationSize = requirements.size,
		.memoryTypeIndex = find_memory_type(requirements.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
	};
	exportable_check_vk(vkAllocateMemory(device, &allocateInfo, nullptr, &slot.memory), "vkAllocateMemory");
	exportable_check_vk(vkBindImageMemory(device, slot.image, slot.memory, 0), "vkBindImageMemory");
	slot.memorySize = requirements.size;

	slot.readySemaphore = create_semaphore(VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT);
	slot.releasedSemaphore = create_semaphore(VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT);
}

void tffr_vulkan_platform::release_shared_image_os_resources(exportable_image_slot& slot) { }

void const* tffr_vulkan_platform::build_submit_extension_chain(exportable_submission const& submission, exportable_submit_extension_storage& storage) {
	return nullptr;
}

VkResult tffr_vulkan_platform::export_image_handles(exportable_surface_descriptor const* descriptor, exportable_image_slot const& slot, int64_t* outImageHandle, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	auto device = getDevice();
	VkMemoryGetFdInfoKHR imageInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_GET_FD_INFO_KHR,
		.memory = slot.memory,
		.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
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
	int imageFd = -1;
	int readyFd = -1;
	int releasedFd = -1;
	auto result = vkGetMemoryFdKHR(device, &imageInfo, &imageFd);
	if (result != VK_SUCCESS) return result;
	result = vkGetSemaphoreFdKHR(device, &readyInfo, &readyFd);
	if (result != VK_SUCCESS) {
		::close(imageFd);
		return result;
	}
	result = vkGetSemaphoreFdKHR(device, &releasedInfo, &releasedFd);
	if (result != VK_SUCCESS) {
		::close(imageFd);
		::close(readyFd);
		return result;
	}
	*outImageHandle = imageFd;
	*outReadyHandle = readyFd;
	*outReleasedHandle = releasedFd;
	return VK_SUCCESS;
}

void tffr_vulkan_platform::close_exported_handle(int32_t handleType, int64_t handle) {
	if (handle >= 0) ::close(static_cast<int>(handle));
}

void tffr_vulkan_platform::release_os_resources() { }

#endif
