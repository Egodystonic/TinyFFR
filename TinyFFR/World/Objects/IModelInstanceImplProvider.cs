// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface IModelInstanceImplProvider : IDisposableResourceImplProvider<ModelInstance> {
	Transform GetTransform(ResourceHandle<ModelInstance> handle);
	void SetTransform(ResourceHandle<ModelInstance> handle, Transform newTransform);
	void SetTransformWithoutUpdatingWorldMatrix(ResourceHandle<ModelInstance> handle, in Transform newTransform);
	void SetWorldMatrixWithoutUpdatingTransform(ResourceHandle<ModelInstance> handle, in Matrix4x4 worldMatrix);

	Location GetPosition(ResourceHandle<ModelInstance> handle);
	void SetPosition(ResourceHandle<ModelInstance> handle, Location newPosition);

	Rotation GetRotation(ResourceHandle<ModelInstance> handle);
	void SetRotation(ResourceHandle<ModelInstance> handle, Rotation newRotation);

	Quaternion GetRotationQuaternion(ResourceHandle<ModelInstance> handle);
	void SetRotationQuaternion(ResourceHandle<ModelInstance> handle, Quaternion newRotationQuaternion);

	Vect GetScaling(ResourceHandle<ModelInstance> handle);
	void SetScaling(ResourceHandle<ModelInstance> handle, Vect newScaling);

	Material GetMaterial(ResourceHandle<ModelInstance> handle);
	void SetMaterial(ResourceHandle<ModelInstance> handle, Material newMaterial);

	Mesh GetMesh(ResourceHandle<ModelInstance> handle);
	void SetMesh(ResourceHandle<ModelInstance> handle, Mesh newMesh);

	ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<ModelInstance> handle, Range range, bool recalculateBoundingBox);
	ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<ModelInstance> handle);
	void TriggerManualBoundingBoxRecalculation(ResourceHandle<ModelInstance> handle);
	void SetBoundingBox(ResourceHandle<ModelInstance> handle, PositionedCuboid newBoundingBox);
	PositionedCuboid GetBoundingBox(ResourceHandle<ModelInstance> handle);

	void TranslateBy(ResourceHandle<ModelInstance> handle, Vect translation);
	void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation);
	void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation, Location pivotPoint);
	void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion);
	void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion, Location pivotPoint);
	void ScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	void ScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
	
	void SetDefaultMaterialBaseColor(ResourceHandle<ModelInstance> handle, ColorVect newBaseColor);
	void SetDefaultMaterialShadingStyle(ResourceHandle<ModelInstance> handle, DefaultMaterialShadingStyle newStyle);
	void SetKeyedMaterialColor(ResourceHandle<ModelInstance> handle, ColorChannel key, ColorVect color);

	void SetMaterialEffectTransform(ResourceHandle<ModelInstance> handle, Transform2D newTransform);
	void SetMaterialEffectBlendTexture(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, Texture mapTexture);
	void SetMaterialEffectBlendDistance(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, float distance);
	void SetMaterialEffectOpacity(ResourceHandle<ModelInstance> handle, float opacity);

	int? GetDrawOrderDeferralAmount(ResourceHandle<ModelInstance> handle);
	void SetDrawOrderDeferralAmount(ResourceHandle<ModelInstance> handle, int? newValue);

	void SetScissorRect(ResourceHandle<ModelInstance> handle, XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions);
	void ClearScissorRect(ResourceHandle<ModelInstance> handle);
	Material GetOrCreatePrivateMaterial(ResourceHandle<ModelInstance> handle);
	
	void SetTextInstanceInitialPenAndString(ResourceHandle<ModelInstance> handle, FontPen pen, FontString @string, TextLayout layout);
	void SetTextInstanceLayout(ResourceHandle<ModelInstance> handle, TextLayout layout);
	void UpdateTextInstancePen(ResourceHandle<ModelInstance> handle, FontPen pen);
	void UpdateTextInstanceString(ResourceHandle<ModelInstance> handle, FontString @string);
	FontPen GetTextInstancePen(ResourceHandle<ModelInstance> handle);
	FontString GetTextInstanceString(ResourceHandle<ModelInstance> handle);
}