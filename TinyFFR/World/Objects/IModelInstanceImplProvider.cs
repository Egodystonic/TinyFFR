// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="ModelInstance"/> resources.
/// </summary>
public interface IModelInstanceImplProvider : IDisposableResourceImplProvider<ModelInstance> {
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Transform"/>.
	/// </summary>
	Transform GetTransform(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Transform"/>.
	/// </summary>
	void SetTransform(ResourceHandle<ModelInstance> handle, Transform newTransform);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetTransformWithoutUpdatingWorldMatrix"/>.
	/// </summary>
	void SetTransformWithoutUpdatingWorldMatrix(ResourceHandle<ModelInstance> handle, in Transform newTransform);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetWorldMatrixWithoutUpdatingTransform"/>.
	/// </summary>
	void SetWorldMatrixWithoutUpdatingTransform(ResourceHandle<ModelInstance> handle, in Matrix4x4 worldMatrix);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.Position"/>.
	/// </summary>
	Location GetPosition(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Position"/>.
	/// </summary>
	void SetPosition(ResourceHandle<ModelInstance> handle, Location newPosition);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.Rotation"/>.
	/// </summary>
	Rotation GetRotation(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Rotation"/>.
	/// </summary>
	void SetRotation(ResourceHandle<ModelInstance> handle, Rotation newRotation);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotationQuaternion"/>.
	/// </summary>
	Quaternion GetRotationQuaternion(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotationQuaternion"/>.
	/// </summary>
	void SetRotationQuaternion(ResourceHandle<ModelInstance> handle, Quaternion newRotationQuaternion);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.Scaling"/>.
	/// </summary>
	Vect GetScaling(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Scaling"/>.
	/// </summary>
	void SetScaling(ResourceHandle<ModelInstance> handle, Vect newScaling);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.Material"/>.
	/// </summary>
	Material GetMaterial(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Material"/>.
	/// </summary>
	void SetMaterial(ResourceHandle<ModelInstance> handle, Material newMaterial);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.Mesh"/>.
	/// </summary>
	Mesh GetMesh(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.Mesh"/>.
	/// </summary>
	void SetMesh(ResourceHandle<ModelInstance> handle, Mesh newMesh);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.BorrowVerticesSpan(bool, Range)"/>.
	/// </summary>
	ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<ModelInstance> handle, Range range, bool recalculateBoundingBox);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.BorrowVerticesSpanReadOnly"/>.
	/// </summary>
	ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.TriggerManualBoundingBoxRecalculation"/>.
	/// </summary>
	void TriggerManualBoundingBoxRecalculation(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetModelSpaceBoundingBox"/>.
	/// </summary>
	void SetModelSpaceBoundingBox(ResourceHandle<ModelInstance> handle, PositionedCuboid newBoundingBox);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.GetModelSpaceBoundingBox"/>.
	/// </summary>
	PositionedCuboid GetModelSpaceBoundingBox(ResourceHandle<ModelInstance> handle);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.MoveBy"/>.
	/// </summary>
	void TranslateBy(ResourceHandle<ModelInstance> handle, Vect translation);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotateBy(Rotation)"/>.
	/// </summary>
	void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotateBy(Rotation, Location)"/>.
	/// </summary>
	void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation, Location pivotPoint);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotateBy(Quaternion)"/>.
	/// </summary>
	void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.RotateBy(Quaternion, Location)"/>.
	/// </summary>
	void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion, Location pivotPoint);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.ScaleBy(float)"/>.
	/// </summary>
	void ScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.ScaleBy(Vect)"/>.
	/// </summary>
	void ScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.AdjustScaleBy(float)"/>.
	/// </summary>
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.AdjustScaleBy(Vect)"/>.
	/// </summary>
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
	
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetDefaultMaterialBaseColor"/>.
	/// </summary>
	void SetDefaultMaterialBaseColor(ResourceHandle<ModelInstance> handle, ColorVect newBaseColor);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetDefaultMaterialShadingStyle"/>.
	/// </summary>
	void SetDefaultMaterialShadingStyle(ResourceHandle<ModelInstance> handle, DefaultMaterialShadingStyle newStyle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.SetKeyedMaterialColor"/>.
	/// </summary>
	void SetKeyedMaterialColor(ResourceHandle<ModelInstance> handle, ColorChannel key, ColorVect color);

	/// <summary>
	/// Invoked via <see cref="MaterialEffectController.SetTransform"/>.
	/// </summary>
	void SetMaterialEffectTransform(ResourceHandle<ModelInstance> handle, Transform2D newTransform);
	/// <summary>
	/// Invoked via <see cref="MaterialEffectController.SetBlendTexture"/>.
	/// </summary>
	void SetMaterialEffectBlendTexture(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, Texture mapTexture);
	/// <summary>
	/// Invoked via <see cref="MaterialEffectController.SetBlendDistance"/>.
	/// </summary>
	void SetMaterialEffectBlendDistance(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, float distance);
	/// <summary>
	/// Invoked internally to fade a canvas object in and out; see <see cref="CanvasTexture.Opacity"/>.
	/// </summary>
	void SetMaterialEffectOpacity(ResourceHandle<ModelInstance> handle, float opacity);

	/// <summary>
	/// Invoked via <see cref="ModelInstance.DrawOrderDeferralAmount"/>.
	/// </summary>
	int? GetDrawOrderDeferralAmount(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="ModelInstance.DrawOrderDeferralAmount"/>.
	/// </summary>
	void SetDrawOrderDeferralAmount(ResourceHandle<ModelInstance> handle, int? newValue);

	/// <summary>
	/// Invoked internally to restrict where an instance may draw, which is how canvas objects are clipped to their parent.
	/// </summary>
	void SetScissorRect(ResourceHandle<ModelInstance> handle, XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions);
	/// <summary>
	/// Invoked internally to remove a previously-applied drawing restriction.
	/// </summary>
	void ClearScissorRect(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked internally to give an instance a material of its own, so that per-instance changes do not affect others sharing the original.
	/// </summary>
	Material GetOrCreatePrivateMaterial(ResourceHandle<ModelInstance> handle);
	
	/// <summary>
	/// Invoked internally when a text instance is first created.
	/// </summary>
	void SetTextInstanceInitialPenAndString(ResourceHandle<ModelInstance> handle, FontPen pen, FontString @string, TextLayout layout);
	/// <summary>
	/// Invoked internally when a text instance is laid out.
	/// </summary>
	void SetTextInstanceLayout(ResourceHandle<ModelInstance> handle, TextLayout layout);
	/// <summary>
	/// Invoked via <see cref="TextInstance.Pen"/>.
	/// </summary>
	void UpdateTextInstancePen(ResourceHandle<ModelInstance> handle, FontPen pen);
	/// <summary>
	/// Invoked via <see cref="TextInstance.String"/>.
	/// </summary>
	void UpdateTextInstanceString(ResourceHandle<ModelInstance> handle, FontString @string);
	/// <summary>
	/// Invoked via <see cref="TextInstance.Pen"/>.
	/// </summary>
	FontPen GetTextInstancePen(ResourceHandle<ModelInstance> handle);
	/// <summary>
	/// Invoked via <see cref="TextInstance.String"/>.
	/// </summary>
	FontString GetTextInstanceString(ResourceHandle<ModelInstance> handle);
}