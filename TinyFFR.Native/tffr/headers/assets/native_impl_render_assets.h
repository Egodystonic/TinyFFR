#pragma once

#include "native_impl_init.h"
#include "utils_and_constants.h"
#include "filament/filament/IndexBuffer.h"
#include "filament/filament/VertexBuffer.h"
#include "filamat/Package.h"
#include "filament/Material.h"

using namespace filament;
using namespace filament::math;

typedef VertexBuffer* VertexBufferHandle;
typedef IndexBuffer* IndexBufferHandle;

typedef MaterialInstance* MaterialHandle;

enum MaterialType : int32_t {
	Undefined = 0,
	BasicSolidColor
};

class native_impl_render_assets {
public:
	PushSafeStructPacking
	struct MeshVertex {
		float3 Position;
		float2 TextureUV;
	};
	PopSafeStructPacking
	static_assert(sizeof(MeshVertex) == 20);

	static void allocate_vertex_buffer(BufferIdentity bufferIdentity, MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer);
	static void allocate_index_buffer(BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer);
	static void dispose_vertex_buffer(VertexBufferHandle buffer);
	static void dispose_index_buffer(IndexBufferHandle buffer);

	static filamat::Package BasicSolidColorShaderPackage;
	static Material* BasicSolidColorShaderMaterial;

	static void create_material(MaterialType type, void* argumentsBuffer, int32_t argumentsBufferLengthBytes, MaterialHandle* outMaterial);
};