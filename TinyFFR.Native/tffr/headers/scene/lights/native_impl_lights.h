#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"

using namespace filament;
using namespace filament::math;

typedef int32_t LightHandle;

class native_impl_lights {
public:
	static void get_light_position(LightHandle light, float3* outPosition);
	static void set_light_position(LightHandle light, float3 newPosition);
	static void get_light_color(LightHandle light, float3* outColor);
	static void set_light_color(LightHandle light, float3 newColor);
	static void get_light_shadow_caster(LightHandle light, interop_bool* outIsShadowCaster);
	static void set_light_shadow_caster(LightHandle light, interop_bool isShadowCaster);
	static void set_light_shadow_fidelity(LightHandle light, uint32_t mapSize, uint8_t cascadeCount);

	static void allocate_point_light(LightHandle* outLight);
	static void get_point_light_lumens(LightHandle light, float* outLumens);
	static void set_point_light_lumens(LightHandle light, float newLumens);
	static void get_point_light_max_illumination_radius(LightHandle light, float* outRadius);
	static void set_point_light_max_illumination_radius(LightHandle light, float newRadius);

	static void allocate_spot_light(interop_bool highAccuracy, LightHandle* outLight);
	static void get_spot_light_lumens(LightHandle light, float* outLumens);
	static void set_spot_light_lumens(LightHandle light, float newLumens);
	static void get_spot_light_direction(LightHandle light, float3* outDir);
	static void set_spot_light_direction(LightHandle light, float3 newDir);
	static void get_spot_light_radii(LightHandle light, float* outInnerRadius, float* outOuterRadius);
	static void set_spot_light_radii(LightHandle light, float newInnerRadius, float newOuterRadius);
	static void get_spot_light_max_distance(LightHandle light, float* outDistance);
	static void set_spot_light_max_distance(LightHandle light, float newDistance);

	static void allocate_sun_light(interop_bool includeSunDisc, LightHandle* outLight);
	static void get_sun_light_lux(LightHandle light, float* outLux);
	static void set_sun_light_lux(LightHandle light, float newLux);
	static void get_sun_light_direction(LightHandle light, float3* outDirection);
	static void set_sun_light_direction(LightHandle light, float3 newDirection);
	static void set_sun_parameters(LightHandle light, float angularSize, float haloCoefficient, float haloFalloffExponent);

	static void dispose_light(LightHandle light);
};