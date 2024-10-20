#include "pch.h"
#include "scene/objects/native_impl_objects.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"
#include "filament/RenderableManager.h"

using namespace utils;

void native_impl_objects::allocate_model_instance(VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, MaterialHandle material, ModelInstanceHandle* outModelInstance) {
	auto entity = native_impl_init::filament_engine_ptr->getEntityManager().create();

	auto result = RenderableManager::Builder(1)
		.culling(false)
		.geometry(0, RenderableManager::PrimitiveType::TRIANGLES, vb, ib, ibStartIndex, ibCount)
		.material(0, material)
		.build(*native_impl_init::filament_engine_ptr, entity);

	if (result != RenderableManager::Builder::Success) Throw("Could not create entity.");

	*outModelInstance = Entity::smuggle(entity);
}
StartExportedFunc(allocate_model_instance, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, MaterialHandle material, ModelInstanceHandle* outModelInstance) {
	native_impl_objects::allocate_model_instance(vb, ib, ibStartIndex, ibCount, material, outModelInstance);
	EndExportedFunc
}

void native_impl_objects::dispose_model_instance(ModelInstanceHandle modelInstance) {
	auto entity = Entity::import(modelInstance);
	native_impl_init::filament_engine_ptr->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_model_instance, ModelInstanceHandle modelInstance) {
	native_impl_objects::dispose_model_instance(modelInstance);
	EndExportedFunc
}
