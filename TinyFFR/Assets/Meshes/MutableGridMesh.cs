// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using System.Numerics;
using System.Threading;
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
	public Orientation2D Origin { get; }

	public MutableGridMesh(Mesh underlyingMesh, XYPair<int> gridDimensions, Direction xDir, Direction yDir, Direction upDir, Orientation2D origin) {
		// ReSharper disable once CompareOfFloatsByEqualityOperator This is only used to help us skip using a matrix for the transform below and is not critical
		static bool IsCardinal(Direction d) => MathF.Abs(d.X) + MathF.Abs(d.Y) + MathF.Abs(d.Z) == 1f;
		
		UnderlyingMesh = underlyingMesh;
		GridDimensions = gridDimensions;
		XDir = xDir;
		YDir = yDir;
		UpDir = upDir;
		Origin = origin;
		_directionsAllCardinal = IsCardinal(xDir) && IsCardinal(yDir) && IsCardinal(upDir);
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
	public int GetVertexIndex(XYPair<int> xy) => GridDimensions.Index(xy);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetVertexCoordinate(int index) => GridDimensions.ReverseIndex(index);
	public XYPair<float> GetVertexCoordinateNormalized(int index) => GetVertexCoordinateNormalized(GetVertexCoordinate(index));
	public XYPair<float> GetVertexCoordinateNormalized(XYPair<int> xy) {
		var result = xy.Cast<float>() / (GridDimensions - XYPair<int>.One).Cast<float>();
		return result - UiUtils.TranslateAnchoredCanvasOffsetNormalized(DiagonalOrientation2D.DownLeft, Origin);
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

public readonly struct MutableGridInstance : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridInstance>, ITransformedSceneObject, IMaterialUsingSceneObject {
	static readonly Lock _staticMutationLock = new();
	static readonly HeapPool _sharedHeapPool = new();
	static readonly ArrayPoolBackedMap<nuint, MutableGridInstance> _activeLeaseMap = new();
	static nuint _prevLeaseId = 0U;
	
	readonly PooledHeapMemory<MutableGridVertex> _vertexBuffer;
	readonly PooledHeapMemory<XYPair<float>> _precalculatedNormalizedCoords;
	public ModelInstance UnderlyingModelInstance { get; }
	public MutableGridMesh ParentGridMesh { get; }
	
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
	
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetMaterial(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	public MaterialEffectController? MaterialEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.MaterialEffects;
	}

	internal MutableGridInstance(ModelInstance underlyingModelInstance, MutableGridMesh parentGridMesh) {
		UnderlyingModelInstance = underlyingModelInstance;
		ParentGridMesh = parentGridMesh;
		lock (_staticMutationLock) {
			_vertexBuffer = _sharedHeapPool.Borrow<MutableGridVertex>(parentGridMesh.GridDimensions.Area);
			_precalculatedNormalizedCoords = _sharedHeapPool.Borrow<XYPair<float>>(parentGridMesh.GridDimensions.Area);
		}
		_vertexBuffer.Span.Clear();
		for (var y = 0; y < parentGridMesh.GridDimensions.Y; ++y) {
			for (var x = 0; x < parentGridMesh.GridDimensions.X; ++x) {
				_precalculatedNormalizedCoords.Span[parentGridMesh.GridDimensions.Index(x, y)] = parentGridMesh.GetVertexCoordinateNormalized((x, y));
			}
		}
	}
	
	public unsafe ScopedSpanLease<MutableGridVertex> BorrowVerticesSpan(bool permitLateralDisplacement) {
		static void HandleLeaseDisposalWithLateralDisplacement(object? _, nuint leaseId) {
			MutableGridInstance @this;
			lock (_staticMutationLock) {
				if (!_activeLeaseMap.Remove(leaseId, out @this)) return;
			}
				
			var gridVerts = @this._vertexBuffer.Span;
			using var innerLease = @this.UnderlyingModelInstance.BorrowVerticesSpan(false);
			using var defaultVertsLease = @this.ParentGridMesh.UnderlyingMesh.BorrowDefaultVerticesSpan();
			var gridDimensions = @this.ParentGridMesh.GridDimensions;
			var gridArea = gridDimensions.Area;
			var isTwoSided = defaultVertsLease.Span.Length == gridArea * 2;
			var xDir = @this.ParentGridMesh.XDir;
			var yDir = @this.ParentGridMesh.YDir;
			var upDir = @this.ParentGridMesh.UpDir;
			var scalarOffsetPerGridStep = ((gridDimensions - XYPair<int>.One).Cast<float>().Reciprocal ?? XYPair<float>.One) * 0.5f;
			var vectorOffsetPerGridStep = (X: xDir * scalarOffsetPerGridStep.X, Y: yDir * scalarOffsetPerGridStep.Y);
			
			if (isTwoSided) {
				for (var y = 0; y < gridDimensions.Y; ++y) {
					for (var x = 0; x < gridDimensions.X; ++x) {
						var index = gridDimensions.Index(x, y);
						
						innerLease.Span[index] = innerLease.Span[index] with {
							Location = defaultVertsLease.Span[index].Location
								+ (vectorOffsetPerGridStep.X * gridVerts[index].NormalizedLateralOffset.X)
								+ (vectorOffsetPerGridStep.Y * gridVerts[index].NormalizedLateralOffset.Y)
								+ (upDir * gridVerts[index].Height)
						};
						innerLease.Span[index + gridArea] = innerLease.Span[index + gridArea] with { Location = innerLease.Span[index].Location };
					}	
				}
			}
			else {
				for (var y = 0; y < gridDimensions.Y; ++y) {
					for (var x = 0; x < gridDimensions.X; ++x) {
						var index = gridDimensions.Index(x, y);
						
						innerLease.Span[index] = innerLease.Span[index] with {
							Location = defaultVertsLease.Span[index].Location
								+ (vectorOffsetPerGridStep.X * gridVerts[index].NormalizedLateralOffset.X)
								+ (vectorOffsetPerGridStep.Y * gridVerts[index].NormalizedLateralOffset.Y)
								+ (upDir * gridVerts[index].Height)
						};
					}	
				}
			}
		}
		
		static void HandleLeaseDisposal(object? _, nuint leaseId) {
			MutableGridInstance @this;
			lock (_staticMutationLock) {
				if (!_activeLeaseMap.Remove(leaseId, out @this)) return;
			}
				
			var gridVerts = @this._vertexBuffer.Span;
			using var innerLease = @this.UnderlyingModelInstance.BorrowVerticesSpan(false);
			using var defaultVertsLease = @this.ParentGridMesh.UnderlyingMesh.BorrowDefaultVerticesSpan();
			var gridDimensions = @this.ParentGridMesh.GridDimensions;
			var gridArea = gridDimensions.Area;
			var isTwoSided = defaultVertsLease.Span.Length == gridArea * 2;
			var upDir = @this.ParentGridMesh.UpDir;
			
			if (isTwoSided) {
				for (var y = 0; y < gridDimensions.Y; ++y) {
					for (var x = 0; x < gridDimensions.X; ++x) {
						var index = gridDimensions.Index(x, y);
						
						innerLease.Span[index] = innerLease.Span[index] with {
							Location = defaultVertsLease.Span[index].Location + (upDir * gridVerts[index].Height)
						};
						innerLease.Span[index + gridArea] = innerLease.Span[index + gridArea] with { Location = innerLease.Span[index].Location };
					}	
				}
			}
			else {
				for (var y = 0; y < gridDimensions.Y; ++y) {
					for (var x = 0; x < gridDimensions.X; ++x) {
						var index = gridDimensions.Index(x, y);
						
						innerLease.Span[index] = innerLease.Span[index] with {
							Location = defaultVertsLease.Span[index].Location + (upDir * gridVerts[index].Height)
						};
					}	
				}
			}
		}
		
		ObjectDisposedException.ThrowIf(UnderlyingModelInstance.IsDisposed, typeof(MutableGridMesh));
		
		lock (_staticMutationLock) {
			var leaseId = ++_prevLeaseId;
			_activeLeaseMap.Add(leaseId, this);
			return new ScopedSpanLease<MutableGridVertex>(permitLateralDisplacement ? &HandleLeaseDisposalWithLateralDisplacement : &HandleLeaseDisposal, null, leaseId, _vertexBuffer.Span);
		}
	}
	
	public void SetTransform(Location position, XYPair<float> size) {
		UnderlyingModelInstance.SetTransform(ParentGridMesh.CalculateTransform(position, size));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetVertexIndex(XYPair<int> xy) => ParentGridMesh.GetVertexIndex(xy);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetVertexCoordinate(int index) => ParentGridMesh.GetVertexCoordinate(index);
	public XYPair<float> GetVertexCoordinateNormalized(int index) {
		ObjectDisposedException.ThrowIf(UnderlyingModelInstance.IsDisposed, typeof(MutableGridMesh));
		return _precalculatedNormalizedCoords.Span[index];
	}
	public XYPair<float> GetVertexCoordinateNormalized(XYPair<int> xy) => GetVertexCoordinateNormalized(ParentGridMesh.GridDimensions.Index(xy));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
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
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => UnderlyingModelInstance.SetDefaultMaterialBaseColor(baseColor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => UnderlyingModelInstance.SetDefaultMaterialShadingStyle(style);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() {
		lock (_staticMutationLock) {
			foreach (var instance in _activeLeaseMap.Values) {
				if (instance == this) {
#pragma warning disable CA1065 // "Don't throw exceptions in dispose" -- Vastly preferable to leaking leases
					throw new InvalidOperationException($"Can not dispose this {this} as there are least one active vertex span lease(s) not yet disposed.");
#pragma warning restore CA1065
				}
			}
			_vertexBuffer.Dispose();
			_precalculatedNormalizedCoords.Dispose();
		}
		UnderlyingModelInstance.Dispose();
	}

	public override string ToString() => $"{ParentGridMesh.GridDimensions.X}x{ParentGridMesh.GridDimensions.Y} Mutable Grid {UnderlyingModelInstance}";

	#region Equality
	public bool Equals(MutableGridInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is MutableGridInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(MutableGridInstance left, MutableGridInstance right) => left.Equals(right);
	public static bool operator !=(MutableGridInstance left, MutableGridInstance right) => !left.Equals(right);
	#endregion
}

public readonly record struct MutableGridVertex(float Height, XYPair<float> NormalizedLateralOffset) {
	public MutableGridVertex(float Height) : this(Height, XYPair<float>.Zero) { }
}