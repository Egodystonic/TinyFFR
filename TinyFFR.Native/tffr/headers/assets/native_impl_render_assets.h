#pragma once

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/IndexBuffer.h"
#include "filament/VertexBuffer.h"
#include "filamat/Package.h"
#include "filament/Material.h"
#include "filament/Texture.h"

using namespace filament;
using namespace filament::math;

typedef VertexBuffer* VertexBufferHandle;
typedef IndexBuffer* IndexBufferHandle;

typedef Texture* TextureHandle;
typedef Material* PackageHandle;
typedef MaterialInstance* MaterialHandle;

class native_impl_render_assets {
public:
	PushSafeStructPacking
	struct MeshVertex {
		float3 Position;
		float2 TextureUV;
		float4 Tangent;
	};
	PopSafeStructPacking
	static_assert(sizeof(MeshVertex) == 36);

	static void allocate_vertex_buffer(BufferIdentity bufferIdentity, MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer);
	static void allocate_index_buffer(BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer);
	static void dispose_vertex_buffer(VertexBufferHandle buffer);
	static void dispose_index_buffer(IndexBufferHandle buffer);
	static void calculate_tangent_rotation(float3 tangent, float3 bitangent, float3 normal, float4* outRotation);

	static void load_texture_rgb_24(BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, TextureHandle* outTexture);
	static void load_texture_rgba_32(BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, TextureHandle* outTexture);
	static void dispose_texture(TextureHandle texture);

	static void load_shader_package(void* dataPtr, int32_t dataLen, PackageHandle* outHandle);
	static void create_material(PackageHandle package, MaterialHandle* outMaterial);
	static void set_material_parameter_texture(MaterialHandle material, const char* parameterName, int32_t parameterNameLength, TextureHandle texture);
	static void dispose_material(MaterialHandle material);
	static void dispose_shader_package(PackageHandle handle);
};