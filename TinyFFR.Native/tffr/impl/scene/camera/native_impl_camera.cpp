#include "pch.h"
#include "scene/camera/native_impl_camera.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"

using namespace utils;

void native_impl_camera::allocate_camera(CameraHandle* outCamera) {
	auto entity = filament_engine->getEntityManager().create();
	*outCamera = filament_engine->createCamera(entity);

	ThrowIfNull(*outCamera, "Could not create camera.");
}
StartExportedFunc(allocate_camera, CameraHandle* outCamera) {
	native_impl_camera::allocate_camera(outCamera);
	EndExportedFunc
}



void native_impl_camera::set_camera_projection_matrix(CameraHandle camera, mat4f* newMatrixPtr, float_t nearPlaneDist, float_t farPlaneDist) {
	//camera->setCustomProjection(static_cast<mat4>(*newMatrixPtr), static_cast<double>(nearPlaneDist), static_cast<double>(farPlaneDist));
	camera->setProjection(90.0, 1.777, 0.001, 1000);
}
StartExportedFunc(set_camera_projection_matrix, CameraHandle camera, mat4f* newMatrixPtr, float_t nearPlaneDist, float_t farPlaneDist) {
	native_impl_camera::set_camera_projection_matrix(camera, newMatrixPtr, nearPlaneDist, farPlaneDist);
	EndExportedFunc
}

void native_impl_camera::get_camera_projection_matrix(CameraHandle camera, mat4f* outMatrix, float_t* outNearPlaneDist, float_t* outFarPlaneDist) {
	*outMatrix = static_cast<mat4f>(camera->getProjectionMatrix());
	*outNearPlaneDist = static_cast<float_t>(camera->getNear());
	*outFarPlaneDist = static_cast<float_t>(camera->getCullingFar());
}
StartExportedFunc(get_camera_projection_matrix, CameraHandle camera, mat4f* outMatrix, float_t* outNearPlaneDist, float_t* outFarPlaneDist) {
	native_impl_camera::get_camera_projection_matrix(camera, outMatrix, outNearPlaneDist, outFarPlaneDist);
	EndExportedFunc
}



void native_impl_camera::set_camera_view_matrix(CameraHandle camera, mat4f* newMatrixPtr) {
	camera->lookAt({ 0.0, 0.0, -100.0 }, { 0.0, 0.0, 1.0 }, { 0.0, 1.0, 0.0 });
	//camera->setModelMatrix(inverse(*newMatrixPtr));
}
StartExportedFunc(set_camera_view_matrix, CameraHandle camera, mat4f* newMatrixPtr) {
	native_impl_camera::set_camera_view_matrix(camera, newMatrixPtr);
	EndExportedFunc
}

void native_impl_camera::get_camera_view_matrix(CameraHandle camera, mat4f* outMatrix) {
	*outMatrix = static_cast<mat4f>(camera->getViewMatrix());
}
StartExportedFunc(get_camera_view_matrix, CameraHandle camera, mat4f* outMatrix) {
	native_impl_camera::get_camera_view_matrix(camera, outMatrix);
	EndExportedFunc
}


void native_impl_camera::dispose_camera(CameraHandle camera) {
	ThrowIfNull(camera, "Camera handle was null.");
	auto entity = camera->getEntity();
	filament_engine->destroyCameraComponent(entity);
	filament_engine->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_camera, CameraHandle camera) {
	native_impl_camera::dispose_camera(camera);
	EndExportedFunc
}