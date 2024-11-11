#include "pch.h"
#include "scene/objects/native_impl_objects.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"
#include "filament/RenderableManager.h"
#include "filament/TransformManager.h"

using namespace utils;

void native_impl_objects::allocate_model_instance(mat4f* initialTransformPtr, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, MaterialHandle material, ModelInstanceHandle* outModelInstance) {
	auto entity = filament_engine->getEntityManager().create();

	auto result = RenderableManager::Builder(1)
		.culling(false)
		.geometry(0, RenderableManager::PrimitiveType::TRIANGLES, vb, ib, ibStartIndex, ibCount)
		.material(0, material)
		.boundingBox({ { 0.0, 0.0, 0.0 }, { 1.0, 1.0, 1.0 } })
		.build(*filament_engine, entity);

	if (result != RenderableManager::Builder::Success) Throw("Could not create entity.");

	filament_engine->getTransformManager().create(entity, TransformManager::Instance{}, *initialTransformPtr);
	filament_engine->getTransformManager().create(entity, TransformManager::Instance{});
	*outModelInstance = Entity::smuggle(entity);
}
StartExportedFunc(allocate_model_instance, mat4f* initialTransformPtr, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount, MaterialHandle material, ModelInstanceHandle* outModelInstance) {
	native_impl_objects::allocate_model_instance(initialTransformPtr, vb, ib, ibStartIndex, ibCount, material, outModelInstance);
	EndExportedFunc
}

void native_impl_objects::set_model_instance_mesh(ModelInstanceHandle modelInstance, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount) {
	auto& manager = filament_engine->getRenderableManager();
	auto instance = manager.getInstance(Entity::import(modelInstance));
	ThrowIf(!instance.isValid(), "Given entity instance was not associated with any renderable.");
	manager.setGeometryAt(instance, 0, RenderableManager::PrimitiveType::TRIANGLES, vb, ib, ibStartIndex, ibCount);
}
StartExportedFunc(set_model_instance_mesh, ModelInstanceHandle modelInstance, VertexBufferHandle vb, IndexBufferHandle ib, int32_t ibStartIndex, int32_t ibCount) {
	native_impl_objects::set_model_instance_mesh(modelInstance, vb, ib, ibStartIndex, ibCount);
	EndExportedFunc
}

void native_impl_objects::set_model_instance_material(ModelInstanceHandle modelInstance, MaterialHandle material) {
	auto& manager = filament_engine->getRenderableManager();
	auto instance = manager.getInstance(Entity::import(modelInstance));
	ThrowIf(!instance.isValid(), "Given entity instance was not associated with any renderable.");
	manager.setMaterialInstanceAt(instance, 0, material);
}
StartExportedFunc(set_model_instance_material, ModelInstanceHandle modelInstance, MaterialHandle material) {
	native_impl_objects::set_model_instance_material(modelInstance, material);
	EndExportedFunc
}

void native_impl_objects::set_model_instance_world_mat(ModelInstanceHandle modelInstance, mat4f* worldMatPtr) {
	auto entity = Entity::import(modelInstance);
	auto& manager = filament_engine->getTransformManager();
	manager.setTransform(manager.getInstance(entity), *worldMatPtr);
}
StartExportedFunc(set_model_instance_world_mat, ModelInstanceHandle modelInstance, mat4f* worldMatPtr) {
	native_impl_objects::set_model_instance_world_mat(modelInstance, worldMatPtr);
	EndExportedFunc
}


void native_impl_objects::dispose_model_instance(ModelInstanceHandle modelInstance) {
	auto entity = Entity::import(modelInstance);
	filament_engine->getTransformManager().destroy(entity);
	filament_engine->getRenderableManager().destroy(entity);
	filament_engine->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_model_instance, ModelInstanceHandle modelInstance) {
	native_impl_objects::dispose_model_instance(modelInstance);
	EndExportedFunc
}
