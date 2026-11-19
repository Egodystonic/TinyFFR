#include "pch.h"
#if defined(TFFR_WIN)
#include <d3d11_1.h>
#include <dxgi1_2.h>
#endif
#include "scene/native_impl_exportable_surface_vulkan.h"

#if defined(TFFR_WIN)
#include <cstring>
#include <new>

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxgi.lib")

using namespace bluevk;

namespace {
	constexpr VkImageUsageFlags SharedColorUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_SAMPLED_BIT;
	constexpr DWORD KeyedMutexAcquireTimeoutMs = INFINITE;

	ID3D11Device* shared_texture_device = nullptr;

	struct keyed_mutex_submit_extension {
		VkWin32KeyedMutexAcquireReleaseInfoKHR info;
		VkDeviceMemory acquireMemory;
		uint64_t acquireKey;
		uint32_t acquireTimeout;
		VkDeviceMemory releaseMemory;
		uint64_t releaseKey;
	};
	static_assert(sizeof(keyed_mutex_submit_extension) <= sizeof(exportable_submit_extension_storage));

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

	bool device_can_import(VkPhysicalDevice physicalDevice, VkExternalMemoryHandleTypeFlagBits handleType) {
		VkPhysicalDeviceExternalImageFormatInfo externalImageInfo {
			.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_IMAGE_FORMAT_INFO,
			.handleType = handleType
		};
		VkPhysicalDeviceImageFormatInfo2 imageFormatInfo {
			.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2,
			.pNext = &externalImageInfo,
			.format = VK_FORMAT_B8G8R8A8_UNORM,
			.type = VK_IMAGE_TYPE_2D,
			.tiling = VK_IMAGE_TILING_OPTIMAL,
			.usage = SharedColorUsage,
			.flags = 0
		};
		VkExternalImageFormatProperties externalImageProperties { .sType = VK_STRUCTURE_TYPE_EXTERNAL_IMAGE_FORMAT_PROPERTIES };
		VkImageFormatProperties2 imageFormatProperties { .sType = VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2, .pNext = &externalImageProperties };
		if (vkGetPhysicalDeviceImageFormatProperties2(physicalDevice, &imageFormatInfo, &imageFormatProperties) != VK_SUCCESS) return false;
		return (externalImageProperties.externalMemoryProperties.externalMemoryFeatures & VK_EXTERNAL_MEMORY_FEATURE_IMPORTABLE_BIT) != 0;
	}

	ID3D11Device* create_device_on_adapter(uint8_t const* luid) {
		IDXGIFactory1* factory = nullptr;
		if (FAILED(CreateDXGIFactory1(__uuidof(IDXGIFactory1), reinterpret_cast<void**>(&factory)))) return nullptr;
		ID3D11Device* result = nullptr;
		IDXGIAdapter1* adapter = nullptr;
		for (UINT i = 0; factory->EnumAdapters1(i, &adapter) != DXGI_ERROR_NOT_FOUND; ++i) {
			DXGI_ADAPTER_DESC1 desc;
			auto const matches = SUCCEEDED(adapter->GetDesc1(&desc)) && std::memcmp(&desc.AdapterLuid, luid, EXPORTABLE_SURFACE_DEVICE_LUID_LENGTH) == 0;
			if (matches) {
				if (FAILED(D3D11CreateDevice(adapter, D3D_DRIVER_TYPE_UNKNOWN, nullptr, D3D11_CREATE_DEVICE_BGRA_SUPPORT, nullptr, 0, D3D11_SDK_VERSION, &result, nullptr, nullptr))) result = nullptr;
			}
			adapter->Release();
			if (matches) break;
		}
		factory->Release();
		return result;
	}

	VkExternalMemoryHandleTypeFlagBits vulkan_handle_type_for(int32_t handleType) {
		return handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE
			? VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT
			: VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_KMT_BIT;
	}
}

filament::backend::VulkanPlatform::ExtensionSet tffr_vulkan_platform::getRequiredDeviceExtensions() {
	return {
		VK_KHR_EXTERNAL_MEMORY_WIN32_EXTENSION_NAME,
		VK_KHR_WIN32_KEYED_MUTEX_EXTENSION_NAME
	};
}

uint32_t tffr_vulkan_platform::evaluate_os_supported_handle_types(VkPhysicalDevice physicalDevice) {
	if (!deviceLuidValid) return 0;
	if (!device_supports_extension(physicalDevice, VK_KHR_EXTERNAL_MEMORY_WIN32_EXTENSION_NAME)) return 0;
	if (vkGetMemoryWin32HandlePropertiesKHR == nullptr) return 0;

	if (shared_texture_device == nullptr) shared_texture_device = create_device_on_adapter(deviceLuid);
	if (shared_texture_device == nullptr) return 0;

	uint32_t result = 0;
	if (device_can_import(physicalDevice, VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_KMT_BIT)) {
		result |= 1U << EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_LEGACY_SHARED_HANDLE;
	}
	if (device_supports_extension(physicalDevice, VK_KHR_WIN32_KEYED_MUTEX_EXTENSION_NAME) && device_can_import(physicalDevice, VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT)) {
		result |= 1U << EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE;
	}
	if (result == 0) {
		shared_texture_device->Release();
		shared_texture_device = nullptr;
	}
	return result;
}

void tffr_vulkan_platform::create_shared_image(exportable_surface_descriptor const* descriptor, exportable_image_slot& slot) {
	ThrowIfNull(shared_texture_device, "No D3D11 device is available for exportable surface textures.");
	auto const isNtHandle = descriptor->handleType == EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE;

	D3D11_TEXTURE2D_DESC textureDesc {
		.Width = descriptor->liveWidth,
		.Height = descriptor->liveHeight,
		.MipLevels = 1,
		.ArraySize = 1,
		.Format = DXGI_FORMAT_B8G8R8A8_UNORM,
		.SampleDesc = { 1, 0 },
		.Usage = D3D11_USAGE_DEFAULT,
		.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE,
		.CPUAccessFlags = 0,
		.MiscFlags = isNtHandle
			? static_cast<UINT>(D3D11_RESOURCE_MISC_SHARED_NTHANDLE | D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX)
			: static_cast<UINT>(D3D11_RESOURCE_MISC_SHARED)
	};
	ID3D11Texture2D* texture = nullptr;
	ThrowIf(FAILED(shared_texture_device->CreateTexture2D(&textureDesc, nullptr, &texture)), "Could not create shared D3D11 texture.");
	slot.osTexture = texture;

	IDXGIResource1* dxgiResource = nullptr;
	ThrowIf(FAILED(texture->QueryInterface(__uuidof(IDXGIResource1), reinterpret_cast<void**>(&dxgiResource))), "Could not query shared D3D11 texture as a DXGI resource.");
	HANDLE sharedHandle = nullptr;
	auto const handleResult = isNtHandle
		? dxgiResource->CreateSharedHandle(nullptr, DXGI_SHARED_RESOURCE_READ | DXGI_SHARED_RESOURCE_WRITE, nullptr, &sharedHandle)
		: dxgiResource->GetSharedHandle(&sharedHandle);
	dxgiResource->Release();
	ThrowIf(FAILED(handleResult) || sharedHandle == nullptr, "Could not get a shared handle for D3D11 texture.");
	slot.osSharedHandle = sharedHandle;
	slot.osSharedHandleOwned = isNtHandle;

	auto device = getDevice();
	auto const vulkanHandleType = vulkan_handle_type_for(descriptor->handleType);
	VkExternalMemoryImageCreateInfo externalInfo {
		.sType = VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO,
		.handleTypes = static_cast<VkExternalMemoryHandleTypeFlags>(vulkanHandleType)
	};
	VkImageCreateInfo imageInfo {
		.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
		.pNext = &externalInfo,
		.flags = 0,
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

	VkMemoryWin32HandlePropertiesKHR handleProperties { .sType = VK_STRUCTURE_TYPE_MEMORY_WIN32_HANDLE_PROPERTIES_KHR };
	exportable_check_vk(vkGetMemoryWin32HandlePropertiesKHR(device, vulkanHandleType, sharedHandle, &handleProperties), "vkGetMemoryWin32HandlePropertiesKHR");
	VkMemoryRequirements requirements;
	vkGetImageMemoryRequirements(device, slot.image, &requirements);
	auto const compatibleTypeBits = requirements.memoryTypeBits & handleProperties.memoryTypeBits;
	ThrowIf(compatibleTypeBits == 0, "No Vulkan memory type can import the shared D3D11 texture.");

	VkMemoryDedicatedAllocateInfo dedicatedInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
		.image = slot.image
	};
	VkImportMemoryWin32HandleInfoKHR importInfo {
		.sType = VK_STRUCTURE_TYPE_IMPORT_MEMORY_WIN32_HANDLE_INFO_KHR,
		.pNext = &dedicatedInfo,
		.handleType = vulkanHandleType,
		.handle = sharedHandle
	};
	VkMemoryAllocateInfo allocateInfo {
		.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
		.pNext = &importInfo,
		.allocationSize = requirements.size,
		.memoryTypeIndex = find_memory_type(compatibleTypeBits, 0)
	};
	exportable_check_vk(vkAllocateMemory(device, &allocateInfo, nullptr, &slot.memory), "vkAllocateMemory");
	exportable_check_vk(vkBindImageMemory(device, slot.image, slot.memory, 0), "vkBindImageMemory");
	slot.memorySize = requirements.size;
}

void tffr_vulkan_platform::release_shared_image_os_resources(exportable_image_slot& slot) {
	if (slot.osSharedHandle != nullptr && slot.osSharedHandleOwned) CloseHandle(static_cast<HANDLE>(slot.osSharedHandle));
	if (slot.osTexture != nullptr) static_cast<ID3D11Texture2D*>(slot.osTexture)->Release();
	slot.osSharedHandle = nullptr;
	slot.osSharedHandleOwned = false;
	slot.osTexture = nullptr;
}

void const* tffr_vulkan_platform::build_submit_extension_chain(exportable_submission const& submission, exportable_submit_extension_storage& storage) {
	if (!submission.acquireKeyedMutex && !submission.releaseKeyedMutex) return nullptr;
	auto extension = new (storage.bytes) keyed_mutex_submit_extension {};
	extension->acquireMemory = submission.keyedMutexMemory;
	extension->acquireKey = submission.keyedMutexAcquireKey;
	extension->acquireTimeout = KeyedMutexAcquireTimeoutMs;
	extension->releaseMemory = submission.keyedMutexMemory;
	extension->releaseKey = submission.keyedMutexReleaseKey;
	extension->info = {
		.sType = VK_STRUCTURE_TYPE_WIN32_KEYED_MUTEX_ACQUIRE_RELEASE_INFO_KHR,
		.acquireCount = submission.acquireKeyedMutex ? 1U : 0U,
		.pAcquireSyncs = &extension->acquireMemory,
		.pAcquireKeys = &extension->acquireKey,
		.pAcquireTimeouts = &extension->acquireTimeout,
		.releaseCount = submission.releaseKeyedMutex ? 1U : 0U,
		.pReleaseSyncs = &extension->releaseMemory,
		.pReleaseKeys = &extension->releaseKey
	};
	return &extension->info;
}

VkResult tffr_vulkan_platform::export_image_handles(exportable_surface_descriptor const* descriptor, exportable_image_slot const& slot, int64_t* outImageHandle, int64_t* outReadyHandle, int64_t* outReleasedHandle) {
	*outReadyHandle = 0;
	*outReleasedHandle = 0;
	if (!slot.osSharedHandleOwned) {
		*outImageHandle = reinterpret_cast<int64_t>(slot.osSharedHandle);
		return VK_SUCCESS;
	}
	HANDLE duplicate = nullptr;
	auto const process = GetCurrentProcess();
	if (!DuplicateHandle(process, static_cast<HANDLE>(slot.osSharedHandle), process, &duplicate, 0, FALSE, DUPLICATE_SAME_ACCESS)) return VK_ERROR_OUT_OF_HOST_MEMORY;
	*outImageHandle = reinterpret_cast<int64_t>(duplicate);
	return VK_SUCCESS;
}

void tffr_vulkan_platform::close_exported_handle(int32_t handleType, int64_t handle) {
	if (handleType != EXPORTABLE_SURFACE_HANDLE_TYPE_D3D11_TEXTURE_NT_HANDLE || handle == 0) return;
	CloseHandle(reinterpret_cast<HANDLE>(handle));
}

void tffr_vulkan_platform::release_os_resources() {
	if (shared_texture_device == nullptr) return;
	shared_texture_device->Release();
	shared_texture_device = nullptr;
}

#endif
