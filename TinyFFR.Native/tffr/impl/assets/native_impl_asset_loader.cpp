#include "pch.h"
#include "assets/native_impl_asset_loader.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/ktxreader/Ktx1Reader.h"

#define STBI_FAILURE_USERMSG
#define STB_IMAGE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#define STB_IMAGE_WRITE_IMPLEMENTATION // This should only be defined in one file ever, it imports the entire implementation for stb_image in as a definition file
#include <filesystem>

#include <unordered_map>
#include <string>
#include "assimp/GltfMaterial.h"
#include "stb/stb_image.h"
#include "stb/stb_image_write.h"

constexpr unsigned int MeshMaxCount = 1000000;
constexpr unsigned int NoAnswerFoundGlobalIndex = MeshMaxCount + 1;

#pragma region Asset Loading
void native_impl_asset_loader::load_asset_file_in_to_memory(const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	ThrowIfNull(filePath, "File path pointer was null.");
	ThrowIfNull(outAssetHandle, "Out asset handle pointer was null.");
	unsigned int flags = aiProcess_CalcTangentSpace 
					   | aiProcess_GenNormals
					   | aiProcess_GenBoundingBoxes
					   | aiProcess_GenUVCoords
					   | aiProcess_Triangulate
					   | aiProcess_JoinIdenticalVertices // Note: Although this is technically an optimization it's actually required to make skeletal vertices correct after triangulation, so it's not optional. Also it's required for indexed geometry.
					   | aiProcess_SortByPType
					   | aiProcess_LimitBoneWeights
					   | aiProcess_PopulateArmatureData;
	if (fixCommonExporterErrors) {
		flags |= aiProcess_FindDegenerates
			   | aiProcess_FindInvalidData
			   | aiProcess_FixInfacingNormals;	
	}
	if (optimize) {
		flags |= aiProcess_ImproveCacheLocality
			   | aiProcess_OptimizeGraph
			   | aiProcess_OptimizeMeshes
			   | aiProcess_FindInstances;
	}
	*outAssetHandle = aiImportFile(filePath, flags);
	ThrowIfNull(*outAssetHandle, "Could not load asset '", filePath, "': ", aiGetErrorString());
}
StartExportedFunc(load_asset_file_in_to_memory, const char* filePath, interop_bool fixCommonExporterErrors, interop_bool optimize, MemoryLoadedAssetHandle* outAssetHandle) {
	native_impl_asset_loader::load_asset_file_in_to_memory(filePath, fixCommonExporterErrors, optimize, outAssetHandle);
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
#pragma endregion

#pragma region Mesh Queries
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
	*outMaterialIndex = static_cast<int32_t>(get_mesh_at_index(assetHandle, meshIndex, unused)->mMaterialIndex);
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

void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_bone_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outBoneCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outBoneCount, "Out bone count pointer was null.");

	auto unused = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, unused);
	*outBoneCount = static_cast<int32_t>(mesh->mNumBones);
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_bone_count, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outBoneCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_bone_count(assetHandle, meshIndex, outBoneCount);
	EndExportedFunc
}
#pragma endregion

#pragma region Mesh Geometry
typedef void(*standard_vertex_data_callback)(unsigned vertexIndex, float3& position, float2& textureUv, float4& tangent, void* bufferPtr);

void get_mesh_standard_vertex_attributes(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, bool prebakeTransforms, int32_t bufferSizeVertices, void* bufferPtr, standard_vertex_data_callback callback, aiMesh** outMeshPtr) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");

	auto transform = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, transform);
	if (!prebakeTransforms) transform = aiMatrix4x4 { };
	auto normalMatrix = aiMatrix3x3 { transform };
	normalMatrix = normalMatrix.Inverse().Transpose();

	ThrowIf(bufferSizeVertices < 0, "Invalid buffer size.");
	ThrowIf(static_cast<uint32_t>(bufferSizeVertices) < mesh->mNumVertices, "Given buffer was too small.")

	auto hasUVs = mesh->HasTextureCoords(0);
	auto hasNormals = mesh->HasNormals();
	auto hasTangents = hasNormals && mesh->HasTangentsAndBitangents();

	for (auto vertexIndex = 0U; vertexIndex < mesh->mNumVertices; ++vertexIndex) {
		auto position = transform * mesh->mVertices[vertexIndex];
		auto uv = hasUVs ? mesh->mTextureCoords[0][vertexIndex] : aiVector3D{ 0.0f, 0.0f, 0.0f };
		auto tangent = float4{ };
		if (hasTangents) {
			auto t = (normalMatrix * mesh->mTangents[vertexIndex]).Normalize();
			auto b = (normalMatrix * mesh->mBitangents[vertexIndex]).Normalize();
			auto n = (normalMatrix * mesh->mNormals[vertexIndex]).Normalize();
			native_impl_render_assets::calculate_tangent_rotation(
				float3{ t.x, t.y, t.z },
				float3{ b.x, b.y, b.z },
				float3{ n.x, n.y, n.z },
				&tangent
			);
		}
		else if (hasNormals) {
			auto n = (normalMatrix * mesh->mNormals[vertexIndex]).Normalize();
			auto orthogonalizationTargetVect = std::abs(n.y) < 0.999f ? aiVector3D{0, 1, 0} : aiVector3D{1, 0, 0};
			auto t = (orthogonalizationTargetVect ^ n).Normalize();
			auto b = (n ^ t).Normalize();
			native_impl_render_assets::calculate_tangent_rotation(
				float3{ t.x, t.y, t.z },
				float3{ b.x, b.y, b.z },
				float3{ n.x, n.y, n.z },
				&tangent
			);
		}
		else tangent = float4{ 0.0f, 0.0f, 0.0f, 1.0f };

		auto f3Position = float3 { position.x, position.y, position.z };
		auto f2textureUv = float2 { uv.x, uv.y };
		callback(
			vertexIndex,
			f3Position,
			f2textureUv,
			tangent,
			bufferPtr
		);
	}
	
	*outMeshPtr = mesh;
}

void native_impl_asset_loader::copy_loaded_asset_mesh_vertices(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer) {
	auto callback = [](unsigned vertexIndex, float3& position, float2& textureUv, float4& tangent, void* userData) {
		static_cast<native_impl_render_assets::MeshVertex*>(userData)[vertexIndex] = {
			.Position = { position.x, position.y, position.z },
			.TextureUV = { textureUv.x, textureUv.y },
			.Tangent = tangent
		};
	};
	
	aiMesh* unused;
	get_mesh_standard_vertex_attributes(assetHandle, meshIndex, true, bufferSizeVertices, buffer, callback, &unused);
}
StartExportedFunc(copy_loaded_asset_mesh_vertices, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertex* buffer) {
	native_impl_asset_loader::copy_loaded_asset_mesh_vertices(assetHandle, meshIndex, bufferSizeVertices, buffer);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_vertices(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertexSkeletal* buffer) {
	auto callback = [](unsigned vertexIndex, float3& position, float2& textureUv, float4& tangent, void* userData) {
		static_cast<native_impl_render_assets::MeshVertexSkeletal*>(userData)[vertexIndex] = {
			.Position = { position.x, position.y, position.z },
			.TextureUV = { textureUv.x, textureUv.y },
			.Tangent = tangent
		};
	};
	
	aiMesh* mesh;
	get_mesh_standard_vertex_attributes(assetHandle, meshIndex, false, bufferSizeVertices, buffer, callback, &mesh);

	for (auto boneIndex = 0U; boneIndex < min(mesh->mNumBones, 255U); ++boneIndex) {
		auto bone = mesh->mBones[boneIndex];
		for (auto weightIndex = 0U; weightIndex < bone->mNumWeights; ++weightIndex) {
			auto weight = bone->mWeights[weightIndex];
			
			ThrowIf(weight.mVertexId >= bufferSizeVertices, "Bone weight vertex ID out of bounds.");
			auto& vertexRef = buffer[weight.mVertexId];
			
			for (auto i = 0; i < MaxSkeletalVertexBoneCount; ++i) {
				if (vertexRef.BoneWeights[i] == 0.0f) {
					vertexRef.BoneIndices[i] = static_cast<uint8_t>(boneIndex);
					vertexRef.BoneWeights[i] = weight.mWeight;
					break;
				}
			}
		}
	}
}
StartExportedFunc(copy_loaded_asset_mesh_skeletal_vertices, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t bufferSizeVertices, native_impl_render_assets::MeshVertexSkeletal* buffer) {
	native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_vertices(assetHandle, meshIndex, bufferSizeVertices, buffer);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_triangles(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, interop_bool correctFlippedOrientation, int32_t bufferSizeTriangles, int32_t* buffer) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");

	auto transform = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, transform);
	auto transformDetIsNeg = transform.Determinant() < 0.0f;
	auto triangleCount = get_mesh_triangle_count(mesh);

	ThrowIf(bufferSizeTriangles < triangleCount, "Given buffer was too small.");

	if (correctFlippedOrientation && transformDetIsNeg) {
		for (auto faceIndex = 0; faceIndex < triangleCount; ++faceIndex) {
			auto face = mesh->mFaces[faceIndex];
			if (face.mNumIndices != 3) continue;
			buffer[(faceIndex * 3) + 0] = static_cast<int32_t>(face.mIndices[0]);
			buffer[(faceIndex * 3) + 1] = static_cast<int32_t>(face.mIndices[2]);
			buffer[(faceIndex * 3) + 2] = static_cast<int32_t>(face.mIndices[1]);
		}
	}
	else {
		for (auto faceIndex = 0; faceIndex < triangleCount; ++faceIndex) {
			auto face = mesh->mFaces[faceIndex];
			if (face.mNumIndices != 3) continue;
			buffer[(faceIndex * 3) + 0] = static_cast<int32_t>(face.mIndices[0]);
			buffer[(faceIndex * 3) + 1] = static_cast<int32_t>(face.mIndices[1]);
			buffer[(faceIndex * 3) + 2] = static_cast<int32_t>(face.mIndices[2]);
		}
	}
}
StartExportedFunc(copy_loaded_asset_mesh_triangles, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, interop_bool correctFlippedOrientation, int32_t bufferSizeTriangles, int32_t* buffer) {
	native_impl_asset_loader::copy_loaded_asset_mesh_triangles(assetHandle, meshIndex, correctFlippedOrientation, bufferSizeTriangles, buffer);
	EndExportedFunc
}
#pragma endregion

#pragma region Materials and Textures
void native_impl_asset_loader::get_loaded_asset_texture_size(MemoryLoadedAssetHandle assetHandle, int32_t textureIndex, const char* assetRootDirPath, int32_t* outWidth, int32_t* outHeight) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(textureIndex) >= assetHandle->mNumTextures, "Texture index was out of bounds.");
	ThrowIfNull(outWidth, "Out width pointer was null.");
	ThrowIfNull(outHeight, "Out height pointer was null.");
	
	auto texture = assetHandle->mTextures[textureIndex];
	
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
	
	// Compressed format, requires stbi
	if (texture->mHeight == 0U) { 
		ThrowIf(texture->mWidth > 0x7FFFFFF, "Embedded texture '", texture->mFilename.C_Str(), "' is too large.");
		int width, height, channelCount;
		stbi_set_flip_vertically_on_load(true);
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
		
	return -1;
}

bool apply_material_texture_if_present(MemoryLoadedAssetHandle assetHandle, aiMaterial* matPtr, native_impl_asset_loader::AssetMaterialParam* paramPtr, aiTextureType texType) {
	aiString path;
	auto result = matPtr->GetTexture(texType, 0, &path) == aiReturn_SUCCESS;
	if (!result) return result;
	paramPtr->Format = native_impl_asset_loader::AssetMaterialParamDataFormat::TextureMap;
	paramPtr->TextureMapIndex = get_texture_index_from_path(assetHandle, path);
	if (paramPtr->TextureMapIndex < 0) {
		ThrowIfNotPositive(texType, "Can't encode external texture index as negative value because given texture type value is already non-positive.");
		paramPtr->TextureMapIndex = texType * -1;
	}
	return result;
}

bool apply_material_numerical_if_present(MemoryLoadedAssetHandle assetHandle, aiMaterial* matPtr, native_impl_asset_loader::AssetMaterialParam* paramPtr, const char* pKey, unsigned int type, unsigned int idx, bool clamp, bool supportsMultiChannel) {
	aiColor4D rgba;
	aiColor3D rgb;
	ai_real real;
	
	if (supportsMultiChannel) {
		auto result = matPtr->Get(pKey, type, idx, rgba) == aiReturn_SUCCESS;
		if (result) {
			paramPtr->Format = native_impl_asset_loader::AssetMaterialParamDataFormat::Numerical;
			paramPtr->NumericalValueR = rgba.r;
			paramPtr->NumericalValueG = rgba.g;
			paramPtr->NumericalValueB = rgba.b;
			paramPtr->NumericalValueA = rgba.a;
			if (clamp) {
				paramPtr->NumericalValueR = std::clamp(paramPtr->NumericalValueR, 0.0f, 1.0f);
				paramPtr->NumericalValueG = std::clamp(paramPtr->NumericalValueG, 0.0f, 1.0f);
				paramPtr->NumericalValueB = std::clamp(paramPtr->NumericalValueB, 0.0f, 1.0f);
				paramPtr->NumericalValueA = std::clamp(paramPtr->NumericalValueA, 0.0f, 1.0f);
			}
			return result;
		}
	
		result = matPtr->Get(pKey, type, idx, rgb) == aiReturn_SUCCESS;
		if (result) {
			paramPtr->Format = native_impl_asset_loader::AssetMaterialParamDataFormat::Numerical;
			paramPtr->NumericalValueR = rgba.r;
			paramPtr->NumericalValueG = rgba.g;
			paramPtr->NumericalValueB = rgba.b;
			paramPtr->NumericalValueA = 1.0f;
			if (clamp) {
				paramPtr->NumericalValueR = std::clamp(paramPtr->NumericalValueR, 0.0f, 1.0f);
				paramPtr->NumericalValueG = std::clamp(paramPtr->NumericalValueG, 0.0f, 1.0f);
				paramPtr->NumericalValueB = std::clamp(paramPtr->NumericalValueB, 0.0f, 1.0f);
			}
			return result;
		}
	}
	
	auto result = matPtr->Get(pKey, type, idx, real) == aiReturn_SUCCESS;
	if (result) {
		if (clamp) real = std::clamp(real, 0.0f, 1.0f);
		paramPtr->Format = native_impl_asset_loader::AssetMaterialParamDataFormat::Numerical;
		paramPtr->NumericalValueR = real;
		paramPtr->NumericalValueG = real;
		paramPtr->NumericalValueB = real;
		paramPtr->NumericalValueA = real;
		return result;
	}
	
	return false;
}

void moderate_texture_data_with_factor_var(aiMaterial* matPtr, native_impl_asset_loader::AssetMaterialParam* paramPtr, const char* pKey, unsigned int type, unsigned int idx) {
	if (paramPtr->Format != native_impl_asset_loader::TextureMap) return;
	
	ai_real real;
	auto result = matPtr->Get(pKey, type, idx, real) == aiReturn_SUCCESS;
	if (!result) return;
	if (real == 1.0f) return;
	
	paramPtr->NumericalValueR = -1.0f;
	paramPtr->NumericalValueG = -1.0f;
	paramPtr->NumericalValueB = -1.0f;
	paramPtr->NumericalValueA = real;
}

void native_impl_asset_loader::get_loaded_asset_texture_path_len(MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, int32_t textureIndex, const char* assetRootDirPath, int32_t* outPathLength) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(materialIndex) >= assetHandle->mNumMaterials, "Material index was out of bounds.");
	ThrowIfNull(outPathLength, "Out path length pointer was null.");
	
	aiString path;
	auto result = assetHandle->mMaterials[materialIndex]->GetTexture(static_cast<aiTextureType>(-1 * textureIndex), 0, &path);
	if (result != aiReturn_SUCCESS) {
		*outPathLength = 0;
		return;
	}
	auto cStr = path.C_Str();
	std::filesystem::path root { assetRootDirPath };
	std::filesystem::path rel { cStr };
	std::filesystem::path full = root / rel; // Don't be tempted to inline this and the line below, root / rel creates a temp that is moved to 'full' here. C++ fucking sucks lol
	auto fullPath = full.c_str();
	*outPathLength = static_cast<int32_t>(strlen(fullPath));
}
StartExportedFunc(get_loaded_asset_texture_path_len, MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, int32_t textureIndex, const char* assetRootDirPath, int32_t* outPathLength) {
	native_impl_asset_loader::get_loaded_asset_texture_path_len(assetHandle, materialIndex, textureIndex, assetRootDirPath, outPathLength);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_texture_path(MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, int32_t textureIndex, const char* assetRootDirPath, char* strBuffer, int32_t bufferLengthBytes) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(materialIndex) >= assetHandle->mNumMaterials, "Material index was out of bounds.");
	ThrowIfNull(strBuffer, "String buffer pointer was null.");
	
	aiString path;
	auto result = assetHandle->mMaterials[materialIndex]->GetTexture(static_cast<aiTextureType>(-1 * textureIndex), 0, &path);
	ThrowIf(result != aiReturn_SUCCESS, "Could not load texture path!");
	auto cStr = path.C_Str();
	std::filesystem::path root { assetRootDirPath };
	std::filesystem::path rel { cStr };
	std::filesystem::path full = root / rel; // Don't be tempted to inline this and the line below, root / rel creates a temp that is moved to 'full' here. C++ fucking sucks lol
	auto fullPath = full.c_str();
	auto len = strlen(fullPath);
	ThrowIf(bufferLengthBytes <= len, "Given string buffer too small for texture path string.");
	strcpy(strBuffer, fullPath);
}
StartExportedFunc(get_loaded_asset_texture_path, MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, int32_t textureIndex, const char* assetRootDirPath, char* strBuffer, int32_t bufferLengthBytes) {
	native_impl_asset_loader::get_loaded_asset_texture_path(assetHandle, materialIndex, textureIndex, assetRootDirPath, strBuffer, bufferLengthBytes);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_material_data(MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, AssetMaterialParamGroup* paramGroupPtr, AssetMaterialAlphaFormat* outAlphaFormat, float_t* outRefractionThickness) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(static_cast<uint32_t>(materialIndex) >= assetHandle->mNumMaterials, "Material index was out of bounds.");
	ThrowIfNull(paramGroupPtr, "Param group pointer was null.");
	ThrowIfNull(outAlphaFormat, "Out alpha format pointer was null.");
	
	auto mat = assetHandle->mMaterials[materialIndex];
	bool valueFound = false;
	AssetMaterialParam* paramPtr = nullptr;
	
	// Color
	paramPtr = paramGroupPtr->ColorParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_BASE_COLOR)
		|| apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_DIFFUSE)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_BASE_COLOR, true, true)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_COLOR_DIFFUSE, true, true);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// Normals
	paramPtr = paramGroupPtr->NormalParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_NORMALS);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// ORM - O
	paramPtr = paramGroupPtr->AmbientOcclusionParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_AMBIENT_OCCLUSION);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// ORM - R
	paramPtr = paramGroupPtr->RoughnessParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_DIFFUSE_ROUGHNESS)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_ROUGHNESS_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else moderate_texture_data_with_factor_var(mat, paramPtr, AI_MATKEY_ROUGHNESS_FACTOR);
	
	// ORM - ^R (i.e. Glossiness, Shininess)
	paramPtr = paramGroupPtr->GlossinessParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_SHININESS);
		// Assimp always provides this value as part of its legacy model so we just check for a texture now 
		// || apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_SHININESS_STRENGTH, true)
		// || apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_SHININESS, true);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// ORM - M
	paramPtr = paramGroupPtr->MetallicParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_METALNESS)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_METALLIC_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else moderate_texture_data_with_factor_var(mat, paramPtr, AI_MATKEY_METALLIC_FACTOR);
	
	// ORM(R) - IoR
	paramPtr = paramGroupPtr->IoRParamsPtr;
	valueFound = 
		apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_REFRACTI, false, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// AT - A
	paramPtr = paramGroupPtr->AbsorptionParamsPtr;
	valueFound =
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_TRANSMISSION)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_VOLUME_ATTENUATION_COLOR, true, true);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// AT - T
	paramPtr = paramGroupPtr->TransmissionParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_TRANSMISSION)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_TRANSMISSION_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else moderate_texture_data_with_factor_var(mat, paramPtr, AI_MATKEY_TRANSMISSION_FACTOR);
	
	// Emissive - Color
	paramPtr = paramGroupPtr->EmissiveColorParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_EMISSIVE)
		|| apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_EMISSION_COLOR)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_COLOR_EMISSIVE, true, true);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else if (paramPtr->Format == Numerical) {
		if (paramPtr->NumericalValueR == 0.0f && paramPtr->NumericalValueG == 0.0f && paramPtr->NumericalValueB == 0.0f) {
			paramPtr->Format = NotIncluded;
		}
	}
	
	// Emissive - Intensity
	paramPtr = paramGroupPtr->EmissiveIntensityParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_EMISSIVE)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_EMISSIVE_INTENSITY, false, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	// Anisotropy - Angle
	paramPtr = paramGroupPtr->AnisotropyAngleParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_ANISOTROPY)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_ANISOTROPY_ROTATION, false, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else if (paramPtr->Format == Numerical) {
		auto input = paramPtr->NumericalValueR;
		if (input < 0.0f) input *= -1.0f;
		input /= (F_PI * 2.0f);
		paramPtr->NumericalValueR = input;
		paramPtr->NumericalValueG = input;
		paramPtr->NumericalValueB = input;
		paramPtr->NumericalValueA = input;
	}
	
	// Anisotropy - Strength
	paramPtr = paramGroupPtr->AnisotropyStrengthParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_ANISOTROPY)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_ANISOTROPY_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else moderate_texture_data_with_factor_var(mat, paramPtr, AI_MATKEY_ANISOTROPY_FACTOR);
	
	// Clear coat - Strength
	paramPtr = paramGroupPtr->ClearCoatStrengthParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_CLEARCOAT)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_CLEARCOAT_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	else moderate_texture_data_with_factor_var(mat, paramPtr, AI_MATKEY_CLEARCOAT_FACTOR);
	
	// Clear coat - Roughness
	paramPtr = paramGroupPtr->ClearCoatRoughnessParamsPtr;
	valueFound = 
		apply_material_texture_if_present(assetHandle, mat, paramPtr, aiTextureType_CLEARCOAT)
		|| apply_material_numerical_if_present(assetHandle, mat, paramPtr, AI_MATKEY_CLEARCOAT_ROUGHNESS_FACTOR, true, false);
	if (!valueFound) paramPtr->Format = AssetMaterialParamDataFormat::NotIncluded;
	
	*outAlphaFormat = AssetMaterialAlphaFormat::None;
	aiString alphaMode;
	if (mat->Get(AI_MATKEY_GLTF_ALPHAMODE, alphaMode) == aiReturn_SUCCESS) {
		if (alphaMode == aiString("MASK")) *outAlphaFormat = AssetMaterialAlphaFormat::Masked;
		else if (alphaMode == aiString("BLEND")) *outAlphaFormat = AssetMaterialAlphaFormat::Blended;
	}
	
	*outRefractionThickness = -1.0f;
	ai_real aiRefThick;
	if (mat->Get(AI_MATKEY_VOLUME_THICKNESS_FACTOR, aiRefThick) == aiReturn_SUCCESS) {
		*outRefractionThickness = aiRefThick;
	}
}
StartExportedFunc(get_loaded_asset_material_data, MemoryLoadedAssetHandle assetHandle, int32_t materialIndex, native_impl_asset_loader::AssetMaterialParamGroup* paramGroupPtr, native_impl_asset_loader::AssetMaterialAlphaFormat* outAlphaFormat, float_t* outRefractionThickness) {
	native_impl_asset_loader::get_loaded_asset_material_data(assetHandle, materialIndex, paramGroupPtr, outAlphaFormat, outRefractionThickness);
	EndExportedFunc
}
#pragma endregion

#pragma region Skeletal Data
void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_count(MemoryLoadedAssetHandle assetHandle, int32_t* outAnimationCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outAnimationCount, "Out animation count pointer was null.");
	
	*outAnimationCount = static_cast<int32_t>(assetHandle->mNumAnimations);
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_animation_count, MemoryLoadedAssetHandle assetHandle, int32_t* outAnimationCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_count(assetHandle, outAnimationCount);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_metadata(MemoryLoadedAssetHandle assetHandle, int32_t animIndex, float_t ticksPerSecOverride, int32_t* outNameLengthBytes, float_t* outDurationSeconds, int32_t* outChannelCount) {
	ThrowIfNull(assetHandle, "Anim handle was null.");
	ThrowIf(animIndex < 0 || animIndex >= assetHandle->mNumAnimations, "Anim index out of bounds.");
	ThrowIfNull(outNameLengthBytes, "Out name length pointer was null.");
	ThrowIfNull(outDurationSeconds, "Out duration pointer was null.");
	ThrowIfNull(outChannelCount, "Out channel count pointer was null.");
	
	auto anim = assetHandle->mAnimations[animIndex];
	auto tps = static_cast<double>(ticksPerSecOverride);
	if (tps == 0.0) tps = (anim->mTicksPerSecond > 0.0 ? anim->mTicksPerSecond : 25.0);

	*outNameLengthBytes = static_cast<int32_t>(anim->mName.length);
	*outDurationSeconds = static_cast<float_t>(anim->mDuration / tps);
	*outChannelCount = static_cast<int32_t>(anim->mNumChannels);
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_animation_metadata, MemoryLoadedAssetHandle assetHandle, int32_t animIndex, float_t ticksPerSecOverride, int32_t* outNameLengthBytes, float_t* outDurationSeconds, int32_t* outChannelCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_metadata(assetHandle, animIndex, ticksPerSecOverride, outNameLengthBytes, outDurationSeconds, outChannelCount);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_animation_name(MemoryLoadedAssetHandle assetHandle, int32_t animIndex, char* nameBuffer, int32_t bufferLengthBytes) {
	ThrowIfNull(assetHandle, "Anim handle was null.");
	ThrowIf(animIndex < 0 || animIndex >= assetHandle->mNumAnimations, "Anim index out of bounds.");
	ThrowIfNull(nameBuffer, "Name buffer pointer was null.");
	auto anim = assetHandle->mAnimations[animIndex];
	ThrowIf(bufferLengthBytes < 0 || anim->mName.length > static_cast<uint32_t>(bufferLengthBytes), "Invalid name buffer length.");
	
	strcpy(nameBuffer, anim->mName.C_Str());
}
StartExportedFunc(copy_loaded_asset_mesh_skeletal_animation_name, MemoryLoadedAssetHandle assetHandle, int32_t animIndex, char* nameBuffer, int32_t bufferLengthBytes) {
	native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_animation_name(assetHandle, animIndex, nameBuffer, bufferLengthBytes);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_node_name(NodeHandle nodeHandle, char* nameBuffer, int32_t bufferLengthBytes) {
	ThrowIfNull(nameBuffer, "Name buffer pointer was null.");
	ThrowIf(bufferLengthBytes < 0 || nodeHandle.Node->mName.length > static_cast<uint32_t>(bufferLengthBytes), "Invalid name buffer length.");
	
	strcpy(nameBuffer, nodeHandle.Node->mName.C_Str());
}
StartExportedFunc(copy_loaded_asset_mesh_skeletal_node_name, native_impl_asset_loader::NodeHandle nodeHandle, char* nameBuffer, int32_t bufferLengthBytes) {
	native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_node_name(nodeHandle, nameBuffer, bufferLengthBytes);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_channel_metadata(MemoryLoadedAssetHandle assetHandle, int32_t animIndex, int32_t channelIndex, NodeHandle* nodeHandleBuffer, int32_t handleBufferCount, int32_t* outNodeIndex, int32_t* outScalingKeyframeCount, int32_t* outRotationKeyframeCount, int32_t* outTranslationKeyframeCount) {
	ThrowIfNull(assetHandle, "Anim handle was null.");
	ThrowIf(animIndex < 0 || animIndex >= assetHandle->mNumAnimations, "Anim index out of bounds.");
	auto anim = assetHandle->mAnimations[animIndex];
	ThrowIf(channelIndex < 0 || channelIndex >= anim->mNumChannels, "Channel index out of bounds.");
	ThrowIfNull(nodeHandleBuffer, "Node handle buffer pointer was null.");
	ThrowIf(handleBufferCount < 0, "Invalid handle buffer count.");
	ThrowIfNull(outNodeIndex, "Out node index pointer was null.");
	ThrowIfNull(outScalingKeyframeCount, "Out scaling keyframe count pointer was null.");
	ThrowIfNull(outRotationKeyframeCount, "Out rotation keyframe count pointer was null.");
	ThrowIfNull(outTranslationKeyframeCount, "Out translation keyframe count pointer was null.");

	auto channel = anim->mChannels[channelIndex];
	auto node = assetHandle->mRootNode->FindNode(channel->mNodeName);
	*outNodeIndex = -1;
	for (int32_t i = 0; i < handleBufferCount; ++i) {
		if (nodeHandleBuffer[i].Node == node) {
			*outNodeIndex = i;
			break;
		}
	}

	*outScalingKeyframeCount = static_cast<int32_t>(channel->mNumScalingKeys);
	*outRotationKeyframeCount = static_cast<int32_t>(channel->mNumRotationKeys);
	*outTranslationKeyframeCount = static_cast<int32_t>(channel->mNumPositionKeys);
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_animation_channel_metadata, MemoryLoadedAssetHandle assetHandle, int32_t animIndex, int32_t channelIndex, native_impl_asset_loader::NodeHandle* nodeHandleBuffer, int32_t handleBufferCount, int32_t* outNodeIndex, int32_t* outScalingKeyframeCount, int32_t* outRotationKeyframeCount, int32_t* outTranslationKeyframeCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_animation_channel_metadata(assetHandle, animIndex, channelIndex, nodeHandleBuffer, handleBufferCount, outNodeIndex, outScalingKeyframeCount, outRotationKeyframeCount, outTranslationKeyframeCount);
	EndExportedFunc
}

void native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_animation_channel_data(MemoryLoadedAssetHandle assetHandle, int32_t animIndex, int32_t channelIndex, float_t ticksPerSecOverride, float3* scalingVectorBuffer, float_t* scalingTimeBuffer, int32_t scalingBufferCount, quatf* rotationQuaternionBuffer, float_t* rotationTimeBuffer, int32_t rotationBufferCount, float3* translationVectorBuffer, float_t* translationTimeBuffer, int32_t translationBufferCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIf(animIndex < 0 || animIndex >= assetHandle->mNumAnimations, "Anim index out of bounds.");
	auto anim = assetHandle->mAnimations[animIndex];
	ThrowIf(channelIndex < 0 || channelIndex >= anim->mNumChannels, "Channel index out of bounds.");
	auto channel = anim->mChannels[channelIndex];
	ThrowIf(scalingBufferCount < channel->mNumScalingKeys, "Scaling buffer too small.");
	ThrowIf(rotationBufferCount < channel->mNumRotationKeys, "Rotation buffer too small.");
	ThrowIf(translationBufferCount < channel->mNumPositionKeys, "Translation buffer too small.");
	
	auto tps = static_cast<double>(ticksPerSecOverride);
	if (tps == 0.0) tps = (anim->mTicksPerSecond > 0.0 ? anim->mTicksPerSecond : 25.0);
	auto ticksToSecondsCoefficient = 1.0 / tps;

	for (auto i = 0U; i < channel->mNumScalingKeys; ++i) {
		auto key = channel->mScalingKeys[i];
		scalingVectorBuffer[i] = { key.mValue.x, key.mValue.y, key.mValue.z };
		scalingTimeBuffer[i] = static_cast<float_t>(key.mTime * ticksToSecondsCoefficient);
	}

	for (auto i = 0U; i < channel->mNumRotationKeys; ++i) {
		auto key = channel->mRotationKeys[i];
		rotationQuaternionBuffer[i] = quatf(key.mValue.w, key.mValue.x, key.mValue.y, key.mValue.z);
		rotationTimeBuffer[i] = static_cast<float_t>(key.mTime * ticksToSecondsCoefficient);
	}

	for (auto i = 0U; i < channel->mNumPositionKeys; ++i) {
		auto key = channel->mPositionKeys[i];
		translationVectorBuffer[i] = { key.mValue.x, key.mValue.y, key.mValue.z };
		translationTimeBuffer[i] = static_cast<float_t>(key.mTime * ticksToSecondsCoefficient);
	}
}
StartExportedFunc(copy_loaded_asset_mesh_skeletal_animation_channel_data, MemoryLoadedAssetHandle assetHandle, int32_t animIndex, int32_t channelIndex, float_t ticksPerSecOverride, float3* scalingVectorBuffer, float_t* scalingTimeBuffer, int32_t scalingBufferCount, quatf* rotationQuaternionBuffer, float_t* rotationTimeBuffer, int32_t rotationBufferCount, float3* translationVectorBuffer, float_t* translationTimeBuffer, int32_t translationBufferCount) {
	native_impl_asset_loader::copy_loaded_asset_mesh_skeletal_animation_channel_data(assetHandle, animIndex, channelIndex, ticksPerSecOverride, scalingVectorBuffer, scalingTimeBuffer, scalingBufferCount, rotationQuaternionBuffer, rotationTimeBuffer, rotationBufferCount, translationVectorBuffer, translationTimeBuffer, translationBufferCount);
	EndExportedFunc
}

aiNode* lookup_node_by_name(aiNode* root, const aiString& name) {
	if (root->mName == name) return root;
	for (auto i = 0U; i < root->mNumChildren; ++i) {
		auto result = lookup_node_by_name(root->mChildren[i], name);
		if (result != nullptr) return result;
	}
	ThrowIf(root->mParent == nullptr, "Could not find node '", name.C_Str(), "'"); 
	return nullptr;
}

mat4f transpose_aimat_and_write_to_filament_mat(const aiMatrix4x4& m) {
	return mat4f{
		float4{ m.a1, m.b1, m.c1, m.d1 },
		float4{ m.a2, m.b2, m.c2, m.d2 },
		float4{ m.a3, m.b3, m.c3, m.d3 },
		float4{ m.a4, m.b4, m.c4, m.d4 }
	};
}

unsigned int count_nodes(aiNode* node) {
	auto result = 1U;
	for (auto i = 0U; i < node->mNumChildren; ++i) {
		result += count_nodes(node->mChildren[i]);
	}
	return result;
}

void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_node_count(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outNodeCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(outNodeCount, "Out node count pointer was null.");

	auto unused = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, unused);
	if (!mesh->HasBones()) *outNodeCount = 0;
	else *outNodeCount = static_cast<int32_t>(count_nodes(mesh->mBones[0]->mArmature));
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_node_count, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, int32_t* outNodeCount) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_node_count(assetHandle, meshIndex, outNodeCount);
	EndExportedFunc
}

unsigned int write_nodes(aiMesh* mesh, aiNode* node, native_impl_asset_loader::NodeHandle* buffer, unsigned int remainingBufferCount, std::unordered_map<std::string, unsigned int>& boneMap) {
	ThrowIf(remainingBufferCount == 0U, "Buffer not large enough.");
	auto result = 1U;
	auto kvp = boneMap.find(node->mName.C_Str());
	auto boneExists = kvp != boneMap.end();
	buffer[0] = {
		.Node = node,
		.BoneIfExists = boneExists ? mesh->mBones[kvp->second] : nullptr,
		.BoneIndex = boneExists ? static_cast<int32_t>(kvp->second) : -1
	};
	for (auto i = 0U; i < node->mNumChildren; ++i) {
		result += write_nodes(mesh, node->mChildren[i], buffer + result, remainingBufferCount - result, boneMap);
	}
	return result;
}

void native_impl_asset_loader::generate_loaded_asset_mesh_skeletal_node_flat_buffer(MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, NodeHandle* nodeHandleBuffer, int32_t handleBufferCount) {
	ThrowIfNull(assetHandle, "Asset handle pointer was null.");
	ThrowIfNull(nodeHandleBuffer, "Node handle buffer pointer was null.");
	ThrowIf(handleBufferCount <= 0, "Invalid handle buffer count.");
	
	auto unused = aiMatrix4x4{};
	auto mesh = get_mesh_at_index(assetHandle, meshIndex, unused);
	ThrowIf(!mesh->HasBones(), "Can not create flattened node buffer for mesh with no bones.");
	
	std::unordered_map<std::string, unsigned int> boneMap { };
	for (auto i = 0; i < mesh->mNumBones; ++i) {
		boneMap[mesh->mBones[i]->mName.C_Str()] = i;
	}
	
	auto numNodesWritten = write_nodes(mesh, mesh->mBones[0]->mArmature, nodeHandleBuffer, static_cast<unsigned int>(handleBufferCount), boneMap);	
	ThrowIf(numNodesWritten != static_cast<unsigned int>(handleBufferCount), "Buffer count did not match node count.");
}
StartExportedFunc(generate_loaded_asset_mesh_skeletal_node_flat_buffer, MemoryLoadedAssetHandle assetHandle, int32_t meshIndex, native_impl_asset_loader::NodeHandle* nodeHandleBuffer, int32_t handleBufferCount) {
	native_impl_asset_loader::generate_loaded_asset_mesh_skeletal_node_flat_buffer(assetHandle, meshIndex, nodeHandleBuffer, handleBufferCount);
	EndExportedFunc
}

void native_impl_asset_loader::get_loaded_asset_mesh_skeletal_node(NodeHandle* nodeHandleBuffer, int32_t handleBufferCount, int32_t nodeIndex, mat4f* outInverseBindPose, mat4f* outDefaultTransform, int32_t* outParentNodeIndex, int32_t* outBoneIndex, int32_t* outNodeNameLengthBytes) {
    ThrowIfNull(nodeHandleBuffer, "Node handle buffer pointer was null.");
	ThrowIf(handleBufferCount <= 0, "Handle buffer count was invalid.");
    ThrowIfNull(outInverseBindPose, "Out inverse bind pose pointer was null.");
    ThrowIfNull(outDefaultTransform, "Out default transform pointer was null.");
    ThrowIfNull(outParentNodeIndex, "Out parent node index was null.");
    ThrowIfNull(outBoneIndex, "Out bone index was null.");
	ThrowIf(nodeIndex >= handleBufferCount, "Node index was >= handle buffer count.");
	
	auto identityMatrix = mat4f { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f };
		
	auto nodeHandle = nodeHandleBuffer[nodeIndex];
	*outInverseBindPose = nodeHandle.BoneIfExists == nullptr ? identityMatrix : transpose_aimat_and_write_to_filament_mat(nodeHandle.BoneIfExists->mOffsetMatrix);
	*outDefaultTransform = transpose_aimat_and_write_to_filament_mat(nodeHandle.Node->mTransformation);
	
	*outParentNodeIndex = -1;
	auto* parentNode = nodeHandle.Node->mParent;
	for (auto i = nodeIndex - 1; i >= 0; --i) {
		if (nodeHandleBuffer[i].Node == parentNode) {
			*outParentNodeIndex = i;
			break;
		}
	}
	
	*outBoneIndex = nodeHandle.BoneIndex;
	*outNodeNameLengthBytes = static_cast<int32_t>(nodeHandle.Node->mName.length);
}
StartExportedFunc(get_loaded_asset_mesh_skeletal_node, native_impl_asset_loader::NodeHandle* nodeHandleBuffer, int32_t handleBufferCount, int32_t nodeIndex, mat4f* outInverseBindPose, mat4f* outDefaultTransform, int32_t* outParentNodeIndex, int32_t* outBoneIndex, int32_t* outNodeNameLengthBytes) {
	native_impl_asset_loader::get_loaded_asset_mesh_skeletal_node(nodeHandleBuffer, handleBufferCount, nodeIndex, outInverseBindPose, outDefaultTransform, outParentNodeIndex, outBoneIndex, outNodeNameLengthBytes);
	EndExportedFunc
}
#pragma endregion

#pragma region Standalone File I/O
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
#pragma endregion