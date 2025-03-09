#pragma once

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "native_impl_render_assets.h"
#include "assimp/cimport.h"
#include "assimp/scene.h"
#include "assimp/postprocess.h"
#include "filament/Skybox.h"
#include "filament/IndirectLight.h"

typedef const aiScene* MemoryLoadedAssetHandle;
typedef const unsigned char* MemoryLoadedTextureRgba32DataPtr;
typedef Skybox* SkyboxHandle;
typedef IndirectLight* IndirectLightHandle;

class native_impl_asset_loader {
public:
	static void load_asset_file_in_to_memory(const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle);
	static void get_loaded_asset_mesh_count(MemoryLoadedAssetHandle assetHandle, int32_t* outMeshCount);
	static void get_loaded_asset_material_count(MemoryLoadedAssetHandle assetHandle, int32_t* outMaterialCount);
	static void get_loaded_asset_texture_count(MemoryLoadedAssetHandle assetHandle, int32_t* outTextureCount);
	static void get_loaded_asset_mesh_vertex_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outVertexCount);
	static void get_loaded_asset_mesh_triangle_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outTriangleCount);
	static void copy_loaded_asset_mesh_vertices(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer);
	static void copy_loaded_asset_mesh_triangles(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeTriangles, int32_t* buffer);
	static void unload_asset_file_from_memory(MemoryLoadedAssetHandle assetHandle);

	static void load_texture_file_in_to_memory(const char* filePath, interop_bool includeWAlphaChannel, int32_t* outWidth, int32_t* outHeight, MemoryLoadedTextureRgba32DataPtr* outTextureData);
	static void unload_texture_file_from_memory(MemoryLoadedTextureRgba32DataPtr textureData);

	static void load_skybox_file_in_to_memory(uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle, SkyboxHandle* outSkyboxHandle);
	static void unload_skybox_file_from_memory(TextureHandle textureHandle, SkyboxHandle skyboxHandle);
	static void load_ibl_file_in_to_memory(uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle, IndirectLightHandle* outLightHandle);
	static void unload_ibl_file_from_memory(TextureHandle textureHandle, IndirectLightHandle lightHandle);
};