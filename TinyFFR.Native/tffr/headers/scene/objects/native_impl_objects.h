#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"

using namespace filament;
using namespace filament::math;

typedef int32_t ModelInstanceHandle;

class native_impl_objects {
public:
	static void allocate_model_instance(mat4f* initialTransformPtr, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, int32_t boneCount, MaterialHandle material, ModelInstanceHandle* outModelInstance);
	static void set_model_instance_mesh(ModelInstanceHandle modelInstance, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount);
	static void set_model_instance_material(ModelInstanceHandle modelInstance, MaterialHandle material);
	static void set_model_instance_world_mat(ModelInstanceHandle modelInstance, mat4f* worldMatPtr);
	static void set_model_instance_bone_transforms(ModelInstanceHandle modelInstance, mat4f* transforms, int32_t boneCount);
	static void dispose_model_instance(ModelInstanceHandle modelInstance);
};