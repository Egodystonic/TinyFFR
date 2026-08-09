// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	readonly record struct FullCameraLockedInstanceData(
		CameraLockedScalingMode ScalingMode,
		CameraLockStyle LockStyle,
		Direction LockedUprightDirection,
		Orientation2D PositionAnchor,
		QuadInstance? Quad,
		TextInstance? Text
	) {
		public ModelInstance ModelInstance => Quad?.UnderlyingModelInstance ?? Text!.Value.UnderlyingModelInstance;
		
		public Vect GetGeneralAnchorOffset(Vect worldScaling) {
			var size = new XYPair<float>(worldScaling.X, worldScaling.Y);
			if (Text is not { } text) return QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(size, PositionAnchor);
			
			var @string = text.String;
			return @string.Font.GetTextInstanceAnchorOffset(@string.Size, size, PositionAnchor);
		}
	}
	
	readonly record struct AbridgedCameraLockedInstanceData(CameraLockedScalingMode ScalingMode, ModelInstance ModelInstance);

	// Ledger of all camera-locked instances so we can skip the per-bucket lookups in Remove when an instance was never camera-locked.
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedSet<ModelInstance>> _cameraLockedInstancesLedger = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedSet<ModelInstance>> _camLockedTrivialInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, AbridgedCameraLockedInstanceData>> _camLockedAbridgedInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, FullCameraLockedInstanceData>> _camLockedFullInstanceMap = new();
	readonly MapPool<ResourceHandle<ModelInstance>, AbridgedCameraLockedInstanceData> _camLockedAbridgedInstanceMapPool;
	readonly MapPool<ResourceHandle<ModelInstance>, FullCameraLockedInstanceData> _camLockedFullInstanceMapPool;

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
		AddCameraLockedInstance(handle, new FullCameraLockedInstanceData(quad.ScalingMode, quad.LockStyle, quad.LockedUprightDirection, quad.PositionAnchor, quad.UnderlyingQuadInstance, null));
	}
	public void Add(ResourceHandle<Scene> handle, CameraLockedTextInstance text) {
		ThrowIfThisOrHandleIsDisposed(handle);
		AddCameraLockedInstance(handle, new FullCameraLockedInstanceData(text.ScalingMode, text.LockStyle, text.LockedUprightDirection, text.PositionAnchor, null, text.UnderlyingTextInstance));
	}
	void AddCameraLockedInstance(ResourceHandle<Scene> handle, in FullCameraLockedInstanceData data) {
		var modelInstance = data.ModelInstance;
		bool added;
		if (InstanceUsesSharedPlaneRotation(data.LockStyle, data.LockedUprightDirection, data.PositionAnchor)) {
			added = data.ScalingMode == CameraLockedScalingMode.Standard
				? _camLockedTrivialInstanceMap[handle].Add(modelInstance)
				: _camLockedAbridgedInstanceMap[handle].TryAdd(modelInstance.Handle, new(data.ScalingMode, data.ModelInstance));
		}
		else {
			added = _camLockedFullInstanceMap[handle].TryAdd(modelInstance.Handle, data);
		}
		if (added) {
			_cameraLockedInstancesLedger[handle].Add(modelInstance);
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
		if (!_cameraLockedInstancesLedger[handle].Remove(modelInstance)) return;

		var miHandle = modelInstance.Handle;
		_camLockedTrivialInstanceMap[handle].Remove(modelInstance);
		_camLockedAbridgedInstanceMap[handle].Remove(miHandle);
		_camLockedFullInstanceMap[handle].Remove(miHandle);
	}
	
	public void PrepareCameraSensitiveObjectsForRender(ResourceHandle<Scene> handle, Camera targetCamera) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		PrepareCameraSensitivePrimitivesForRender(handle, targetCamera);

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

		foreach (var modelInstance in _camLockedTrivialInstanceMap[handle]) {
			modelInstance.SetRotationQuaternion(planeRotationQuat);
		}

		foreach (var inst in _camLockedAbridgedInstanceMap[handle].Values) {
			var modelInstance = inst.ModelInstance;
			var storedTransform = modelInstance.Transform;
			var worldScaling = CalculateWorldScaling(inst.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var worldMatrix = new Transform(storedTransform.Translation, planeRotationQuat, worldScaling).ToMatrix();
			modelInstance.SetWorldMatrixWithoutUpdatingTransform(worldMatrix);
		}

		foreach (var inst in _camLockedFullInstanceMap[handle].Values) {
			var modelInstance = inst.ModelInstance;
			var storedTransform = modelInstance.Transform;
			var worldScaling = CalculateWorldScaling(inst.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var billboard = storedTransform with { Scaling = worldScaling };
			var anchorOffset = inst.GetGeneralAnchorOffset(worldScaling);
			ApplyGeneralCameraLockedFacing(ref billboard, anchorOffset, inst.LockStyle, inst.LockedUprightDirection, cameraPosition, cameraUpDirection, planeFacingDirection);
			CommitCameraLockedTransform(modelInstance, in billboard, inst.ScalingMode, storedTransform.Scaling);
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
		// var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		// // When the locked axis points ~straight at/away from the camera the facing can not be resolved; fall back to
		// // any valid facing rather than leaving a stale (edge-on) transform frozen from a previous frame.
		// var facingOrNull = anchorPosition.DirectionTo(cameraPosition).OrthogonalizedAgainst(lockedUpDirection);
		// var facingDirection = facingOrNull == null || facingOrNull == Direction.None ? lockedUpDirection.AnyOrthogonal() : facingOrNull.Value;
		//
		// transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection, lockedUpDirection);
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
		// // When the locked axis points ~straight at/away from the camera the facing can not be resolved. Rather than
		// // leaving a stale transform (which freezes the quad edge-on), fall back to any valid facing: the quad stays on
		// // its axis and foreshortens to a point naturally under projection.
		// var facingOrNull = planeFacingDirection.OrthogonalizedAgainst(lockedUpDirection);
		// var facingDirection = facingOrNull == null || facingOrNull == Direction.None ? lockedUpDirection.AnyOrthogonal() : facingOrNull.Value;
		//
		// var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		// transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection, lockedUpDirection);
		
		var facingDirection = planeFacingDirection.OrthogonalizedAgainst(lockedUpDirection);
		if (facingDirection == Direction.None || facingDirection == null) return;
		var anchorPosition = (transform.Translation - Rotation.Rotate(meshSpaceAnchorOffset, transform.RotationQuaternion)).AsLocation();
		transform = BuildCameraLockedTransform(anchorPosition, meshSpaceAnchorOffset, transform.Scaling, facingDirection.Value, lockedUpDirection);
	}

	static Vect CalculateWorldScaling(CameraLockedScalingMode mode, Vect storedScaling, Vect translation, in ScreenScalingContext scalingContext) {
		if (mode == CameraLockedScalingMode.Standard) return storedScaling;

		var (screenW, screenH) = scalingContext.ProjectionType == CameraProjectionType.Orthographic
			? CameraUtils.CalculateOrthographicViewportWorldSize(scalingContext.OrthographicHeight, scalingContext.AspectRatio)
			: CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistanceFromFovTangents(scalingContext.HalfHorizontalFovTangent, scalingContext.HalfVerticalFovTangent, (translation.AsLocation() - scalingContext.CameraPosition).Dot(scalingContext.CameraViewDirection));

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
	
	void DisposeAllCameraLockedData(ResourceHandle<Scene> handle) {
		_camLockedTrivialInstanceMap.Remove(handle);
		_camLockedAbridgedInstanceMapPool.Return(_camLockedAbridgedInstanceMap[handle]);
		_camLockedAbridgedInstanceMap.Remove(handle);
		_camLockedFullInstanceMapPool.Return(_camLockedFullInstanceMap[handle]);
		_camLockedFullInstanceMap.Remove(handle);
		_modelInstanceSetPool.Return(_cameraLockedInstancesLedger[handle]);
		_cameraLockedInstancesLedger.Remove(handle);
	}
	
	void DisposeAllCameraLockedResources() {
		_camLockedTrivialInstanceMap.Dispose();
		_camLockedAbridgedInstanceMap.Dispose();
		_camLockedFullInstanceMap.Dispose();
		_cameraLockedInstancesLedger.Dispose();
		_camLockedAbridgedInstanceMapPool.Dispose();
		_camLockedFullInstanceMapPool.Dispose();
	}
}
