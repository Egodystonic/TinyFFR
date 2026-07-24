// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	// Ledger of all camera-locked instances so we can 
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedSet<ModelInstance>> _cameraLockedInstancesCanary = new();
	// Plane-trivial: FaceCameraPlane + no upright + centre anchor + Standard scaling. Needs only the shared plane rotation per frame,
	// so it stores just the ModelInstance rather than the whole camera-locked instance struct.
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedSet<ModelInstance>> _planeTrivialQuadInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedSet<ModelInstance>> _planeTrivialTextInstanceMap = new();
	// Plane-scaled: FaceCameraPlane + no upright + centre anchor + screen-scaled. Shared rotation, but per-instance derived scale.
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedQuadInstance>> _planeScaledQuadInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedTextInstance>> _planeScaledTextInstanceMap = new();
	// General: everything else (all FaceCameraPosition; plus FaceCameraPlane with upright and/or offset anchor). Full per-instance billboard.
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedQuadInstance>> _generalQuadInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedTextInstance>> _generalTextInstanceMap = new();
	readonly MapPool<ResourceHandle<ModelInstance>, CameraLockedQuadInstance> _camLockedQuadMapPool;
	readonly MapPool<ResourceHandle<ModelInstance>, CameraLockedTextInstance> _camLockedTextMapPool;
	
	readonly record struct ScreenScalingContext(
		CameraProjectionType ProjectionType,
		float HalfHorizontalFovTangent,
		float HalfVerticalFovTangent,
		float OrthographicHeight,
		float AspectRatio,
		Location CameraPosition,
		Direction CameraViewDirection
	);
	
	static bool InstanceUsesSharedPlaneRotation(CameraLockStyle lockStyle, Direction lockedUprightDirection, Orientation2D positionAnchor)
		=> lockStyle == CameraLockStyle.FaceCameraPlane && lockedUprightDirection == Direction.None && positionAnchor == Orientation2D.None;

	public void Add(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var modelInstance = quad.UnderlyingQuadInstance.UnderlyingModelInstance;
		bool added;
		if (InstanceUsesSharedPlaneRotation(quad.LockStyle, quad.LockedUprightDirection, quad.PositionAnchor)) {
			added = quad.ScalingMode == CameraLockedScalingMode.Standard
				? _planeTrivialQuadInstanceMap[handle].Add(modelInstance)
				: _planeScaledQuadInstanceMap[handle].TryAdd(modelInstance.Handle, quad);
		}
		else {
			added = _generalQuadInstanceMap[handle].TryAdd(modelInstance.Handle, quad);
		}
		if (added) {
			_cameraLockedInstancesCanary[handle].Add(modelInstance);
			Add(handle, modelInstance);
		}
	}
	public void Add(ResourceHandle<Scene> handle, CameraLockedTextInstance text) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var modelInstance = text.UnderlyingTextInstance.UnderlyingModelInstance;
		bool added;
		if (InstanceUsesSharedPlaneRotation(text.LockStyle, text.LockedUprightDirection, text.PositionAnchor)) {
			added = text.ScalingMode == CameraLockedScalingMode.Standard
				? _planeTrivialTextInstanceMap[handle].Add(modelInstance)
				: _planeScaledTextInstanceMap[handle].TryAdd(modelInstance.Handle, text);
		}
		else {
			added = _generalTextInstanceMap[handle].TryAdd(modelInstance.Handle, text);
		}
		if (added) {
			_cameraLockedInstancesCanary[handle].Add(modelInstance);
			Add(handle, modelInstance);
		}
	}
	
	public void Remove(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad) {
		Remove(handle, quad.UnderlyingQuadInstance.UnderlyingModelInstance);
	}
	public void Remove(ResourceHandle<Scene> handle, CameraLockedTextInstance text) {
		Remove(handle, text.UnderlyingTextInstance.UnderlyingModelInstance);
	}
	void RemoveInstanceFromCameraLockedMaps(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		if (!_cameraLockedInstancesCanary[handle].Remove(modelInstance)) return;
		
		var miHandle = modelInstance.Handle;
		_planeTrivialQuadInstanceMap[handle].Remove(modelInstance);
		_planeTrivialTextInstanceMap[handle].Remove(modelInstance);
		_planeScaledQuadInstanceMap[handle].Remove(miHandle);
		_planeScaledTextInstanceMap[handle].Remove(miHandle);
		_generalQuadInstanceMap[handle].Remove(miHandle);
		_generalTextInstanceMap[handle].Remove(miHandle);
	}
	
	public void PrepareCameraLockedObjectsForRender(ResourceHandle<Scene> handle, Camera targetCamera) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var cameraPosition = targetCamera.Position;
		var cameraViewDirection = targetCamera.ViewDirection;
		var cameraUpDirection = targetCamera.UpDirection;
		var planeFacingDirection = -cameraViewDirection;
		var planeRotationQuat = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, planeFacingDirection, cameraUpDirection).ToQuaternion();

		var screenScaling = new ScreenScalingContext(
			targetCamera.ProjectionType,
			MathF.Tan(targetCamera.HorizontalFieldOfView.Radians * 0.5f),
			MathF.Tan(targetCamera.VerticalFieldOfView.Radians * 0.5f),
			targetCamera.OrthographicHeight,
			targetCamera.AspectRatio,
			cameraPosition,
			cameraViewDirection
		);

		// Plane-trivial: one shared rotation, nothing else per instance.
		foreach (var modelInstance in _planeTrivialQuadInstanceMap[handle]) {
			modelInstance.SetRotationQuaternion(planeRotationQuat);
		}
		foreach (var modelInstance in _planeTrivialTextInstanceMap[handle]) {
			modelInstance.SetRotationQuaternion(planeRotationQuat);
		}

		// Plane-scaled: shared rotation + per-instance derived world scale, pushed as a world matrix so the stored fraction is untouched.
		foreach (var quad in _planeScaledQuadInstanceMap[handle].Values) {
			var storedTransform = quad.UnderlyingQuadInstance.Transform;
			var worldScaling = CalculateWorldScaling(quad.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var worldMatrix = new Transform(storedTransform.Translation, planeRotationQuat, worldScaling).ToMatrix();
			quad.UnderlyingQuadInstance.UnderlyingModelInstance.SetWorldMatrixWithoutUpdatingTransform(worldMatrix);
		}
		foreach (var text in _planeScaledTextInstanceMap[handle].Values) {
			var storedTransform = text.UnderlyingTextInstance.Transform;
			var worldScaling = CalculateWorldScaling(text.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var worldMatrix = new Transform(storedTransform.Translation, planeRotationQuat, worldScaling).ToMatrix();
			text.UnderlyingTextInstance.UnderlyingModelInstance.SetWorldMatrixWithoutUpdatingTransform(worldMatrix);
		}

		// General: full per-instance billboard, honouring the instance's CameraLockStyle and locked upright.
		foreach (var quad in _generalQuadInstanceMap[handle].Values) {
			var storedTransform = quad.UnderlyingQuadInstance.Transform;
			var worldScaling = CalculateWorldScaling(quad.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var billboard = storedTransform with { Scaling = worldScaling };
			var anchorOffset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(new XYPair<float>(worldScaling.X, worldScaling.Y), quad.PositionAnchor);
			ApplyGeneralCameraLockedFacing(ref billboard, anchorOffset, quad.LockStyle, quad.LockedUprightDirection, cameraPosition, cameraUpDirection, planeFacingDirection);
			CommitCameraLockedTransform(quad.UnderlyingQuadInstance.UnderlyingModelInstance, in billboard, quad.ScalingMode, storedTransform.Scaling);
		}
		foreach (var text in _generalTextInstanceMap[handle].Values) {
			var storedTransform = text.UnderlyingTextInstance.Transform;
			var @string = text.UnderlyingTextInstance.String;
			var worldScaling = CalculateWorldScaling(text.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var billboard = storedTransform with { Scaling = worldScaling };
			var anchorOffset = @string.Font.GetTextInstanceAnchorOffset(@string.Size, new XYPair<float>(worldScaling.X, worldScaling.Y), text.PositionAnchor);
			ApplyGeneralCameraLockedFacing(ref billboard, anchorOffset, text.LockStyle, text.LockedUprightDirection, cameraPosition, cameraUpDirection, planeFacingDirection);
			CommitCameraLockedTransform(text.UnderlyingTextInstance.UnderlyingModelInstance, in billboard, text.ScalingMode, storedTransform.Scaling);
		}
	}

	static void ApplyGeneralCameraLockedFacing(ref Transform billboard, Vect anchorOffset, CameraLockStyle lockStyle, Direction lockedUprightDirection, Location cameraPosition, Direction cameraUpDirection, Direction planeFacingDirection) {
		if (lockStyle == CameraLockStyle.FaceCameraPlane) {
			if (lockedUprightDirection == Direction.None) GetPlanarSphericalCameraLockedTransform(ref billboard, anchorOffset, planeFacingDirection, cameraUpDirection);
			else GetPlanarCylindricalCameraLockedTransform(ref billboard, anchorOffset, planeFacingDirection, lockedUprightDirection);
		}
		else {
			if (lockedUprightDirection == Direction.None) GetSphericalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, cameraUpDirection);
			else GetCylindricalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, lockedUprightDirection);
		}
	}
	
	static Transform BuildCameraLockedTransform(Location anchorPosition, Vect meshSpaceAnchorOffset, Vect scaling, Direction facingDirection, Direction upDirection) {
		var localX = Direction.FastFromDualOrthogonalization(facingDirection, upDirection).ToVector3();
		var localY = upDirection.ToVector3();
		var localZ = -facingDirection.ToVector3();

		var rotationQuaternion = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
			localX.X, localX.Y, localX.Z, 0f,
			localY.X, localY.Y, localY.Z, 0f,
			localZ.X, localZ.Y, localZ.Z, 0f,
			0f, 0f, 0f, 1f
		));

		var rotatedAnchorOffset = localX * meshSpaceAnchorOffset.X + localY * meshSpaceAnchorOffset.Y + localZ * meshSpaceAnchorOffset.Z;
		return new Transform(
			anchorPosition.AsVect() + Vect.FromVector3(rotatedAnchorOffset),
			rotationQuaternion,
			scaling
		);
	}

	static void GetSphericalCameraLockedTransform(ref Transform transform, Vect meshSpaceAnchorOffset, Location cameraPosition, Direction cameraUpDirection) {
		var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		var facingDirection = anchorPosition.DirectionTo(cameraPosition);
		if (facingDirection == Direction.None) return;

		var upDirection = cameraUpDirection.OrthogonalizedAgainst(facingDirection) ?? facingDirection.AnyOrthogonal();
		transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection, upDirection);
	}

	static void GetCylindricalCameraLockedTransform(ref Transform transform, Vect meshSpaceAnchorOffset, Location cameraPosition, Direction lockedUpDirection) {
		var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		var facingDirection = anchorPosition.DirectionTo(cameraPosition).OrthogonalizedAgainst(lockedUpDirection);
		if (facingDirection == Direction.None || facingDirection == null) return;

		transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection.Value, lockedUpDirection);
	}

	static void GetPlanarSphericalCameraLockedTransform(ref Transform transform, Vect meshSpaceAnchorOffset, Direction planeFacingDirection, Direction cameraUpDirection) {
		var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		var upDirection = cameraUpDirection.OrthogonalizedAgainst(planeFacingDirection) ?? planeFacingDirection.AnyOrthogonal();
		transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, planeFacingDirection, upDirection);
	}

	static void GetPlanarCylindricalCameraLockedTransform(ref Transform transform, Vect meshSpaceAnchorOffset, Direction planeFacingDirection, Direction lockedUpDirection) {
		var facingDirection = planeFacingDirection.OrthogonalizedAgainst(lockedUpDirection);
		if (facingDirection == Direction.None || facingDirection == null) return;

		var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection.Value, lockedUpDirection);
	}

	static Vect CalculateWorldScaling(CameraLockedScalingMode mode, Vect storedScaling, Vect translation, in ScreenScalingContext scalingContext) {
		if (mode == CameraLockedScalingMode.Standard) return storedScaling;

		float screenW, screenH;
		if (scalingContext.ProjectionType == CameraProjectionType.Orthographic) {
			screenW = scalingContext.OrthographicHeight * scalingContext.AspectRatio;
			screenH = scalingContext.OrthographicHeight;
		}
		else {
			var doubleDepth = MathF.Max((translation.AsLocation() - scalingContext.CameraPosition).Dot(scalingContext.CameraViewDirection), 0f) * 2f;
			screenW = doubleDepth * scalingContext.HalfHorizontalFovTangent;
			screenH = doubleDepth * scalingContext.HalfVerticalFovTangent;
		}

		return mode switch {
			CameraLockedScalingMode.ViewportFractionalFixedWidth => new Vect(storedScaling.X * screenW, storedScaling.Y, storedScaling.Z),
			CameraLockedScalingMode.ViewportFractionalFixedHeight => new Vect(storedScaling.X, storedScaling.Y * screenH, storedScaling.Z),
			CameraLockedScalingMode.ViewportFractionalFixedWidthAndHeight => new Vect(storedScaling.X * screenW, storedScaling.Y * screenH, storedScaling.Z),
			CameraLockedScalingMode.ViewportFractionalFixedWidthPlusPreservedAspectRatio => new Vect(storedScaling.X * screenW, storedScaling.Y * screenW, storedScaling.Z),
			CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio => new Vect(storedScaling.X * screenH, storedScaling.Y * screenH, storedScaling.Z),
			_ => storedScaling
		};
	}

	static void CommitCameraLockedTransform(ModelInstance modelInstance, in Transform billboard, CameraLockedScalingMode mode, Vect storedScaling) {
		if (mode == CameraLockedScalingMode.Standard) {
			modelInstance.SetTransform(billboard);
		}
		else {
			modelInstance.SetWorldMatrixWithoutUpdatingTransform(billboard.ToMatrix());
			modelInstance.SetTransformWithoutUpdatingWorldMatrix(billboard with { Scaling = storedScaling });
		}
	}
}
