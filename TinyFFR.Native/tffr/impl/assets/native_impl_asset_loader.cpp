#include "pch.h"
#include "assets/native_impl_asset_loader.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#define STBI_FAILURE_USERMSG
#include "stb/stb_imageh.h"

void native_impl_asset_loader::load_asset_file_in_to_memory(const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outAssetHandle, "Out asset handle pointer was null.");
	unsigned int flags = aiProcess_CalcTangentSpace | aiProcess_GenNormals | aiProcess_GenBoundingBoxes | aiProcess_GenUVCoords | aiProcess_Triangulate;
	if (fixCommonExporterErrors) flags |= aiProcess_FindDegenerates | aiProcess_FindInstances | aiProcess_FindInvalidData | aiProcess_FixInfacingNormals | aiProcess_GenNormals;
	if (optimize) flags |= aiProcess_ImproveCacheLocality | aiProcess_JoinIdenticalVertices | aiProcess_OptimizeGraph | aiProcess_OptimizeMeshes;
	*outAssetHandle = aiImportFile(filePath, flags);
	ThrowIfNull(*outAssetHandle, "Could not load asset '", filePath, "': ", aiGetErrorString());
}
StartExportedFunc(load_asset_file_in_to_memory, const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	native_impl_asset_loader::load_asset_file_in_to_memory(filePath, fixCommonExporterErrors, optimize, outAssetHandle);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_mesh_count(MemoryLoadedAssetHandle assetHandle, int32_t* outMeshCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outMeshCount, "Out mesh count pointer was null.");
	*outMeshCount = static_cast<int32_t>(assetHandle->mNumMeshes);
}
StartExportedFunc(get_loaded_asset_mesh_count, MemoryLoadedAssetHandle assetHandle, int32_t* outMeshCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_count(assetHandle, outMeshCount);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_material_count(MemoryLoadedAssetHandle assetHandle, int32_t* outMaterialCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outMaterialCount, "Out material count pointer was null.");
	*outMaterialCount = static_cast<int32_t>(assetHandle->mNumMaterials);
}
StartExportedFunc(get_loaded_asset_material_count, MemoryLoadedAssetHandle assetHandle, int32_t* outMaterialCount) {
	native_impl_asset_loader::get_loaded_asset_material_count(assetHandle, outMaterialCount);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_texture_count(MemoryLoadedAssetHandle assetHandle, int32_t* outTextureCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outTextureCount, "Out texture count pointer was null.");
	*outTextureCount = static_cast<int32_t>(assetHandle->mNumTextures);
}
StartExportedFunc(get_loaded_asset_texture_count, MemoryLoadedAssetHandle assetHandle, int32_t* outTextureCount) {
	native_impl_asset_loader::get_loaded_asset_texture_count(assetHandle, outTextureCount);
	EndExportedFunc
}



void native_impl_asset_loader::unload_asset_file_from_memory(MemoryLoadedAssetHandle assetHandle) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	aiReleaseImport(assetHandle);
}
StartExportedFunc(unload_asset_file_from_memory, MemoryLoadedAssetHandle assetHandle) {
	native_impl_asset_loader::unload_asset_file_from_memory(assetHandle);
	EndExportedFunc
}






void native_impl_asset_loader::load_texture_file_in_to_memory(const char* filePath, interop_bool includeWAlphaChannel, int32_t* outWidth, int32_t* outHeight, MemoryLoadedTextureRgba32DataPtr* outTextureData) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outTextureData, "Out texture data pointer was null.");
	int width, height, channelCount;
	stbi_set_flip_vertically_on_load(true);
	auto imageData = stbi_load(filePath, &width, &height, &channelCount, includeWAlphaChannel ? 4 : 3);
	ThrowIfNull(imageData, "Could not load texture '", filePath, "': ", stbi_failure_reason());
	*outTextureData = imageData;
	*outWidth = static_cast<int32_t>(width);
	*outHeight = static_cast<int32_t>(height);
}
StartExportedFunc(load_texture_file_in_to_memory, const char* filePath, interop_bool includeWAlphaChannel, int32_t* outWidth, int32_t* outHeight, MemoryLoadedTextureRgba32DataPtr* outTextureHandle) {
	native_impl_asset_loader::load_texture_file_in_to_memory(filePath, includeWAlphaChannel, outWidth, outHeight, outTextureHandle);
	EndExportedFunc
}

void native_impl_asset_loader::unload_texture_file_from_memory(MemoryLoadedTextureRgba32DataPtr textureData) {
	ThrowIfNull(textureData, "Texture data pointer was null.");
	stbi_image_free((void*)textureData);
}
StartExportedFunc(unload_texture_file_from_memory, MemoryLoadedTextureRgba32DataPtr textureData) {
	native_impl_asset_loader::unload_texture_file_from_memory(textureData);
	EndExportedFunc
}