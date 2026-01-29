#include "pch.h"
#include "assets/native_impl_asset_loader.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/ktxreader/Ktx1Reader.h"

#define STBI_FAILURE_USERMSG
#define STB_IMAGE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#define STB_IMAGE_WRITE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#include <filesystem>

#include "stb/stb_image.h"
#include "stb/stb_image_write.h"

constexpr unsigned int MeshMaxCount = 1000000;
constexpr unsigned int NoAnswerFoundGlobalIndex = MeshMaxCount + 1;

void native_impl_asset_loader::load_asset_file_in_to_memory(const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outAssetHandle, "Out asset handle pointer was null.");
	unsigned int flags = aiProcess_CalcTangentSpace | aiProcess_GenNormals | aiProcess_GenBoundingBoxes | aiProcess_GenUVCoords | aiProcess_Triangulate | aiProcess_SortByPType;
	if (fixCommonExporterErrors) flags |= aiProcess_FindDegenerates | aiProcess_FindInvalidData | aiProcess_FixInfacingNormals;
	if (optimize) flags |= aiProcess_ImproveCacheLocality | aiProcess_JoinIdenticalVertices | aiProcess_OptimizeGraph | aiProcess_OptimizeMeshes | aiProcess_FindInstances;
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

void native_impl_asset_loader::get_loaded_asset_mesh_material_index(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outMaterialIndex) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outMaterialIndex, "Out material index pointer was null.");
	
	auto unused = aiMatrix4x4{};
	*outMaterialIndex = get_mesh_at_index(assetHandle, meshIndex, unused)->mMaterialIndex;
}
StartExportedFunc(get_loaded_asset_mesh_material_index, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outMaterialIndex) {
	native_impl_asset_loader::get_loaded_asset_mesh_material_index(assetHandle, meshIndex, outMaterialIndex);
	EndExportedFunc
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

void native_impl_asset_loader::get_loaded_asset_texture_size(MemoryLoadedAssetHandle assetHandle, int32_t textureIndex, const char* assetRootDirPath, int32_t* outWidth, int32_t* outHeight) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(textureIndex) >= assetHandle->mNumTextures, "Texture index was out of bounds.");
	ThrowIfNull(outWidth, "Out width pointer was null.");
	ThrowIfNull(outHeight, "Out height pointer was null.");
	
	auto texture = assetHandle->mTextures[textureIndex];
	auto filename = texture->mFilename;
	
	// Embedded texture
	if (filename.length < 1U || filename.data[0] == '*') { 
		// Compressed format
		if (texture->mHeight == 0U) { 
			ThrowIf(texture->mWidth > 0x7FFFFFF, "Embedded texture '", texture->mFilename.C_Str(), "' is too large.");
			int width, height, channelCount;
			auto result = stbi_info_from_memory(reinterpret_cast<stbi_uc const*>(texture->pcData), static_cast<int>(texture->mWidth), &width, &height, &channelCount);
			ThrowIfNotPositive(result, "Could not load metadata for embedded texture '", texture->mFilename.C_Str(), "': ", stbi_failure_reason());
			*outWidth = static_cast<int32_t>(width);
			*outHeight = static_cast<int32_t>(height);
			return;
		}
		
		// Uncompressed format
		*outWidth = static_cast<int32_t>(texture->mWidth);
		*outHeight = static_cast<int32_t>(texture->mHeight);
		return;
	}
	
	// External file
	int32_t channelCount;
	std::filesystem::path root { assetRootDirPath };
	std::filesystem::path rel { filename.C_Str() };
	std::filesystem::path full = root / rel; // Don't be tempted to inline this and the line below, root / rel creates a temp that is moved to 'full' here. C++ fucking sucks lol
	auto fullPath = full.c_str();
	get_texture_file_data(fullPath, outWidth, outHeight, &channelCount);
}
StartExportedFunc(get_loaded_asset_texture_size, MemoryLoadedAssetHandle assetHandle, int32_t textureIndex, const char* assetRootDirPath, int32_t* outWidth, int32_t* outHeight) {
	native_impl_asset_loader::get_loaded_asset_texture_size(assetHandle, textureIndex, assetRootDirPath, outWidth, outHeight);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_texture_data(MemoryLoadedAssetHandle assetHandle, int32_t textureIndex, const char* assetRootDirPath, MemoryLoadedTextureRgba32DataPtr buffer, int32_t bufferLengthBytes, int32_t* outWidth, int32_t* outHeight) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(textureIndex) >= assetHandle->mNumTextures, "Texture index was out of bounds.");
	ThrowIfNull(assetRootDirPath, "Asset root file path was null.");
	ThrowIfNull(buffer, "Buffer pointer was null.");
	
	constexpr int MaxEmbeddedTextureDimension = 16384;
	auto texture = assetHandle->mTextures[textureIndex];
	auto filename = texture->mFilename;
	
	// Embedded texture
	if (filename.length < 1U || filename.data[0] == '*') { 
		// Compressed format, requires stbi
		if (texture->mHeight == 0U) { 
			ThrowIf(texture->mWidth > 0x7FFFFFF, "Embedded texture '", texture->mFilename.C_Str(), "' is too large.");
			int width, height, channelCount;
			auto imageData = stbi_load_from_memory(reinterpret_cast<stbi_uc const*>(texture->pcData), static_cast<int>(texture->mWidth), &width, &height, &channelCount, 4);
			ThrowIfNull(imageData, "Could not load embedded texture '", texture->mFilename.C_Str(), "': ", stbi_failure_reason());
			if (width > MaxEmbeddedTextureDimension || height > MaxEmbeddedTextureDimension || width <= 0 || height <= 0) {
				stbi_image_free(imageData);
				Throw("Embedded texture was either too large or had malformed size information.");
			}
			if (width * height * 4 > bufferLengthBytes) {
				stbi_image_free(imageData);
				Throw("Given buffer length was too small to fit embedded texture.");
			}
			memcpy((void*) buffer, imageData, width * height * 4); 
			stbi_image_free(imageData);
			*outWidth = width;
			*outHeight = height;
			return;
		}
		
		// Uncompressed format, need to manually copy over
		ThrowIf(texture->mWidth > MaxEmbeddedTextureDimension, "Embedded texture was either too large or had malformed size information.");
		ThrowIf(texture->mHeight > MaxEmbeddedTextureDimension, "Embedded texture was either too large or had malformed size information.");
		auto numTexels = texture->mWidth * texture->mHeight;
		if (numTexels * 4 > bufferLengthBytes) {
			Throw("Given buffer length was too small to fit embedded texture.");
		}
		for (auto i = 0; i < numTexels; ++i) {
			buffer[i * 4 + 0] = texture->pcData[i].r;
			buffer[i * 4 + 1] = texture->pcData[i].g;
			buffer[i * 4 + 2] = texture->pcData[i].b;
			buffer[i * 4 + 3] = texture->pcData[i].a;
		}
		*outWidth = static_cast<int32_t>(texture->mWidth);
		*outHeight = static_cast<int32_t>(texture->mHeight);
		return;
	}
	
	// External file
	std::filesystem::path root { assetRootDirPath };
	std::filesystem::path rel { filename.C_Str() };
	std::filesystem::path full = root / rel; // Don't be tempted to inline this and the line below, root / rel creates a temp that is moved to 'full' here. C++ fucking sucks lol
	auto fullPath = full.c_str();
	int32_t width, height;
	MemoryLoadedTextureRgba32DataPtr imageData;
	load_texture_file_in_to_memory(fullPath, interop_bool_true, &width, &height, &imageData);
	if (width > MaxEmbeddedTextureDimension || height > MaxEmbeddedTextureDimension || width <= 0 || height <= 0) {
		stbi_image_free(imageData);
		Throw("Embedded texture was either too large or had malformed size information.");
	}
	if (width * height * 4 > bufferLengthBytes) {
		stbi_image_free(imageData);
		Throw("Given buffer length was too small to fit embedded texture.");
	}
	memcpy((void*) buffer, imageData, width * height * 4); 
	stbi_image_free(imageData);
	*outWidth = width;
	*outHeight = height;
}
StartExportedFunc(get_loaded_asset_texture_data, MemoryLoadedAssetHandle assetHandle, int32_t textureIndex, const char* assetRootFilePath, MemoryLoadedTextureRgba32DataPtr buffer, int32_t bufferLengthBytes, int32_t* outWidth, int32_t* outHeight) {
	native_impl_asset_loader::get_loaded_asset_texture_data(assetHandle, textureIndex, assetRootFilePath, buffer, bufferLengthBytes, outWidth, outHeight);
	EndExportedFunc
}

int get_texture_index_from_path(MemoryLoadedAssetHandle assetHandle, aiString& path) {
	auto cstr = path.C_Str();
	auto cstrlen = strlen(cstr);
	if (cstrlen >= 2 && cstr[0] == '*') {
		auto idx = atoi(cstr + 1);
		if (idx < assetHandle->mNumTextures) return idx;
	}
	
	for (auto i = 0; i < assetHandle->mNumTextures; ++i) {
		if (assetHandle->mTextures[i]->mFilename == path) {
			return i;
		}
	}
	
	return -1;
}

void native_impl_asset_loader::get_loaded_asset_material_data(MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, AssetMaterialParam* outColorParam, AssetMaterialParam* outNormalsParam, AssetMaterialParam* outOrmParam) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(materialIndex) >= assetHandle->mNumMaterials, "Material index was out of bounds.");
	ThrowIfNull(outColorParam, "Out color param pointer was null.");
	ThrowIfNull(outNormalsParam, "Out normals param pointer was null.");
	ThrowIfNull(outOrmParam, "Out orm param pointer was null.");
	
	auto mat = assetHandle->mMaterials[materialIndex];
		
	outColorParam->Format = AssetMaterialParamDataFormat::NotIncluded; 
	outNormalsParam->Format = AssetMaterialParamDataFormat::NotIncluded; 
	outOrmParam->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	aiString path;
	aiColor4D rgba;
	
	// Color
	auto hasColorTex = 
		mat->GetTexture(aiTextureType_BASE_COLOR, 0, &path) == aiReturn_SUCCESS
		|| mat->GetTexture(aiTextureType_DIFFUSE, 0, &path) == aiReturn_SUCCESS;
	if (hasColorTex) {
		outColorParam->Format = AssetMaterialParamDataFormat::TextureMap;
		outColorParam->TextureMapIndex = get_texture_index_from_path(assetHandle, path);
		ThrowIfNegative(outColorParam->TextureMapIndex, "Could not find matching texture index for '", path.C_Str(), "'"); 		
	}
	else {
		auto hasColorData =
			mat->Get(AI_MATKEY_BASE_COLOR, rgba) == aiReturn_SUCCESS
			|| mat->Get(AI_MATKEY_COLOR_DIFFUSE, rgba) == aiReturn_SUCCESS;
		if (hasColorData) {
			outColorParam->Format = AssetMaterialParamDataFormat::Numerical;
			outColorParam->NumericalValueR = rgba.r;
			outColorParam->NumericalValueG = rgba.g;
			outColorParam->NumericalValueB = rgba.b;
			outColorParam->NumericalValueA = rgba.a;
		}
	}
	
	// Normals
	auto hasNormalTex = 
		mat->GetTexture(aiTextureType_NORMALS, 0, &path) == aiReturn_SUCCESS;
	if (hasNormalTex) {
		outNormalsParam->Format = AssetMaterialParamDataFormat::TextureMap;
		outNormalsParam->TextureMapIndex = get_texture_index_from_path(assetHandle, path);
		ThrowIfNegative(outNormalsParam->TextureMapIndex, "Could not find matching texture index for '", path.C_Str(), "'");
	}
	else {
		auto hasNormalsData =
			mat->Get(AI_MATKEY_MAPPING_NORMALS(0), rgba) == aiReturn_SUCCESS;
		if (hasNormalsData) {
			outNormalsParam->Format = AssetMaterialParamDataFormat::Numerical;
			outNormalsParam->NumericalValueR = rgba.r;
			outNormalsParam->NumericalValueG = rgba.g;
			outNormalsParam->NumericalValueB = rgba.b;
			outNormalsParam->NumericalValueA = rgba.a;
		}
	}
	
	// ORM
	// TODO
}
StartExportedFunc(get_loaded_asset_material_data, MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, native_impl_asset_loader::AssetMaterialParam* outColorParam, native_impl_asset_loader::AssetMaterialParam* outNormalsParam, native_impl_asset_loader::AssetMaterialParam* outOrmParam) {
	native_impl_asset_loader::get_loaded_asset_material_data(assetHandle, materialIndex, outColorParam, outNormalsParam, outOrmParam);
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


void native_impl_asset_loader::get_texture_file_data(const char* filePath, int32_t* outWidth, int32_t* outHeight, int32_t* outChannelCount) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outWidth, "Out width pointer was null.");
	ThrowIfNull(outHeight, "Out height pointer was null.");
	ThrowIfNull(outChannelCount, "Out channel count pointer was null.");
	int width, height, channelCount;
	auto result = stbi_info(filePath, &width, &height, &channelCount);
	ThrowIfNotPositive(result, "Could not load metadata for texture '", filePath, "': ", stbi_failure_reason());
	*outWidth = static_cast<int32_t>(width);
	*outHeight = static_cast<int32_t>(height);
	*outChannelCount = static_cast<int32_t>(channelCount);
}
StartExportedFunc(get_texture_file_data, const char* filePath, int32_t* outWidth, int32_t* outHeight, int32_t* outChannelCount) {
	native_impl_asset_loader::get_texture_file_data(filePath, outWidth, outHeight, outChannelCount);
	EndExportedFunc
}
void native_impl_asset_loader::load_texture_file_in_to_memory(const char* filePath, interop_bool includeWAlphaChannel, int32_t* outWidth, int32_t* outHeight, MemoryLoadedTextureRgba32DataPtr* outTextureData) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outWidth, "Out width pointer was null.");
	ThrowIfNull(outHeight, "Out height pointer was null.");
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