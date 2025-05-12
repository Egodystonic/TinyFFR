#include "pch.h"
#include "scene/native_impl_scene.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/Scene.h"
#include "filament/Skybox.h"
#include "filament/IndirectLight.h"
#include "scene/native_impl_render.h"

using namespace utils;

void native_impl_scene::allocate_scene(SceneHandle* outScene) {
	ThrowIfNull(outScene, "Scene out pointer was null.");
	*outScene = filament_engine->createScene();
	ThrowIfNull(*outScene, "Could not create scene.");
}
StartExportedFunc(allocate_scene, SceneHandle* outScene) {
	native_impl_scene::allocate_scene(outScene);
	EndExportedFunc
}

void native_impl_scene::add_model_instance_to_scene(SceneHandle scene, ModelInstanceHandle modelInstance) {
	ThrowIfNull(scene, "Scene was null.");
	scene->addEntity(Entity::import(modelInstance));
}
StartExportedFunc(add_model_instance_to_scene, SceneHandle scene, ModelInstanceHandle modelInstance) {
	native_impl_scene::add_model_instance_to_scene(scene, modelInstance);
	EndExportedFunc
}

void native_impl_scene::remove_model_instance_from_scene(SceneHandle scene, ModelInstanceHandle modelInstance) {
	ThrowIfNull(scene, "Scene was null.");
	scene->remove(Entity::import(modelInstance));
}
StartExportedFunc(remove_model_instance_from_scene, SceneHandle scene, ModelInstanceHandle modelInstance) {
	native_impl_scene::remove_model_instance_from_scene(scene, modelInstance);
	EndExportedFunc
}

void native_impl_scene::add_light_to_scene(SceneHandle scene, LightHandle light) {
	ThrowIfNull(scene, "Scene was null.");
	scene->addEntity(Entity::import(light));
}
StartExportedFunc(add_light_to_scene, SceneHandle scene, LightHandle light) {
	native_impl_scene::add_light_to_scene(scene, light);
	EndExportedFunc
}

void native_impl_scene::remove_light_from_scene(SceneHandle scene, LightHandle light) {
	ThrowIfNull(scene, "Scene was null.");
	scene->remove(Entity::import(light));
}
StartExportedFunc(remove_light_from_scene, SceneHandle scene, LightHandle light) {
	native_impl_scene::remove_light_from_scene(scene, light);
	EndExportedFunc
}

void native_impl_scene::create_scene_backdrop_color(float3 color, float indirectLightingIntensity, SkyboxHandle* outSkybox, IndirectLightHandle* outIndirectLight) {
	ThrowIfNull(outSkybox, "Out skybox pointer was null.");
	ThrowIfNull(outIndirectLight, "Out indirect light pointer was null.");

	*outSkybox = Skybox::Builder()
		.color(float4{ color, 1.0f })
		.intensity(indirectLightingIntensity)
		.showSun(true)
		.build(*filament_engine);
	ThrowIfNull(*outSkybox, "Could not create skybox.");

	*outIndirectLight = IndirectLight::Builder()
		.irradiance(1, &color)
		.intensity(indirectLightingIntensity)
		.build(*filament_engine);
	ThrowIfNull(*outIndirectLight, "Could not create indirect light.");
}
StartExportedFunc(create_scene_backdrop_color, float3 color, float indirectLightingIntensity, SkyboxHandle* outSkybox, IndirectLightHandle* outIndirectLight) {
	native_impl_scene::create_scene_backdrop_color(color, indirectLightingIntensity, outSkybox, outIndirectLight);
	EndExportedFunc
}

void native_impl_scene::create_scene_backdrop_texture(TextureHandle skyboxTexture, TextureHandle iblTexture, float indirectLightingIntensity, SkyboxHandle* outSkybox, IndirectLightHandle* outIndirectLight) {
	ThrowIfNull(skyboxTexture, "Skybox texture was null.");
	ThrowIfNull(iblTexture, "IBL texture was null.");
	ThrowIfNull(outSkybox, "Out skybox pointer was null.");
	ThrowIfNull(outIndirectLight, "Out indirect light pointer was null.");

	*outSkybox = Skybox::Builder()
		.environment(skyboxTexture)
		.intensity(indirectLightingIntensity)
		.showSun(true)
		.build(*filament_engine);
	ThrowIfNull(*outSkybox, "Could not create skybox.");

	*outIndirectLight = IndirectLight::Builder()
		.reflections(iblTexture)
		.intensity(indirectLightingIntensity)
		.build(*filament_engine);
	ThrowIfNull(*outIndirectLight, "Could not create indirect light.");
}
StartExportedFunc(create_scene_backdrop_texture, TextureHandle skyboxTexture, TextureHandle iblTexture, float indirectLightingIntensity, SkyboxHandle* outSkybox, IndirectLightHandle* outIndirectLight) {
	native_impl_scene::create_scene_backdrop_texture(skyboxTexture, iblTexture, indirectLightingIntensity, outSkybox, outIndirectLight);
	EndExportedFunc
}
void native_impl_scene::set_scene_backdrop(SceneHandle scene, SkyboxHandle skybox, IndirectLightHandle indirectLight) {
	ThrowIfNull(scene, "Scene was null.");
	ThrowIfNull(skybox, "Skybox was null.");

	scene->setSkybox(skybox);
	scene->setIndirectLight(indirectLight);
}
StartExportedFunc(set_scene_backdrop, SceneHandle scene, SkyboxHandle skybox, IndirectLightHandle indirectLight) {
	native_impl_scene::set_scene_backdrop(scene, skybox, indirectLight);
	EndExportedFunc
}
void native_impl_scene::unset_scene_backdrop(SceneHandle scene) {
	ThrowIfNull(scene, "Scene was null.");

	scene->setSkybox(nullptr);
	scene->setIndirectLight(nullptr);
}
StartExportedFunc(unset_scene_backdrop, SceneHandle scene) {
	native_impl_scene::unset_scene_backdrop(scene);
	EndExportedFunc
}
void native_impl_scene::dispose_scene_backdrop(SkyboxHandle skybox, IndirectLightHandle indirectLight) {
	ThrowIfNull(skybox, "Skybox was null.");
	ThrowIfNull(indirectLight, "Light was null.");

	filament_engine->destroy(indirectLight);
	filament_engine->destroy(skybox);
}
StartExportedFunc(dispose_scene_backdrop, SkyboxHandle skybox, IndirectLightHandle indirectLight) {
	native_impl_scene::dispose_scene_backdrop(skybox, indirectLight);
	EndExportedFunc
}

void native_impl_scene::dispose_scene(SceneHandle scene) {
	ThrowIfNull(scene, "Scene was null.");
	ThrowIf(!filament_engine->destroy(scene), "Could not dispose scene.");
}
StartExportedFunc(dispose_scene, SceneHandle scene) {
	native_impl_scene::dispose_scene(scene);
	EndExportedFunc
}