#include "pch.h"
#include "assets/native_impl_asset_loader.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#define STBI_FAILURE_USERMSG
#include "stb/stb_imageh.h"

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

void native_impl_asset_loader::get_loaded_asset_mesh_vertex_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outVertexCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outVertexCount, "Out vertex count pointer was null.");
	ThrowIf(meshIndex >= assetHandle->mNumMeshes, "Mesh index was out of bounds.");

	*outVertexCount = static_cast<int32_t>(assetHandle->mMeshes[meshIndex]->mNumVertices);
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
	ThrowIf(meshIndex >= assetHandle->mNumMeshes, "Mesh index was out of bounds.");

	*outTriangleCount = get_mesh_triangle_count(assetHandle->mMeshes[meshIndex]);
}
StartExportedFunc(get_loaded_asset_mesh_triangle_count, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outTriangleCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_triangle_count(assetHandle, meshIndex, outTriangleCount);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_vertices(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(meshIndex >= assetHandle->mNumMeshes, "Mesh index was out of bounds.");

	auto mesh = assetHandle->mMeshes[meshIndex];

	ThrowIf(bufferSizeVertices < mesh->mNumVertices, "Given buffer was too small.")

	auto hasUVs = mesh->HasTextureCoords(0);
	auto hasTangents = mesh->HasTangentsAndBitangents() && mesh->HasNormals();

	for (auto vertexIndex = 0; vertexIndex < mesh->mNumVertices; ++vertexIndex) {
		auto position = mesh->mVertices[vertexIndex];
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
	ThrowIf(meshIndex >= assetHandle->mNumMeshes, "Mesh index was out of bounds.");

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