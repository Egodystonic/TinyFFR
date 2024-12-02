#include "pch.h"
#include "scene/camera/native_impl_camera.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"

using namespace utils;

void native_impl_camera::allocate_camera(CameraHandle* outCamera) {
	ThrowIfNull(outCamera, "Camera out pointer was null.");
	auto entity = filament_engine->getEntityManager().create();
	*outCamera = filament_engine->createCamera(entity);

	ThrowIfNull(*outCamera, "Could not create camera.");
}
StartExportedFunc(allocate_camera, CameraHandle* outCamera) {
	native_impl_camera::allocate_camera(outCamera);
	EndExportedFunc
}



void native_impl_camera::set_camera_projection_matrix(CameraHandle camera, mat4f* newMatrixPtr, float_t nearPlaneDist, float_t farPlaneDist) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(newMatrixPtr, "Matrix was null.");
	camera->setCustomProjection(static_cast<mat4>(*newMatrixPtr), static_cast<double>(nearPlaneDist), static_cast<double>(farPlaneDist));
}
StartExportedFunc(set_camera_projection_matrix, CameraHandle camera, mat4f* newMatrixPtr, float_t nearPlaneDist, float_t farPlaneDist) {
	native_impl_camera::set_camera_projection_matrix(camera, newMatrixPtr, nearPlaneDist, farPlaneDist);
	EndExportedFunc
}

void native_impl_camera::get_camera_projection_matrix(CameraHandle camera, mat4f* outMatrix, float_t* outNearPlaneDist, float_t* outFarPlaneDist) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(outMatrix, "Matrix out pointer was null.");
	ThrowIfNull(outNearPlaneDist, "Near plane out pointer was null.");
	ThrowIfNull(outFarPlaneDist, "Far plane out pointer was null.");
	*outMatrix = static_cast<mat4f>(camera->getProjectionMatrix());
	*outNearPlaneDist = static_cast<float_t>(camera->getNear());
	*outFarPlaneDist = static_cast<float_t>(camera->getCullingFar());
}
StartExportedFunc(get_camera_projection_matrix, CameraHandle camera, mat4f* outMatrix, float_t* outNearPlaneDist, float_t* outFarPlaneDist) {
	native_impl_camera::get_camera_projection_matrix(camera, outMatrix, outNearPlaneDist, outFarPlaneDist);
	EndExportedFunc
}


void native_impl_camera::set_camera_model_matrix(CameraHandle camera, mat4f* newMatrixPtr) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(newMatrixPtr, "Matrix was null.");
	camera->setModelMatrix(*newMatrixPtr);
}
StartExportedFunc(set_camera_model_matrix, CameraHandle camera, mat4f* newMatrixPtr) {
	native_impl_camera::set_camera_model_matrix(camera, newMatrixPtr);
	EndExportedFunc
}

void native_impl_camera::get_camera_model_matrix(CameraHandle camera, mat4f* outMatrix) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(outMatrix, "Matrix out pointer was null.");
	*outMatrix = static_cast<mat4f>(camera->getModelMatrix());
}
StartExportedFunc(get_camera_model_matrix, CameraHandle camera, mat4f* outMatrix) {
	native_impl_camera::get_camera_model_matrix(camera, outMatrix);
	EndExportedFunc
}


void native_impl_camera::set_camera_view_matrix(CameraHandle camera, mat4f* newMatrixPtr) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(newMatrixPtr, "Matrix was null.");
	camera->setModelMatrix(inverse(*newMatrixPtr));
}
StartExportedFunc(set_camera_view_matrix, CameraHandle camera, mat4f* newMatrixPtr) {
	native_impl_camera::set_camera_view_matrix(camera, newMatrixPtr);
	EndExportedFunc
}

void native_impl_camera::get_camera_view_matrix(CameraHandle camera, mat4f* outMatrix) {
	ThrowIfNull(camera, "Camera was null.");
	ThrowIfNull(outMatrix, "Matrix out pointer was null.");
	*outMatrix = static_cast<mat4f>(camera->getViewMatrix());
}
StartExportedFunc(get_camera_view_matrix, CameraHandle camera, mat4f* outMatrix) {
	native_impl_camera::get_camera_view_matrix(camera, outMatrix);
	EndExportedFunc
}


void native_impl_camera::dispose_camera(CameraHandle camera) {
	ThrowIfNull(camera, "Camera was null.");
	auto entity = camera->getEntity();
	filament_engine->destroyCameraComponent(entity);
	filament_engine->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_camera, CameraHandle camera) {
	native_impl_camera::dispose_camera(camera);
	EndExportedFunc
}