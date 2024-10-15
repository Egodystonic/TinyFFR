#pragma once

#include "utils_and_constants.h"
#include "filament/filament/Camera.h"

using namespace filament;
using namespace filament::math;

typedef Camera* CameraHandle;

class native_impl_camera {
public:
	static void allocate_camera(CameraHandle* outCamera);
	static void set_camera_projection_matrix(CameraHandle camera, mat4f* newMatrixPtr, float_t nearPlaneDist, float_t farPlaneDist);
	static void get_camera_projection_matrix(CameraHandle camera, mat4f* outMatrix, float_t* outNearPlaneDist, float_t* outFarPlaneDist);
	static void set_camera_view_matrix(CameraHandle camera, mat4f* newMatrixPtr);
	static void get_camera_view_matrix(CameraHandle camera, mat4f* outMatrix);
	static void dispose_camera(CameraHandle camera);
};