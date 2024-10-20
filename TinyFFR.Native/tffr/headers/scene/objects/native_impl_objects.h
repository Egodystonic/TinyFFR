#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"

using namespace filament;
using namespace filament::math;

typedef int32_t ModelInstanceHandle;

class native_impl_objects {
public:
	static void allocate_model_instance(VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, MaterialHandle material, ModelInstanceHandle* outModelInstance);
	static void dispose_model_instance(ModelInstanceHandle modelInstance);
};