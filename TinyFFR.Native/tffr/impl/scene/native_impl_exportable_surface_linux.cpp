#include "pch.h"
#include "scene/native_impl_exportable_surface_vulkan.h"

#if defined(TFFR_LINUX)
#include <unistd.h>
#include <cstring>

using namespace bluevk;

namespace {
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

bool tffr_vulkan_platform::device_supports_handle_export(VkPhysicalDevice physicalDevice) {
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME)) return false;
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME)) return false;
	return vkGetMemoryFdKHR != nullptr && vkGetSemaphoreFdKHR != nullptr;
}

VkResult tffr_vulkan_platform::export_memory_handle(VkDeviceMemory memory, int64_t* outHandle) {
	VkMemoryGetFdInfoKHR info {
		.sType = VK_STRUCTURE_TYPE_MEMORY_GET_FD_INFO_KHR,
		.memory = memory,
		.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	int fd = -1;
	auto result = vkGetMemoryFdKHR(getDevice(), &info, &fd);
	*outHandle = fd;
	return result;
}

VkResult tffr_vulkan_platform::export_semaphore_handle(VkSemaphore semaphore, int64_t* outHandle) {
	VkSemaphoreGetFdInfoKHR info {
		.sType = VK_STRUCTURE_TYPE_SEMAPHORE_GET_FD_INFO_KHR,
		.semaphore = semaphore,
		.handleType = VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT
	};
	int fd = -1;
	auto result = vkGetSemaphoreFdKHR(getDevice(), &info, &fd);
	*outHandle = fd;
	return result;
}

void tffr_vulkan_platform::close_exported_handle(int64_t handle) {
	if (handle >= 0) ::close(static_cast<int>(handle));
}

#endif
