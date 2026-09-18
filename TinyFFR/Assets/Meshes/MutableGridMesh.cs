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

/// <summary>
/// A mesh that is a flat grid of vertices, each of which can be displaced at runtime by the instances created from it.
/// </summary>
/// <remarks>
/// <para>
/// A mutable grid is a flat sheet of vertices that objects created from it can displace at runtime, which is how effects like rippling
/// water, rolling terrain and waving cloth can be made. Mutable grids are also useful for mathematical or diagnostic manifold/plane visualizations.
/// Often (but not always) paired with a dynamic <see cref="Texture"/> (i.e. one created with <see cref="Texture.AllowsDynamicWrites"/> set to <c>true</c>).
/// </para>
/// <para>
/// The mesh itself holds the flat, undisplaced grid; each instance keeps its own displacements, so many
/// differently-deformed surfaces can share one mesh.
/// </para>
/// <para>
/// This wraps an ordinary <see cref="Mesh"/> and can be used anywhere one is expected. The underlying mesh must have been
/// created with per-instance vertex mutation enabled, which costs some ordinary memory in addition to the usual video memory.
/// </para>
/// </remarks>
public readonly struct MutableGridMesh : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridMesh> {
	readonly bool _directionsAllCardinal;
	/// <summary>
	/// The general-purpose mesh this grid is a specialized view of.
	/// </summary>
	public Mesh UnderlyingMesh { get; } 
	/// <summary>
	/// How many vertices the grid has along each of its two axes.
	/// </summary>
	/// <remarks>
	/// A grid of <c>(n, m)</c> has <c>n * m</c> vertices and <c>(n - 1) * (m - 1)</c> quads between them, so the smallest
	/// useful grid is <c>(2, 2)</c>.
	/// </remarks>
	public XYPair<int> GridDimensions { get; }
	/// <summary>
	/// Which way in the world the grid's first axis runs.
	/// </summary>
	public Direction XDir { get; }
	/// <summary>
	/// Which way in the world the grid's second axis runs.
	/// </summary>
	public Direction YDir { get; }
	/// <summary>
	/// Which way the grid's vertices are displaced when their height is raised.
	/// </summary>
	/// <remarks>
	/// For a grid lying flat on the ground this is straight up, which is what makes a height value read as an elevation.
	/// </remarks>
	public Direction UpDir { get; }
	/// <summary>
	/// Which corner of the grid its own origin sits at (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	public Orientation2D Origin { get; }

	/// <summary>
	/// Constructs a new <see cref="MutableGridMesh"/> describing how an existing mesh's vertices are laid out as a grid.
	/// </summary>
	/// <remarks>
	/// Only use this for a mesh that really was built as a grid of the given dimensions; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingMesh">The mesh to wrap.</param>
	/// <param name="gridDimensions">The value for <see cref="GridDimensions"/>. Both components must be at least <c>2</c>.</param>
	/// <param name="xDir">The value for <see cref="XDir"/>.</param>
	/// <param name="yDir">The value for <see cref="YDir"/>.</param>
	/// <param name="upDir">The value for <see cref="UpDir"/>.</param>
	/// <param name="origin">The value for <see cref="Origin"/>.</param>
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
	
	/// <summary>
	/// Calculates the transform that places and sizes this grid as described.
	/// </summary>
	/// <remarks>
	/// The grid's geometry is built at a fixed size, so it must be scaled in to place. This works that out, taking the grid's
	/// own axis directions in to account so that the size given is applied along the grid rather than along the world axes.
	/// </remarks>
	/// <param name="position">Where to put the grid.</param>
	/// <param name="size">How large the grid should be along its own two axes, in world units (metres).</param>
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

	/// <summary>
	/// Returns the position in the vertex list of the vertex at the given grid coordinate.
	/// </summary>
	/// <param name="xy">Which vertex of the grid, in the range <c>(0, 0)</c> to <c>GridDimensions - (1, 1)</c> inclusive.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetVertexIndex(XYPair<int> xy) => GridDimensions.Index(xy);
	/// <summary>
	/// Returns the grid coordinate of the vertex at the given position in the vertex list.
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="GetVertexIndex"/>.
	/// </remarks>
	/// <param name="index">A position in the vertex list, in the range <c>0 &lt;= index &lt; GridDimensions.Area</c>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetVertexCoordinate(int index) => GridDimensions.ReverseIndex(index);
	/// <summary>
	/// Returns where the vertex at the given position in the vertex list sits across the grid, as a fraction.
	/// </summary>
	/// <remarks>
	/// The result runs from <c>0</c> to <c>1</c> across the grid, shifted so that <see cref="Origin"/> is at zero. That makes
	/// it directly usable as an input to any function of position across the grid — a wave, a slope or a noise field.
	/// </remarks>
	/// <param name="index">A position in the vertex list, in the range <c>0 &lt;= index &lt; GridDimensions.Area</c>.</param>
	public XYPair<float> GetVertexCoordinateNormalized(int index) => GetVertexCoordinateNormalized(GetVertexCoordinate(index));
	/// <summary>
	/// Returns where the vertex at the given grid coordinate sits across the grid, as a fraction.
	/// </summary>
	/// <remarks>
	/// The result runs from <c>0</c> to <c>1</c> across the grid, shifted so that <see cref="Origin"/> is at zero.
	/// </remarks>
	/// <param name="xy">Which vertex of the grid, in the range <c>(0, 0)</c> to <c>GridDimensions - (1, 1)</c> inclusive.</param>
	public XYPair<float> GetVertexCoordinateNormalized(XYPair<int> xy) {
		var result = xy.Cast<float>() / (GridDimensions - XYPair<int>.One).Cast<float>();
		return result - MathUtils.FindAnchorInNormalized2DCoordinateSystem(DiagonalOrientation2D.DownLeft, Origin);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingMesh.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingMesh.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingMesh.CopyName(destinationBuffer);

	/// <summary>
	/// Disposes the underlying mesh, releasing its GPU resources.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingMesh.Dispose();

	/// <inheritdoc />
	public override string ToString() => $"{GridDimensions.X}x{GridDimensions.Y} Mutable Grid {UnderlyingMesh}";

	/// <summary>
	/// Returns the general-purpose mesh this grid is a specialized view of.
	/// </summary>
	/// <param name="operand">The grid mesh to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Mesh(MutableGridMesh operand) => operand.UnderlyingMesh;

	#region Equality
	/// <inheritdoc />
	public bool Equals(MutableGridMesh other) => UnderlyingMesh.Equals(other.UnderlyingMesh);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MutableGridMesh other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingMesh.GetHashCode();
	/// <summary>
	/// Returns whether the two given grid meshes wrap the same underlying mesh.
	/// </summary>
	/// <param name="left">The first grid mesh to compare.</param>
	/// <param name="right">The second grid mesh to compare.</param>
	public static bool operator ==(MutableGridMesh left, MutableGridMesh right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given grid meshes wrap different underlying meshes.
	/// </summary>
	/// <param name="left">The first grid mesh to compare.</param>
	/// <param name="right">The second grid mesh to compare.</param>
	public static bool operator !=(MutableGridMesh left, MutableGridMesh right) => !left.Equals(right);
	#endregion
}

/// <summary>
/// An instance of a <see cref="MutableGridMesh"/>: A grid surface placed in a scene, whose vertices can be displaced independently of any other instance of the same grid mesh.
/// </summary>
/// <remarks>
/// <para>
/// Displacements are made by borrowing the vertex span from <see cref="BorrowVerticesSpan"/>, writing in to it, and disposing
/// the lease; typically once per frame. The values persist between leases, so each frame starts from wherever the surface was
/// left.
/// </para>
/// <para>
/// The grid itself can be placed in the world precisely using <see cref="SetTransform(Location, XYPair{float})"/>.
/// </para>
/// </remarks>
public readonly struct MutableGridInstance : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridInstance>, ITransformedSceneObject, IMaterialUsingSceneObject {
	static readonly Lock _staticMutationLock = new();
	static readonly HeapPool _sharedHeapPool = new();
	static readonly ArrayPoolBackedMap<nuint, MutableGridInstance> _activeLeaseMap = new();
	static nuint _prevLeaseId = 0U;
	
	readonly PooledHeapMemory<MutableGridVertex> _vertexBuffer;
	readonly PooledHeapMemory<XYPair<float>> _precalculatedNormalizedCoords;
	/// <summary>
	/// The general-purpose model instance this grid instance is a specialized view of.
	/// </summary>
	public ModelInstance UnderlyingModelInstance { get; }
	/// <summary>
	/// The grid mesh this instance was created from, which describes how its vertices are laid out.
	/// </summary>
	public MutableGridMesh ParentGridMesh { get; }
	
	/// <inheritdoc />
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Transform;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetTransform(value);
	}
	/// <summary>
	/// Sets <see cref="Transform"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="transform">The new value for <see cref="Transform"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;
	
	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetPosition(value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <inheritdoc />
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotation(value);
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	/// <inheritdoc />
	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.RotationQuaternion;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotationQuaternion(value);
	}
	/// <summary>
	/// Sets <see cref="RotationQuaternion"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotationQuaternion">The new value for <see cref="RotationQuaternion"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	/// <inheritdoc />
	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetScaling(value);
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;
	/// <summary>
	/// Sets <see cref="Scaling"/> to the same value on every axis.
	/// </summary>
	/// <param name="uniformScaling">The scaling to apply on all three axes.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScaling(float uniformScaling) => Scaling = new Vect(uniformScaling);
	
	/// <inheritdoc />
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetMaterial(value);
	}
	/// <summary>
	/// Sets <see cref="Material"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="material">The new value for <see cref="Material"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	/// <inheritdoc />
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
	
	/// <summary>
	/// Borrows the grid's displacement values so that they can be written to, and applies them to the mesh when the lease is disposed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the whole point of a mutable grid: write a height (and optionally a lateral offset) for each vertex, dispose the
	/// lease, and the geometry follows. Nothing reaches the mesh until the lease is disposed, so a whole frame's worth of
	/// changes costs one update rather than one per vertex.
	/// </para>
	/// <para>
	/// The values persist between leases, so each frame starts from whatever was written last. This instance can not be
	/// disposed whilst a lease is outstanding.
	/// </para>
	/// </remarks>
	/// <param name="permitLateralDisplacement">Whether to apply each vertex's sideways offset as well as its height.
	/// Leaving this <see langword="false"/> ignores <see cref="MutableGridVertex.NormalizedLateralOffset"/> entirely,
	/// is cheaper and is all that is needed for a grid that only ever moves up and down.</param>
	/// <exception cref="ObjectDisposedException">Thrown when this instance has already been disposed.</exception>
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
	
	/// <summary>
	/// Places and sizes this grid in one call.
	/// </summary>
	/// <remarks>
	/// This is the convenient way to position a grid, as it works in terms of the grid's own two axes rather than requiring a
	/// full transform to be assembled first.
	/// </remarks>
	/// <param name="position">Where to put the grid.</param>
	/// <param name="size">How large the grid should be along its own two axes, in world units (metres).</param>
	public void SetTransform(Location position, XYPair<float> size) {
		UnderlyingModelInstance.SetTransform(ParentGridMesh.CalculateTransform(position, size));
	}
	
	/// <summary>
	/// Returns the position in the vertex list of the vertex at the given grid coordinate.
	/// </summary>
	/// <param name="xy">Which vertex of the grid, in the range <c>(0, 0)</c> to <c>GridDimensions - (1, 1)</c> inclusive.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetVertexIndex(XYPair<int> xy) => ParentGridMesh.GetVertexIndex(xy);
	/// <summary>
	/// Returns the grid coordinate of the vertex at the given position in the vertex list.
	/// </summary>
	/// <param name="index">A position in the vertex list, in the range <c>0 &lt;= index &lt; GridDimensions.Area</c>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetVertexCoordinate(int index) => ParentGridMesh.GetVertexCoordinate(index);
	/// <summary>
	/// Returns where the vertex at the given position in the vertex list sits across the grid, as a fraction.
	/// </summary>
	/// <remarks>
	/// The result runs from <c>0</c> to <c>1</c> across the grid, shifted so that the grid's origin is at zero. These values are
	/// worked out once when the instance is created, so reading them here is cheaper than recomputing them each frame.
	/// </remarks>
	/// <param name="index">A position in the vertex list, in the range <c>0 &lt;= index &lt; GridDimensions.Area</c>.</param>
	/// <exception cref="ObjectDisposedException">Thrown when this instance has already been disposed.</exception>
	public XYPair<float> GetVertexCoordinateNormalized(int index) {
		ObjectDisposedException.ThrowIf(UnderlyingModelInstance.IsDisposed, typeof(MutableGridMesh));
		return _precalculatedNormalizedCoords.Span[index];
	}
	/// <summary>
	/// Returns where the vertex at the given grid coordinate sits across the grid, as a fraction.
	/// </summary>
	/// <remarks>
	/// The result runs from <c>0</c> to <c>1</c> across the grid, shifted so that the grid's origin is at zero.
	/// </remarks>
	/// <param name="xy">Which vertex of the grid, in the range <c>(0, 0)</c> to <c>GridDimensions - (1, 1)</c> inclusive.</param>
	public XYPair<float> GetVertexCoordinateNormalized(XYPair<int> xy) => GetVertexCoordinateNormalized(ParentGridMesh.GridDimensions.Index(xy));

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingModelInstance.MoveBy(translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => UnderlyingModelInstance.RotateBy(rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotation, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => UnderlyingModelInstance.RotateBy(rotationQuaternion);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotationQuaternion, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingModelInstance.ScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingModelInstance.ScaleBy(vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingModelInstance.AdjustScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingModelInstance.AdjustScaleBy(vect);
	
	/// <summary>
	/// Sets the base colour this grid is drawn in, for a grid using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a grid using a material of your own.
	/// </remarks>
	/// <param name="baseColor">The colour to draw the grid in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => UnderlyingModelInstance.SetDefaultMaterialBaseColor(baseColor);
	/// <summary>
	/// Sets how this grid reacts to light, for a grid using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a grid using a material of your own.
	/// </remarks>
	/// <param name="style">The shading style to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => UnderlyingModelInstance.SetDefaultMaterialShadingStyle(style);
	
	/// <summary>
	/// Disposes the underlying model instance and releases this grid's displacement buffers.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when a vertex span lease taken from <see cref="BorrowVerticesSpan"/> has not yet been disposed.</exception>
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

	/// <inheritdoc />
	public override string ToString() => $"{ParentGridMesh.GridDimensions.X}x{ParentGridMesh.GridDimensions.Y} Mutable Grid {UnderlyingModelInstance}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(MutableGridInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MutableGridInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	/// <summary>
	/// Returns whether the two given grid instances wrap the same underlying model instance.
	/// </summary>
	/// <param name="left">The first grid instance to compare.</param>
	/// <param name="right">The second grid instance to compare.</param>
	public static bool operator ==(MutableGridInstance left, MutableGridInstance right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given grid instances wrap different underlying model instances.
	/// </summary>
	/// <param name="left">The first grid instance to compare.</param>
	/// <param name="right">The second grid instance to compare.</param>
	public static bool operator !=(MutableGridInstance left, MutableGridInstance right) => !left.Equals(right);
	#endregion
}

/// <summary>
/// How far one vertex of a mutable grid is displaced from where it would otherwise sit.
/// </summary>
/// <remarks>
/// A vertex that is left at its default value sits exactly where the flat grid put it.
/// </remarks>
/// <param name="Height">How far the vertex is displaced along the grid's upward direction. May be negative, to push the vertex below the grid.</param>
/// <param name="NormalizedLateralOffset">How far the vertex is displaced across the grid, as a fraction of the spacing between neighbouring vertices.
/// A value of <c>(1, 0)</c> therefore moves the vertex to touch its neighbour's spacing area along the grid's first axis.
/// Ignored unless lateral displacement was permitted when the vertex span was borrowed.</param>
public readonly record struct MutableGridVertex(float Height, XYPair<float> NormalizedLateralOffset) {
	/// <summary>
	/// Constructs a new <see cref="MutableGridVertex"/> displaced only in height, with no sideways offset.
	/// </summary>
	/// <param name="Height">The value for <see cref="Height"/>.</param>
	public MutableGridVertex(float Height) : this(Height, XYPair<float>.Zero) { }
}