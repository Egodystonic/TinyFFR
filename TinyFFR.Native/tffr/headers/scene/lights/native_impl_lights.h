#pragma once

#include "utils_and_constants.h"
#include "assets/native_impl_render_assets.h"

using namespace filament;
using namespace filament::math;

typedef int32_t LightHandle;

class native_impl_lights {
public:
	static void allocate_point_light(LightHandle* outLight);
	static void set_light_position(LightHandle light, float4 newPosition);
	static void set_light_color(LightHandle light, float4 newColor);
	static void dispose_light(LightHandle light);
};