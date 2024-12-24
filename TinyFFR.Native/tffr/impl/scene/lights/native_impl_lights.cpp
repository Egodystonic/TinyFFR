#include "pch.h"
#include "scene/lights/native_impl_lights.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"
#include "filament/LightManager.h"
#include "filament/TransformManager.h"

using namespace utils;

void native_impl_lights::allocate_point_light(LightHandle* outLight) {
	ThrowIfNull(outLight, "Light out pointer was null.");

	auto entity = filament_engine->getEntityManager().create();

	auto result = LightManager::Builder(LightManager::Type::POINT).build(*filament_engine, entity);
	if (result != LightManager::Builder::Success) Throw("Could not create entity.");
	*outLight = Entity::smuggle(entity);
}
StartExportedFunc(allocate_point_light, LightHandle* outLight) {
	native_impl_lights::allocate_point_light(outLight);
	EndExportedFunc
}

void native_impl_lights::get_light_color(LightHandle light, float3* outColor) {
	ThrowIfNull(outColor, "Out color pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outColor = manager.getColor(instance);
}
StartExportedFunc(get_light_color, LightHandle light, float3* outColor) {
	native_impl_lights::get_light_color(light, outColor);
	EndExportedFunc
}

void native_impl_lights::set_light_color(LightHandle light, float3 newColor) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setColor(instance, LinearColor{ newColor });
}
StartExportedFunc(set_light_color, LightHandle light, float3 newColor) {
	native_impl_lights::set_light_color(light, newColor);
	EndExportedFunc
}

void native_impl_lights::get_light_position(LightHandle light, float3* outPosition) {
	ThrowIfNull(outPosition, "Out position pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outPosition = manager.getPosition(instance);
}
StartExportedFunc(get_light_position, LightHandle light, float3* outPosition) {
	native_impl_lights::get_light_position(light, outPosition);
	EndExportedFunc
}

void native_impl_lights::set_light_position(LightHandle light, float3 newPosition) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setPosition(instance, newPosition);
}
StartExportedFunc(set_light_position, LightHandle light, float3 newPosition) {
	native_impl_lights::set_light_position(light, newPosition);
	EndExportedFunc
}

void native_impl_lights::get_point_light_lumens(LightHandle light, float* outLumens) {
	ThrowIfNull(outLumens, "Out lumens pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outLumens = manager.getIntensity(instance);
}
StartExportedFunc(get_point_light_lumens, LightHandle light, float* outLumens) {
	native_impl_lights::get_point_light_lumens(light, outLumens);
	EndExportedFunc
}

void native_impl_lights::set_point_light_lumens(LightHandle light, float newLumens) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setIntensity(instance, newLumens);
}
StartExportedFunc(set_point_light_lumens, LightHandle light, float newLumens) {
	native_impl_lights::set_point_light_lumens(light, newLumens);
	EndExportedFunc
}

void native_impl_lights::get_point_light_max_illumination_radius(LightHandle light, float* outRadius) {
	ThrowIfNull(outRadius, "Out radius pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outRadius = manager.getFalloff(instance);
}
StartExportedFunc(get_point_light_max_illumination_radius, LightHandle light, float* outRadius) {
	native_impl_lights::get_point_light_lumens(light, outRadius);
	EndExportedFunc
}

void native_impl_lights::set_point_light_max_illumination_radius(LightHandle light, float newRadius) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setFalloff(instance, newRadius);
}
StartExportedFunc(set_point_light_max_illumination_radius, LightHandle light, float newRadius) {
	native_impl_lights::set_point_light_max_illumination_radius(light, newRadius);
	EndExportedFunc
}

void native_impl_lights::dispose_light(LightHandle light) {
	auto entity = Entity::import(light);
	filament_engine->getLightManager().destroy(entity);
	filament_engine->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_light, LightHandle light) {
	native_impl_lights::dispose_light(light);
	EndExportedFunc
}