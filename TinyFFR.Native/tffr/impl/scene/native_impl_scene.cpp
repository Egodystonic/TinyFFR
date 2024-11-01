#include "pch.h"
#include "scene/native_impl_scene.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/Scene.h"

using namespace utils;

void native_impl_scene::allocate_scene(SceneHandle* outScene) {
	*outScene = native_impl_init::filament_engine_ptr->createScene();
	ThrowIfNull(*outScene, "Could not create scene.");
}
StartExportedFunc(allocate_scene, SceneHandle* outScene) {
	native_impl_scene::allocate_scene(outScene);
	EndExportedFunc
}

void native_impl_scene::add_model_instance_to_scene(SceneHandle scene, ModelInstanceHandle modelInstance) {
	scene->addEntity(Entity::import(modelInstance));
}
StartExportedFunc(add_model_instance_to_scene, SceneHandle scene, ModelInstanceHandle modelInstance) {
	native_impl_scene::add_model_instance_to_scene(scene, modelInstance);
	EndExportedFunc
}

void native_impl_scene::remove_model_instance_from_scene(SceneHandle scene, ModelInstanceHandle modelInstance) {
	scene->remove(Entity::import(modelInstance));
}
StartExportedFunc(remove_model_instance_from_scene, SceneHandle scene, ModelInstanceHandle modelInstance) {
	native_impl_scene::remove_model_instance_from_scene(scene, modelInstance);
	EndExportedFunc
}

void native_impl_scene::dispose_scene(SceneHandle scene) {
	ThrowIf(!native_impl_init::filament_engine_ptr->destroy(scene), "Could not dispose scene.");
}
StartExportedFunc(dispose_scene, SceneHandle scene) {
	native_impl_scene::dispose_scene(scene);
	EndExportedFunc
}