#include "pch.h"
#include "scene/lights/native_impl_lights.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"
#include "filament/LightManager.h"
#include "filament/TransformManager.h"

using namespace utils;

float candela_to_lumens(float candela) {
	return candela * 4.0f * math::f::PI;
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

void native_impl_lights::get_point_light_lumens(LightHandle light, float* outLumens) {
	ThrowIfNull(outLumens, "Out lumens pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outLumens = candela_to_lumens(manager.getIntensity(instance));
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
	native_impl_lights::get_point_light_max_illumination_radius(light, outRadius);
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



void native_impl_lights::allocate_spot_light(interop_bool highAccuracy, LightHandle* outLight) {
	ThrowIfNull(outLight, "Light out pointer was null.");

	auto entity = filament_engine->getEntityManager().create();

	auto result = LightManager::Builder(highAccuracy ? LightManager::Type::FOCUSED_SPOT : LightManager::Type::SPOT).build(*filament_engine, entity);
	if (result != LightManager::Builder::Success) Throw("Could not create entity.");
	*outLight = Entity::smuggle(entity);
}
StartExportedFunc(allocate_spot_light, interop_bool highAccuracy, LightHandle* outLight) {
	native_impl_lights::allocate_spot_light(highAccuracy, outLight);
	EndExportedFunc
}

void native_impl_lights::get_spot_light_lumens(LightHandle light, float* outLumens) {
	ThrowIfNull(outLumens, "Out lumens pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outLumens = candela_to_lumens(manager.getIntensity(instance));
}
StartExportedFunc(get_spot_light_lumens, LightHandle light, float* outLumens) {
	native_impl_lights::get_spot_light_lumens(light, outLumens);
	EndExportedFunc
}
void native_impl_lights::set_spot_light_lumens(LightHandle light, float newLumens) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setIntensity(instance, newLumens);
}
StartExportedFunc(set_spot_light_lumens, LightHandle light, float newLumens) {
	native_impl_lights::set_spot_light_lumens(light, newLumens);
	EndExportedFunc
}

void native_impl_lights::get_spot_light_direction(LightHandle light, float3* outDir) {
	ThrowIfNull(outDir, "Out direction pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outDir = manager.getDirection(instance);
}
StartExportedFunc(get_spot_light_direction, LightHandle light, float3* outDir) {
	native_impl_lights::get_spot_light_direction(light, outDir);
	EndExportedFunc
}
void native_impl_lights::set_spot_light_direction(LightHandle light, float3 newDir) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setDirection(instance, newDir);
}
StartExportedFunc(set_spot_light_direction, LightHandle light, float3 newDir) {
	native_impl_lights::set_spot_light_direction(light, newDir);
	EndExportedFunc
}

void native_impl_lights::get_spot_light_radii(LightHandle light, float* outInnerRadius, float* outOuterRadius) {
	ThrowIfNull(outInnerRadius, "Out inner radius pointer was null.");
	ThrowIfNull(outOuterRadius, "Out outer radius pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outInnerRadius = manager.getSpotLightInnerCone(instance);
	*outOuterRadius = manager.getSpotLightOuterCone(instance);
}
StartExportedFunc(get_spot_light_radii, LightHandle light, float* outInnerRadius, float* outOuterRadius) {
	native_impl_lights::get_spot_light_radii(light, outInnerRadius, outOuterRadius);
	EndExportedFunc
}
void native_impl_lights::set_spot_light_radii(LightHandle light, float newInnerRadius, float newOuterRadius) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setSpotLightCone(instance, newInnerRadius, newOuterRadius);
}
StartExportedFunc(set_spot_light_radii, LightHandle light, float newInnerRadius, float newOuterRadius) {
	native_impl_lights::set_spot_light_radii(light, newInnerRadius, newOuterRadius);
	EndExportedFunc
}


void native_impl_lights::get_spot_light_max_distance(LightHandle light, float* outDistance) {
	ThrowIfNull(outDistance, "Out distance pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outDistance = manager.getFalloff(instance);
}
StartExportedFunc(get_spot_light_max_distance, LightHandle light, float* outDistance) {
	native_impl_lights::get_spot_light_max_distance(light, outDistance);
	EndExportedFunc
}
void native_impl_lights::set_spot_light_max_distance(LightHandle light, float newDistance) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setFalloff(instance, newDistance);
}
StartExportedFunc(set_spot_light_max_distance, LightHandle light, float newDistance) {
	native_impl_lights::set_spot_light_max_distance(light, newDistance);
	EndExportedFunc
}
void native_impl_lights::get_sun_light_direction(LightHandle light, float3* outDir) {
	ThrowIfNull(outDir, "Out direction pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outDir = manager.getDirection(instance);
}
StartExportedFunc(get_sun_light_direction, LightHandle light, float3* outDir) {
	native_impl_lights::get_sun_light_direction(light, outDir);
	EndExportedFunc
}
void native_impl_lights::set_sun_light_direction(LightHandle light, float3 newDir) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setDirection(instance, newDir);
}
StartExportedFunc(set_sun_light_direction, LightHandle light, float3 newDir) {
	native_impl_lights::set_sun_light_direction(light, newDir);
	EndExportedFunc
}





void native_impl_lights::allocate_sun_light(interop_bool includeSunDisc, LightHandle* outLight) {
	ThrowIfNull(outLight, "Light out pointer was null.");

	auto entity = filament_engine->getEntityManager().create();

	auto result = LightManager::Builder(includeSunDisc ? LightManager::Type::SUN : LightManager::Type::DIRECTIONAL).build(*filament_engine, entity);
	if (result != LightManager::Builder::Success) Throw("Could not create entity.");
	*outLight = Entity::smuggle(entity);
}
StartExportedFunc(allocate_sun_light, interop_bool includeSunDisc, LightHandle* outLight) {
	native_impl_lights::allocate_sun_light(includeSunDisc, outLight);
	EndExportedFunc
}
void native_impl_lights::get_sun_light_lux(LightHandle light, float* outLux) {
	ThrowIfNull(outLux, "Out lux pointer was null.");
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	*outLux = manager.getIntensity(instance);
}
StartExportedFunc(get_sun_light_lux, LightHandle light, float* outLux) {
	native_impl_lights::get_sun_light_lux(light, outLux);
	EndExportedFunc
}
void native_impl_lights::set_sun_light_lux(LightHandle light, float newLux) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setIntensity(instance, newLux);
}
StartExportedFunc(set_sun_light_lux, LightHandle light, float newLux) {
	native_impl_lights::set_sun_light_lux(light, newLux);
	EndExportedFunc
}
void native_impl_lights::set_sun_parameters(LightHandle light, float angularSize, float haloCoefficient, float haloFalloffExponent) {
	auto entity = Entity::import(light);
	auto& manager = filament_engine->getLightManager();
	auto instance = manager.getInstance(entity);
	manager.setSunAngularRadius(instance, angularSize);
	manager.setSunHaloSize(instance, haloCoefficient);
	manager.setSunHaloFalloff(instance, haloFalloffExponent);
	manager.setShadowCaster(instance, true);
}
StartExportedFunc(set_sun_parameters, LightHandle light, float angularSize, float haloCoefficient, float haloFalloffExponent) {
	native_impl_lights::set_sun_parameters(light, angularSize, haloCoefficient, haloFalloffExponent);
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