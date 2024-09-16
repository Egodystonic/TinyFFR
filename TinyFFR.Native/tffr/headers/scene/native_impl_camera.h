#pragma once

#include "utils_and_constants.h"
#include "filament/filament/Camera.h"

using namespace filament;
using namespace filament::math;

typedef Camera* CameraHandle;

class native_impl_camera {
public:
	static void allocate_camera(CameraHandle* outCamera);
	static void set_camera_fov(CameraHandle camera, float_t newFovDegrees);
	static void get_camera_fov(CameraHandle camera, float_t* outFovDegrees);
	static void set_camera_location(CameraHandle camera, float3 newLocation);
	static void get_camera_location(CameraHandle camera, float3* outLocation);
	static void set_camera_direction(CameraHandle camera, float3 newForwardDir, float3 newUpDir);
	static void get_camera_direction(CameraHandle camera, float3* outForwardDir, float3* outUpDir);
	static void dispose_camera(CameraHandle camera);
};