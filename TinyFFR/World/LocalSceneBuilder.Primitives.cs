// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedQuadInstance>> _camLockedQuadInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedTextInstance>> _camLockedTextInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedQuadInstance>> _fastCamLockedQuadInstanceMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CameraLockedTextInstance>> _fastCamLockedTextInstanceMap = new();
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
	
	public void PrepareCameraLockedObjectsForRender(ResourceHandle<Scene> handle, Camera targetCamera) {
		ThrowIfThisOrHandleIsDisposed(handle);	

		var cameraPosition = targetCamera.Position;
		var cameraViewDirection = targetCamera.ViewDirection;
		var cameraUpDirection = targetCamera.UpDirection;

		var fastRotationQuat = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, -cameraViewDirection, cameraUpDirection).ToQuaternion();
		foreach (var quad in _fastCamLockedQuadInstanceMap[handle].Values) {
			quad.UnderlyingQuadInstance.UnderlyingModelInstance.SetRotationQuaternion(fastRotationQuat);
		}
		foreach (var text in _fastCamLockedTextInstanceMap[handle].Values) {
			text.UnderlyingTextInstance.UnderlyingModelInstance.SetRotationQuaternion(fastRotationQuat);
		}

		var screenScaling = new ScreenScalingContext(
			targetCamera.ProjectionType,
			MathF.Tan(targetCamera.HorizontalFieldOfView.Radians * 0.5f),
			MathF.Tan(targetCamera.VerticalFieldOfView.Radians * 0.5f),
			targetCamera.OrthographicHeight,
			targetCamera.AspectRatio,
			cameraPosition,
			cameraViewDirection
		);

		foreach (var quad in _camLockedQuadInstanceMap[handle].Values) {
			var storedTransform = quad.UnderlyingQuadInstance.Transform;
			var worldScaling = CalculateWorldScaling(quad.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var billboard = storedTransform with { Scaling = worldScaling };
			var anchorOffset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(new XYPair<float>(worldScaling.X, worldScaling.Y), quad.PositionAnchor);
			if (quad.LockedUprightDirection == Direction.None) {
				GetSphericalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, cameraUpDirection);
			}
			else {
				GetCylindricalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, quad.LockedUprightDirection);
			}
			CommitCameraLockedTransform(quad.UnderlyingQuadInstance.UnderlyingModelInstance, in billboard, quad.ScalingMode, storedTransform.Scaling);
		}
		foreach (var text in _camLockedTextInstanceMap[handle].Values) {
			var storedTransform = text.UnderlyingTextInstance.Transform;
			var @string = text.UnderlyingTextInstance.String;
			var worldScaling = CalculateWorldScaling(text.ScalingMode, storedTransform.Scaling, storedTransform.Translation, in screenScaling);
			var billboard = storedTransform with { Scaling = worldScaling };
			var anchorOffset = @string.Font.GetTextInstanceAnchorOffset(@string.Size, new XYPair<float>(worldScaling.X, worldScaling.Y), text.PositionAnchor);
			if (text.LockedUprightDirection == Direction.None) {
				GetSphericalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, cameraUpDirection);
			}
			else {
				GetCylindricalCameraLockedTransform(ref billboard, anchorOffset, cameraPosition, text.LockedUprightDirection);
			}
			CommitCameraLockedTransform(text.UnderlyingTextInstance.UnderlyingModelInstance, in billboard, text.ScalingMode, storedTransform.Scaling);
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
