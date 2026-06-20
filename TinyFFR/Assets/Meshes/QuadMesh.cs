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
		// Quad meshes are built as 1x1 squares on the XY plane facing forward with up being the upright direction by the IMeshBuilder default implementation
		
		var rotation = Rotation.FromStartAndEndOrientation(Direction.Forward, Direction.Up, facingDirection, uprightDirection ?? Direction.Up);
		
		var translatedAnchorPoint = UiUtils.TranslateAnchoredCanvasOffsetNormalized(DiagonalOrientation2D.DownLeft, positionAnchor) * -size;
		
		return new Transform(
			translation: (new Vect(translatedAnchorPoint.X, translatedAnchorPoint.Y, 0f) * rotation) + position.AsVect(),
			rotation: rotation,
			scaling: new Vect(size.X, size.Y, 1f)
		);
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

public readonly struct QuadInstance : IDisposable, IStringSpanNameEnabled, IEquatable<QuadInstance> {
	public ModelInstance UnderlyingModelInstance { get; }

	public QuadInstance(ModelInstance underlyingModelInstance) {
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
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
	public void Dispose() => UnderlyingModelInstance.Dispose();

	public override string ToString() => $"Quad {UnderlyingModelInstance}";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ModelInstance(QuadInstance operand) => operand.UnderlyingModelInstance;

	#region Equality
	public bool Equals(QuadInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is QuadInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(QuadInstance left, QuadInstance right) => left.Equals(right);
	public static bool operator !=(QuadInstance left, QuadInstance right) => !left.Equals(right);
	#endregion
}