// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MutableGridMesh : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridMesh> {
	readonly bool _directionsAllCardinal;
	public Mesh UnderlyingMesh { get; } 
	public XYPair<int> GridDimensions { get; }
	public Direction XDir { get; }
	public Direction YDir { get; }
	public Direction UpDir { get; }

	public MutableGridMesh(Mesh underlyingMesh, XYPair<int> gridDimensions, Direction xDir, Direction yDir, Direction upDir) {
		// ReSharper disable once CompareOfFloatsByEqualityOperator This is only used to help us skip using a matrix for the transform below and is not critical
		static bool IsCardinal(Direction d) => MathF.Abs(d.X) + MathF.Abs(d.Y) + MathF.Abs(d.Z) == 1f;
		
		UnderlyingMesh = underlyingMesh;
		GridDimensions = gridDimensions;
		XDir = xDir;
		YDir = yDir;
		UpDir = upDir;
		_directionsAllCardinal = IsCardinal(xDir) && IsCardinal(yDir) && IsCardinal(upDir);
	}

	public int GetVertexIndex(XYPair<int> coordinate, bool clampCoord = true) {
		if (clampCoord) coordinate = coordinate.Clamp(XYPair<int>.Zero, GridDimensions - XYPair<int>.One);
		return GridDimensions.Index(coordinate);
	}
	
	public Transform CalculateTransform(Location position, XYPair<float> size) {
		var translation = position.AsVect();
		
		if (_directionsAllCardinal) {
			return new Transform(
				translation: translation,
				rotation: Rotation.None,
				scaling: new Vect(
					size.X * MathF.Abs(XDir.X) + size.Y * MathF.Abs(YDir.X) + MathF.Abs(UpDir.X),
					size.X * MathF.Abs(XDir.Y) + size.Y * MathF.Abs(YDir.Y) + MathF.Abs(UpDir.Y),
					size.X * MathF.Abs(XDir.Z) + size.Y * MathF.Abs(YDir.Z) + MathF.Abs(UpDir.Z)
				)
			);
		}

		var rowX = size.X * XDir.X * XDir + size.Y * YDir.X * YDir + UpDir.X * UpDir;
		var rowY = size.X * XDir.Y * XDir + size.Y * YDir.Y * YDir + UpDir.Y * UpDir;
		var rowZ = size.X * XDir.Z * XDir + size.Y * YDir.Z * YDir + UpDir.Z * UpDir;
		return new Transform(new Matrix4x4(
			rowX.X, rowX.Y, rowX.Z, 0f,
			rowY.X, rowY.Y, rowY.Z, 0f,
			rowZ.X, rowZ.Y, rowZ.Z, 0f,
			translation.X, translation.Y, translation.Z, 1f
		));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingMesh.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingMesh.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingMesh.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingMesh.Dispose();

	public override string ToString() => $"{GridDimensions.X}x{GridDimensions.Y} Mutable Grid {UnderlyingMesh}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Mesh(MutableGridMesh operand) => operand.UnderlyingMesh;

	#region Equality
	public bool Equals(MutableGridMesh other) => UnderlyingMesh.Equals(other.UnderlyingMesh);
	public override bool Equals(object? obj) => obj is MutableGridMesh other && Equals(other);
	public override int GetHashCode() => UnderlyingMesh.GetHashCode();
	public static bool operator ==(MutableGridMesh left, MutableGridMesh right) => left.Equals(right);
	public static bool operator !=(MutableGridMesh left, MutableGridMesh right) => !left.Equals(right);
	#endregion
}

public readonly struct MutableGridInstance : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridInstance> {
	public ModelInstance UnderlyingModelInstance { get; }
	public MutableGridMesh ParentGridMesh { get; init; }

	public MutableGridInstance(ModelInstance underlyingModelInstance, MutableGridMesh parentGridMesh) {
		UnderlyingModelInstance = underlyingModelInstance;
		ParentGridMesh = parentGridMesh;
	}
	
	// TODO vertex mutation and dynamic texture control, maybe also pass through material effects

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingModelInstance.Dispose();

	public override string ToString() => $"{ParentGridMesh.GridDimensions.X}x{ParentGridMesh.GridDimensions.Y} Mutable Grid {UnderlyingModelInstance}";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ModelInstance(MutableGridInstance operand) => operand.UnderlyingModelInstance;

	#region Equality
	public bool Equals(MutableGridInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is MutableGridInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(MutableGridInstance left, MutableGridInstance right) => left.Equals(right);
	public static bool operator !=(MutableGridInstance left, MutableGridInstance right) => !left.Equals(right);
	#endregion
}