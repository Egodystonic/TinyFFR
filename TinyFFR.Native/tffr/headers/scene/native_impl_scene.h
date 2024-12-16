#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"
#include "objects/native_impl_objects.h"
#include "lights/native_impl_lights.h"

using namespace filament;
using namespace filament::math;

typedef Scene* SceneHandle;

class native_impl_scene {
public:
	static void allocate_scene(SceneHandle* outScene);
	static void add_model_instance_to_scene(SceneHandle scene, ModelInstanceHandle modelInstance);
	static void remove_model_instance_from_scene(SceneHandle scene, ModelInstanceHandle modelInstance);
	static void add_light_to_scene(SceneHandle scene, LightHandle light);
	static void remove_light_from_scene(SceneHandle scene, LightHandle light);
	static void dispose_scene(SceneHandle scene);
};