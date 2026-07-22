// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct QuadMesh : IDisposable, IStringSpanNameEnabled, IEquatable<QuadMesh> {
	public Mesh UnderlyingMesh { get; } 

	public QuadMesh(Mesh underlyingMesh) {
		UnderlyingMesh = underlyingMesh;
	}

	public static Transform CalculateTransformForStandardQuadMesh(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		// Quad meshes are built as 1x1 squares centred on their origin on the XY plane facing backward with up being the upright direction by the IMeshBuilder default implementation
		var rotation = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, facingDirection, uprightDirection ?? Direction.Up);
		return new Transform(
			translation: (CalculateAnchorOffsetForStandardQuadMesh(size, positionAnchor) * rotation) + position.AsVect(),
			rotation: rotation,
			scaling: new Vect(size.X, size.Y, 1f)
		);
	}

	public static Vect CalculateAnchorOffsetForStandardQuadMesh(XYPair<float> size, Orientation2D positionAnchor) {
		var translatedAnchorPoint = (UiUtils.TranslateAnchoredCanvasOffsetNormalized(DiagonalOrientation2D.DownRight, positionAnchor) - new XYPair<float>(0.5f)) * -size;
		return new Vect(translatedAnchorPoint.X, translatedAnchorPoint.Y, 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingMesh.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingMesh.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingMesh.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingMesh.Dispose();

	public override string ToString() => $"Quad {UnderlyingMesh}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Mesh(QuadMesh operand) => operand.UnderlyingMesh;

	#region Equality
	public bool Equals(QuadMesh other) => UnderlyingMesh.Equals(other.UnderlyingMesh);
	public override bool Equals(object? obj) => obj is QuadMesh other && Equals(other);
	public override int GetHashCode() => UnderlyingMesh.GetHashCode();
	public static bool operator ==(QuadMesh left, QuadMesh right) => left.Equals(right);
	public static bool operator !=(QuadMesh left, QuadMesh right) => !left.Equals(right);
	#endregion
}

public interface IQuadInstance : IDisposable, IStringSpanNameEnabled;

public readonly struct QuadInstance : IQuadInstance, IEquatable<QuadInstance>, ITransformedSceneObject, IMaterialUsingSceneObject {
	public ModelInstance UnderlyingModelInstance { get; }

	internal QuadInstance(ModelInstance underlyingModelInstance) {
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Transform;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetTransform(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;
	
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotation(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.RotationQuaternion;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotationQuaternion(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetScaling(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScaling(float uniformScaling) => Scaling = new Vect(uniformScaling);
	
	public Material? Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetMaterial(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material? material) => Material = material;

	public MaterialEffectController? MaterialEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.MaterialEffects;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	public void SetTransform(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		UnderlyingModelInstance.SetTransform(QuadMesh.CalculateTransformForStandardQuadMesh(position, size, facingDirection, uprightDirection, positionAnchor));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingModelInstance.MoveBy(translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => UnderlyingModelInstance.RotateBy(rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotation, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => UnderlyingModelInstance.RotateBy(rotationQuaternion);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotationQuaternion, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingModelInstance.ScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingModelInstance.ScaleBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingModelInstance.AdjustScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingModelInstance.AdjustScaleBy(vect);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetNullMaterialBaseColor(ColorVect baseColor) => UnderlyingModelInstance.SetNullMaterialBaseColor(baseColor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetNullMaterialShadingStyle(NullMaterialShadingStyle style) => UnderlyingModelInstance.SetNullMaterialShadingStyle(style);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingModelInstance.Dispose();

	public override string ToString() => $"Quad {UnderlyingModelInstance}";

	#region Equality
	public bool Equals(QuadInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is QuadInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(QuadInstance left, QuadInstance right) => left.Equals(right);
	public static bool operator !=(QuadInstance left, QuadInstance right) => !left.Equals(right);
	#endregion
}

public readonly struct CameraLockedQuadInstance : IQuadInstance, IEquatable<CameraLockedQuadInstance>, IScaledSceneObject, IPositionedSceneObject, IMaterialUsingSceneObject {
	public QuadInstance UnderlyingQuadInstance { get; }
	public Direction LockedUprightDirection { get; }
	public Orientation2D PositionAnchor { get; }

	internal CameraLockedQuadInstance(QuadInstance underlyingQuadInstance, Direction lockedUprightDirection, Orientation2D positionAnchor) {
		UnderlyingQuadInstance = underlyingQuadInstance;
		LockedUprightDirection = lockedUprightDirection;
		PositionAnchor = positionAnchor;
	}
	
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetScaling(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScaling(float uniformScaling) => Scaling = new Vect(uniformScaling);
	
	public Material? Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetMaterial(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material? material) => Material = material;

	public MaterialEffectController? MaterialEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.MaterialEffects;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingQuadInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingQuadInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingQuadInstance.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingQuadInstance.MoveBy(translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingQuadInstance.ScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingQuadInstance.ScaleBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingQuadInstance.AdjustScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingQuadInstance.AdjustScaleBy(vect);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetNullMaterialBaseColor(ColorVect baseColor) => UnderlyingQuadInstance.SetNullMaterialBaseColor(baseColor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetNullMaterialShadingStyle(NullMaterialShadingStyle style) => UnderlyingQuadInstance.SetNullMaterialShadingStyle(style);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingQuadInstance.Dispose();

	public override string ToString() => UnderlyingQuadInstance.ToString();

	#region Equality
	public bool Equals(CameraLockedQuadInstance other) => UnderlyingQuadInstance.Equals(other.UnderlyingQuadInstance);
	public override bool Equals(object? obj) => obj is CameraLockedQuadInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingQuadInstance.GetHashCode();
	public static bool operator ==(CameraLockedQuadInstance left, CameraLockedQuadInstance right) => left.Equals(right);
	public static bool operator !=(CameraLockedQuadInstance left, CameraLockedQuadInstance right) => !left.Equals(right);
	#endregion
}