#include "pch.h"
#include "assets/native_impl_asset_loader.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/ktxreader/Ktx1Reader.h"

#define STBI_FAILURE_USERMSG
#define STB_IMAGE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#define STB_IMAGE_WRITE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#include "stb/stb_image.h"
#include "stb/stb_image_write.h"

constexpr unsigned int MeshMaxCount = 1000000;
constexpr unsigned int NoAnswerFoundGlobalIndex = MeshMaxCount + 1;

void native_impl_asset_loader::load_asset_file_in_to_memory(const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outAssetHandle, "Out asset handle pointer was null.");
	unsigned int flags = aiProcess_CalcTangentSpace | aiProcess_GenNormals | aiProcess_GenBoundingBoxes | aiProcess_GenUVCoords | aiProcess_Triangulate | aiProcess_SortByPType;
	if (fixCommonExporterErrors) flags |= aiProcess_FindDegenerates | aiProcess_FindInstances | aiProcess_FindInvalidData | aiProcess_FixInfacingNormals | aiProcess_GenNormals;
	if (optimize) flags |= aiProcess_ImproveCacheLocality | aiProcess_JoinIdenticalVertices | aiProcess_OptimizeGraph | aiProcess_OptimizeMeshes;
	*outAssetHandle = aiImportFile(filePath, flags);
	ThrowIfNull(*outAssetHandle, "Could not load asset '", filePath, "': ", aiGetErrorString());
}
StartExportedFunc(load_asset_file_in_to_memory, const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	native_impl_asset_loader::load_asset_file_in_to_memory(filePath, fixCommonExporterErrors, optimize, outAssetHandle);
	EndExportedFunc
}


int32_t count_meshes_in_node_and_children(aiNode* node) {
	auto result = static_cast<int32_t>(node->mNumMeshes);

	for (unsigned int i = 0; i < node->mNumChildren; ++i) {
		result += count_meshes_in_node_and_children(node->mChildren[i]);
	}

	ThrowIf(result > MeshMaxCount || result < 0, "Too many meshes.");
	return result;
}
void native_impl_asset_loader::get_loaded_asset_mesh_count(MemoryLoadedAssetHandle assetHandle, int32_t* outMeshCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outMeshCount, "Out mesh count pointer was null.");

	*outMeshCount = count_meshes_in_node_and_children(assetHandle->mRootNode);
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

void walk_nodes_to_find_global_index_from_mesh_index(aiNode* node, int32_t& startingIndex, int32_t targetIndex, unsigned int& resultGlobalIndex, aiMatrix4x4& resultTransform) {
	ThrowIf(node->mNumMeshes > MeshMaxCount, "Mesh count too high.");

	resultTransform = node->mTransformation * resultTransform;

	auto originalStartingIndex = startingIndex;
	startingIndex += static_cast<int32_t>(node->mNumMeshes);
	ThrowIf(startingIndex < 0, "Mesh count overflow.");
	ThrowIf(startingIndex > static_cast<int32_t>(MeshMaxCount), "Mesh count too high.");
	if (startingIndex > targetIndex) {
		resultGlobalIndex = node->mMeshes[targetIndex - originalStartingIndex];
		return;
	}

	auto originalTransform = resultTransform;
	for (unsigned int i = 0U; i < node->mNumChildren; ++i) {
		walk_nodes_to_find_global_index_from_mesh_index(node->mChildren[i], startingIndex, targetIndex, resultGlobalIndex, resultTransform);
		if (resultGlobalIndex != NoAnswerFoundGlobalIndex) return;
		resultTransform = originalTransform;
	}

	resultGlobalIndex = NoAnswerFoundGlobalIndex;

}
aiMesh* get_mesh_at_index(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, aiMatrix4x4& transform) {
	unsigned int resultGlobalIndex = NoAnswerFoundGlobalIndex;
	int32_t startingIndex = 0;
	walk_nodes_to_find_global_index_from_mesh_index(assetHandle->mRootNode, startingIndex, meshIndex, resultGlobalIndex, transform);
	ThrowIf(resultGlobalIndex >= assetHandle->mNumMeshes, "Mesh index out of bounds.");
	return assetHandle->mMeshes[resultGlobalIndex];
}

void native_impl_asset_loader::get_loaded_asset_mesh_vertex_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outVertexCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outVertexCount, "Out vertex count pointer was null.");

	auto unused = aiMatrix4x4{};
	*outVertexCount = static_cast<int32_t>(get_mesh_at_index(assetHandle, meshIndex, unused)->mNumVertices);
}
StartExportedFunc(get_loaded_asset_mesh_vertex_count, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outVertexCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_vertex_count(assetHandle, meshIndex, outVertexCount);
	EndExportedFunc
}

int32_t get_mesh_triangle_count(aiMesh* mesh) {
	if (mesh->mPrimitiveTypes != aiPrimitiveType_TRIANGLE) return 0;
	
	auto result = static_cast<int32_t>(mesh->mNumFaces);
	if (result < 0) return 0;

	return result;
}
void native_impl_asset_loader::get_loaded_asset_mesh_triangle_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outTriangleCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outTriangleCount, "Out index count pointer was null.");

	auto unused = aiMatrix4x4{};
	*outTriangleCount = get_mesh_triangle_count(get_mesh_at_index(assetHandle, meshIndex, unused));
}
StartExportedFunc(get_loaded_asset_mesh_triangle_count, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outTriangleCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_triangle_count(assetHandle, meshIndex, outTriangleCount);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_vertices(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");

	auto transform = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, transform);

	ThrowIf(bufferSizeVertices < 0, "Invalid buffer size.");
	ThrowIf(static_cast<uint32_t>(bufferSizeVertices) < mesh->mNumVertices, "Given buffer was too small.")

	auto hasUVs = mesh->HasTextureCoords(0);
	auto hasTangents = mesh->HasTangentsAndBitangents() && mesh->HasNormals();

	for (auto vertexIndex = 0U; vertexIndex < mesh->mNumVertices; ++vertexIndex) {
		auto position = transform * mesh->mVertices[vertexIndex];
		auto uv = hasUVs ? mesh->mTextureCoords[0][vertexIndex] : aiVector3D{ 0.0f, 0.0f, 0.0f };
		auto tangent = float4{ };
		if (hasTangents) {
			auto t = mesh->mTangents[vertexIndex];
			auto b = mesh->mBitangents[vertexIndex];
			auto n = mesh->mNormals[vertexIndex];
			native_impl_render_assets::calculate_tangent_rotation(
				float3{ t.x, t.y, t.z },
				float3{ b.x, b.y, b.z },
				float3{ n.x, n.y, n.z },
				&tangent
			);
		}
		buffer[vertexIndex] = {
			.Position = { position.x, position.y, position.z },
			.TextureUV = { uv.x, uv.y },
			.Tangent = tangent
		};
	}
}
StartExportedFunc(copy_loaded_asset_mesh_vertices, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer) {
	native_impl_asset_loader::copy_loaded_asset_mesh_vertices(assetHandle, meshIndex, bufferSizeVertices, buffer);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_triangles(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeTriangles, int32_t* buffer) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(meshIndex) >= assetHandle->mNumMeshes, "Mesh index was out of bounds.");

	auto mesh = assetHandle->mMeshes[meshIndex];
	auto triangleCount = get_mesh_triangle_count(mesh);

	ThrowIf(bufferSizeTriangles < triangleCount, "Given buffer was too small.");

	for (auto faceIndex = 0; faceIndex < triangleCount; ++faceIndex) {
		auto face = mesh->mFaces[faceIndex];
		if (face.mNumIndices != 3) continue;
		buffer[(faceIndex * 3) + 0] = face.mIndices[0];
		buffer[(faceIndex * 3) + 1] = face.mIndices[1];
		buffer[(faceIndex * 3) + 2] = face.mIndices[2];
	}
}
StartExportedFunc(copy_loaded_asset_mesh_triangles, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeTriangles, int32_t* buffer) {
	native_impl_asset_loader::copy_loaded_asset_mesh_triangles(assetHandle, meshIndex, bufferSizeTriangles, buffer);
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

void native_impl_asset_loader::write_texels_to_disk(const char* filePath, int32_t width, int32_t height, int32_t bytesPerPixel, const void* data) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNotPositive(width, "Width was non-positive.");
	ThrowIfNotPositive(height, "Height was non-positive.");
	ThrowIf(bytesPerPixel != 3 && bytesPerPixel != 4, "BPP must be 3 or 4.");
	ThrowIfNull(data, "Data pointer was null.");

	auto writeResult = stbi_write_bmp(filePath, static_cast<int>(width), static_cast<int>(height), static_cast<int>(bytesPerPixel), data);
	ThrowIf(writeResult == 0, "Could not write file, data may be invalid or file location permissions may not be set correctly.");
}
StartExportedFunc(write_texels_to_disk, const char* filePath, int32_t width, int32_t height, int32_t bytesPerPixel, const void* data) {
	native_impl_asset_loader::write_texels_to_disk(filePath, width, height, bytesPerPixel, data);
	EndExportedFunc
}

void native_impl_asset_loader::load_skybox_file_in_to_memory(uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle) {
	ThrowIfNull(textureData, "Texture data pointer was null.");
	ThrowIf(textureDataLength < 0, "Texture data length was negative.");
	ThrowIfNull(outTextureHandle, "Out texture handle pointer was null.");

	// As far as I can tell here https://github.com/google/filament/blob/ee68872a473989706ba1382aa67a318fb088e72f/libs/ktxreader/src/Ktx1Reader.cpp#L115
	// this bundle will be deleted after the texture is loaded on to the GPU; so we don't need to delete it ourselves
	auto* ktx1Bundle = new image::Ktx1Bundle{ textureData, static_cast<uint32_t>(textureDataLength) };
	*outTextureHandle = ktxreader::Ktx1Reader::createTexture(filament_engine, ktx1Bundle, false);
	ThrowIfNull(*outTextureHandle, "Could not create skybox texture with given KTX bundle.");
}
StartExportedFunc(load_skybox_file_in_to_memory, uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle) {
	native_impl_asset_loader::load_skybox_file_in_to_memory(textureData, textureDataLength, outTextureHandle);
	EndExportedFunc
}
void native_impl_asset_loader::unload_skybox_file_from_memory(TextureHandle textureHandle) {
	ThrowIfNull(textureHandle, "Null texture handle.");
	filament_engine->destroy(textureHandle);
}
StartExportedFunc(unload_skybox_file_from_memory, TextureHandle textureHandle) {
	native_impl_asset_loader::unload_skybox_file_from_memory(textureHandle);
	EndExportedFunc
}
void native_impl_asset_loader::load_ibl_file_in_to_memory(uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle) {
	ThrowIfNull(textureData, "Texture data pointer was null.");
	ThrowIf(textureDataLength < 0, "Texture data length was negative.");
	ThrowIfNull(outTextureHandle, "Out texture handle pointer was null.");

	// As far as I can tell here https://github.com/google/filament/blob/ee68872a473989706ba1382aa67a318fb088e72f/libs/ktxreader/src/Ktx1Reader.cpp#L115
	// this bundle will be deleted after the texture is loaded on to the GPU; so we don't need to delete it ourselves
	auto* ktx1Bundle = new image::Ktx1Bundle{ textureData, static_cast<uint32_t>(textureDataLength) };
	*outTextureHandle = ktxreader::Ktx1Reader::createTexture(filament_engine, ktx1Bundle, false);
	ThrowIfNull(*outTextureHandle, "Could not create IBL texture with given KTX bundle.");
}
StartExportedFunc(load_ibl_file_in_to_memory, uint8_t* textureData, int32_t textureDataLength, TextureHandle* outTextureHandle) {
	native_impl_asset_loader::load_ibl_file_in_to_memory(textureData, textureDataLength, outTextureHandle);
	EndExportedFunc
}
void native_impl_asset_loader::unload_ibl_file_from_memory(TextureHandle textureHandle) {
	ThrowIfNull(textureHandle, "Null texture handle.");
	filament_engine->destroy(textureHandle);
}
StartExportedFunc(unload_ibl_file_from_memory, TextureHandle textureHandle) {
	native_impl_asset_loader::unload_ibl_file_from_memory(textureHandle);
	EndExportedFunc
}