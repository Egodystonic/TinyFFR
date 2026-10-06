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
			var storedAnchorOffset = inst.GetGeneralAnchorOffset(storedTransform.Scaling);
			var worldAnchorOffset = inst.ScalingMode == CameraLockedScalingMode.Standard ? storedAnchorOffset : inst.GetGeneralAnchorOffset(worldScaling);
			CalculateCameraLockedTransforms(
				in storedTransform,
				worldScaling,
				storedAnchorOffset,
				worldAnchorOffset,
				inst.LockStyle,
				inst.LockedUprightDirection,
				cameraPosition,
				cameraUpDirection,
				planeFacingDirection,
				out var newStoredTransform,
				out var worldTransform
			);
			CommitCameraLockedTransform(modelInstance, in newStoredTransform, in worldTransform, inst.ScalingMode);
		}
	}

	internal static void CalculateCameraLockedTransforms(in Transform storedTransform, Vect worldScaling, Vect storedAnchorOffset, Vect worldAnchorOffset, CameraLockStyle lockStyle, Direction lockedUprightDirection, Location cameraPosition, Direction cameraUpDirection, Direction planeFacingDirection, out Transform newStoredTransform, out Transform worldTransform) {
		var anchorPosition = (storedTransform.Translation - Rotation.Rotate(storedAnchorOffset, storedTransform.RotationQuaternion)).AsLocation();
		var rotationQuaternion = TryCalculateCameraLockedFacing(anchorPosition, lockStyle, lockedUprightDirection, cameraPosition, cameraUpDirection, planeFacingDirection, out var facingDirection, out var upDirection)
			? CalculateCameraLockedRotation(facingDirection, upDirection)
			: storedTransform.RotationQuaternion;
		newStoredTransform = new Transform(anchorPosition.AsVect() + Rotation.Rotate(storedAnchorOffset, rotationQuaternion), rotationQuaternion, storedTransform.Scaling);
		worldTransform = new Transform(anchorPosition.AsVect() + Rotation.Rotate(worldAnchorOffset, rotationQuaternion), rotationQuaternion, worldScaling);
	}

	static bool TryCalculateCameraLockedFacing(Location anchorPosition, CameraLockStyle lockStyle, Direction lockedUprightDirection, Location cameraPosition, Direction cameraUpDirection, Direction planeFacingDirection, out Direction facingDirection, out Direction upDirection) {
		Direction? facingOrNull = lockStyle == CameraLockStyle.FaceCameraPlane ? planeFacingDirection : anchorPosition.DirectionTo(cameraPosition);
		if (lockedUprightDirection != Direction.None) facingOrNull = facingOrNull.Value.OrthogonalizedAgainst(lockedUprightDirection);
		if (facingOrNull is not { } facing || facing == Direction.None) {
			facingDirection = Direction.None;
			upDirection = Direction.None;
			return false;
		}
		facingDirection = facing;
		upDirection = lockedUprightDirection != Direction.None
			? lockedUprightDirection
			: cameraUpDirection.OrthogonalizedAgainst(facing) ?? facing.AnyOrthogonal();
		return true;
	}
	
	static Quaternion CalculateCameraLockedRotation(Direction facingDirection, Direction upDirection) {
		var localX = Direction.FastFromDualOrthogonalization(facingDirection, upDirection).ToVector3();
		var localY = upDirection.ToVector3();
		var localZ = -facingDirection.ToVector3();

		return Quaternion.CreateFromRotationMatrix(new Matrix4x4(
			localX.X, localX.Y, localX.Z, 0f,
			localY.X, localY.Y, localY.Z, 0f,
			localZ.X, localZ.Y, localZ.Z, 0f,
			0f, 0f, 0f, 1f
		));
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

	static void CommitCameraLockedTransform(ModelInstance modelInstance, in Transform newStoredTransform, in Transform worldTransform, CameraLockedScalingMode mode) {
		if (mode == CameraLockedScalingMode.Standard) {
			modelInstance.SetTransform(newStoredTransform);
		}
		else {
			modelInstance.SetWorldMatrixWithoutUpdatingTransform(worldTransform.ToMatrix());
			modelInstance.SetTransformWithoutUpdatingWorldMatrix(newStoredTransform);
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
