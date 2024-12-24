#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"

using namespace filament;
using namespace filament::math;

typedef int32_t LightHandle;

class native_impl_lights {
public:
	static void allocate_point_light(LightHandle* outLight);
	static void get_light_position(LightHandle light, float3* outPosition);
	static void set_light_position(LightHandle light, float3 newPosition);
	static void get_light_color(LightHandle light, float3* outColor);
	static void set_light_color(LightHandle light, float3 newColor);
	static void get_point_light_lumens(LightHandle light, float* outLumens);
	static void set_point_light_lumens(LightHandle light, float newLumens);
	static void get_point_light_max_illumination_radius(LightHandle light, float* outRadius);
	static void set_point_light_max_illumination_radius(LightHandle light, float newRadius);
	static void dispose_light(LightHandle light);
};