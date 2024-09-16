#include "pch.h"
#include "scene/native_impl_camera.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/utils/Entity.h"
#include "filament/utils/EntityManager.h"

using namespace utils;

void native_impl_camera::allocate_camera(CameraHandle* outCamera) {
	auto entity = native_impl_init::filament_engine_ptr->getEntityManager().create();
	*outCamera = native_impl_init::filament_engine_ptr->createCamera(entity);

	ThrowIfNull(*outCamera, "Could not create camera.");
}
StartExportedFunc(allocate_camera, CameraHandle* outCamera) {
	native_impl_camera::allocate_camera(outCamera);
	EndExportedFunc
}

void native_impl_camera::get_camera_fov(CameraHandle camera, float_t* outFovDegrees) {
	ThrowIfNull(camera, "Camera handle was null.");
	*outFovDegrees = camera->getFieldOfViewInDegrees(Camera::Fov::VERTICAL);
}
StartExportedFunc(get_camera_fov, CameraHandle camera, float_t* outFovDegrees) {
	native_impl_camera::get_camera_fov(camera, outFovDegrees);
	EndExportedFunc
}

void native_impl_camera::dispose_camera(CameraHandle camera) {
	ThrowIfNull(camera, "Camera handle was null.");
	auto entity = camera->getEntity();
	native_impl_init::filament_engine_ptr->destroyCameraComponent(entity);
	native_impl_init::filament_engine_ptr->getEntityManager().destroy(entity);
}
StartExportedFunc(dispose_camera, CameraHandle camera) {
	native_impl_camera::dispose_camera(camera);
	EndExportedFunc
}