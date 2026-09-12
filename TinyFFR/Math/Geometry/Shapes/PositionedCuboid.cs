// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a <see cref="Cuboid"/> positioned at a specific <see cref="Position"/> in world space, aligned with the X/Y/Z axes.
/// </summary>
public readonly struct PositionedCuboid : ITranslatedConvexShape<PositionedCuboid, Cuboid>, ICuboid<PositionedCuboid>,
	IDistanceMeasurable<PositionedSphere>, IDistanceMeasurable<PositionedCuboid>, IDistanceMeasurable<PositionedRotatedCuboid>,
	IIntersectable<PositionedSphere>, IIntersectable<PositionedCuboid>, IIntersectable<PositionedRotatedCuboid> {
	/// <summary>
	/// A <see cref="Cuboid.UnitCube"/> positioned at <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly PositionedCuboid UnitCubeAtOrigin = new(Cuboid.UnitCube, Location.Origin);
	readonly TranslatedConvexShape<Cuboid> _impl;

	/// <summary>
	/// The centre point of this cuboid.
	/// </summary>
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
	}

	/// <inheritdoc />
	public float HalfWidth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfWidth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfWidth = value } };
	}
	/// <inheritdoc />
	public float HalfHeight {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfHeight;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfHeight = value } };
	}
	/// <inheritdoc />
	public float HalfDepth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfDepth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfDepth = value } };
	}

	/// <inheritdoc />
	public float Width {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Width;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Width = value } };
	}
	/// <inheritdoc />
	public float Height {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Height;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Height = value } };
	}
	/// <inheritdoc />
	public float Depth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Depth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Depth = value } };
	}

	/// <inheritdoc />
	public float Volume {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Volume;
	}
	/// <inheritdoc />
	public float SurfaceArea {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SurfaceArea;
	}

	/// <inheritdoc />
	public float SmallestHalfExtent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SmallestHalfExtent;
	}
	/// <inheritdoc />
	public float SmallestExtent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SmallestExtent;
	}
	/// <inheritdoc />
	public float LargestHalfExtent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.LargestHalfExtent;
	}
	/// <inheritdoc />
	public float LargestExtent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.LargestExtent;
	}

	/// <inheritdoc />
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.IsPhysicallyValid;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location CentroidAt(CardinalOrientation side) => _impl.TransformToWorldSpace(_impl.BaseShape.CentroidAt(side));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location CornerAt(DiagonalOrientation corner) => _impl.TransformToWorldSpace(_impl.BaseShape.CornerAt(corner));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Plane SideAt(CardinalOrientation side) => _impl.TransformToWorldSpace(_impl.BaseShape.SideAt(side));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay EdgeAt(IntercardinalOrientation edge) => _impl.TransformToWorldSpace(_impl.BaseShape.EdgeAt(edge));


	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedCuboid, Location> Corners => new(this, GetIteratorVersion(this), &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(PositionedCuboid _) => 8;
	static Location GetCornerForEnumerator(PositionedCuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedCuboid, BoundedRay> Edges => new(this, GetIteratorVersion(this), &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(PositionedCuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(PositionedCuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedCuboid, Plane> Sides => new(this, GetIteratorVersion(this), &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(PositionedCuboid _) => 6;
	static Plane GetSideForEnumerator(PositionedCuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedCuboid, Location> Centroids => new(this, GetIteratorVersion(this), &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(PositionedCuboid _) => 6;
	static Location GetCentroidForEnumerator(PositionedCuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(PositionedCuboid _) => 0;

	/// <inheritdoc cref="Cuboid.GetExtent" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetExtent(Axis axis) => _impl.BaseShape.GetExtent(axis);
	/// <inheritdoc cref="Cuboid.GetHalfExtent" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetHalfExtent(Axis axis) => _impl.BaseShape.GetHalfExtent(axis);
	/// <inheritdoc cref="Cuboid.GetSideSurfaceArea" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetSideSurfaceArea(CardinalOrientation side) => _impl.BaseShape.GetSideSurfaceArea(side);

	/// <inheritdoc />
	public PositionedCuboid WithVolume(float newVolume) => new(_impl.BaseShape.WithVolume(newVolume), Position);
	/// <inheritdoc />
	public PositionedCuboid WithSurfaceArea(float newSurfaceArea) => new(_impl.BaseShape.WithSurfaceArea(newSurfaceArea), Position);
	/// <inheritdoc />
	public PositionedCuboid WithAllExtentsAdjustedBy(float adjustment) => new(_impl.BaseShape.WithAllExtentsAdjustedBy(adjustment), Position);

	Cuboid ITranslatedShape<PositionedCuboid, Cuboid>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}

	Vect ITranslatedShape.Translation {
		get => Position.AsVect();
		init => Position = value.AsLocation();
	}

	/// <summary>
	/// Constructs a new cube (a cuboid with equal <see cref="Width"/>, <see cref="Height"/> and <see cref="Depth"/>) at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="widthHeightDepth">The size of the cube on all three axes.</param>
	/// <param name="centerPoint">The centre point of the cube.</param>
	public PositionedCuboid(float widthHeightDepth, Location centerPoint) : this(new Cuboid(widthHeightDepth), centerPoint) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedCuboid"/> with the given dimensions, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="width">The size of the cuboid on the X axis.</param>
	/// <param name="height">The size of the cuboid on the Y axis.</param>
	/// <param name="depth">The size of the cuboid on the Z axis.</param>
	/// <param name="centerPoint">The centre point of the cuboid.</param>
	public PositionedCuboid(float width, float height, float depth, Location centerPoint) : this(new Cuboid(width, height, depth), centerPoint) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedCuboid"/> from an existing <see cref="Cuboid"/>, positioned at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="baseShape">The unpositioned cuboid.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public PositionedCuboid(Cuboid baseShape, Location centerPoint) : this(new(baseShape, centerPoint.AsVect())) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedCuboid"/> directly from its underlying <see cref="TranslatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="impl">The underlying translated shape.</param>
	public PositionedCuboid(TranslatedConvexShape<Cuboid> impl) {
		_impl = impl;
	}

	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<Cuboid>(PositionedCuboid operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedCuboid"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedCuboid(TranslatedConvexShape<Cuboid> operand) => new(operand);
	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<Cuboid>(PositionedCuboid operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedCuboid"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedCuboid(TranslatedShape<Cuboid> operand) => new(operand);

	/// <summary>
	/// Constructs a new <see cref="PositionedCuboid"/> from its half-dimensions, positioned at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="halfWidth">Half of the desired <see cref="Width"/>.</param>
	/// <param name="halfHeight">Half of the desired <see cref="Height"/>.</param>
	/// <param name="halfDepth">Half of the desired <see cref="Depth"/>.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedCuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth, Location centerPoint) => new(Cuboid.FromHalfDimensions(halfWidth, halfHeight, halfDepth), centerPoint);

	const DiagonalOrientation AmalgamationMinExtentsCorner = DiagonalOrientation.RightDownBackward;
	const DiagonalOrientation AmalgamationMaxExtentsCorner = DiagonalOrientation.LeftUpForward;
	/// <summary>
	/// Returns the smallest axis-aligned <see cref="PositionedCuboid"/> that fully encloses all of <paramref name="subCuboids"/>.
	/// </summary>
	/// <param name="subCuboids">The cuboids to enclose. If empty, a zero-sized cuboid at <see cref="Location.Origin"/> is returned.</param>
	public static PositionedCuboid FromSmallestEnclosingCuboid(params ReadOnlySpan<PositionedCuboid> subCuboids) {
		if (subCuboids.Length <= 0) return new(0f, Location.Origin);
		
		var minExtents = subCuboids[0].CornerAt(AmalgamationMinExtentsCorner);
		var maxExtents = subCuboids[0].CornerAt(AmalgamationMaxExtentsCorner);
		
		for (var i = 1; i < subCuboids.Length; ++i) {
			var cMin = subCuboids[i].CornerAt(AmalgamationMinExtentsCorner);
			var cMax = subCuboids[i].CornerAt(AmalgamationMaxExtentsCorner);
			minExtents = new Location(
				Single.Min(minExtents.X, cMin.X),  	
				Single.Min(minExtents.Y, cMin.Y),  	
				Single.Min(minExtents.Z, cMin.Z)  	
			);
			maxExtents = new Location(
				Single.Max(maxExtents.X, cMax.X),  	
				Single.Max(maxExtents.Y, cMax.Y),  	
				Single.Max(maxExtents.Z, cMax.Z)  	
			);
		}
		
		return new PositionedCuboid(
			maxExtents.X - minExtents.X,	
			maxExtents.Y - minExtents.Y,	
			maxExtents.Z - minExtents.Z,
			minExtents + minExtents.VectTo(maxExtents).ScaledBy(0.5f)
		);
	}
	/// <inheritdoc cref="FromSmallestEnclosingCuboid(ReadOnlySpan{PositionedCuboid})" />
	/// <typeparam name="TCuboidList">The type of the list of cuboids.</typeparam>
	public static PositionedCuboid FromSmallestEnclosingCuboid<TCuboidList>(TCuboidList subCuboids) where TCuboidList : IReadOnlyList<PositionedCuboid> {
		if (subCuboids.Count <= 0) return new(0f, Location.Origin);
		
		var minExtents = subCuboids[0].CornerAt(AmalgamationMinExtentsCorner);
		var maxExtents = subCuboids[0].CornerAt(AmalgamationMaxExtentsCorner);
		
		for (var i = 1; i < subCuboids.Count; ++i) {
			var cMin = subCuboids[i].CornerAt(AmalgamationMinExtentsCorner);
			var cMax = subCuboids[i].CornerAt(AmalgamationMaxExtentsCorner);
			minExtents = new Location(
				Single.Min(minExtents.X, cMin.X),  	
				Single.Min(minExtents.Y, cMin.Y),  	
				Single.Min(minExtents.Z, cMin.Z)  	
			);
			maxExtents = new Location(
				Single.Max(maxExtents.X, cMax.X),  	
				Single.Max(maxExtents.Y, cMax.Y),  	
				Single.Max(maxExtents.Z, cMax.Z)  	
			);
		}
		
		return new PositionedCuboid(
			maxExtents.X - minExtents.X,	
			maxExtents.Y - minExtents.Y,	
			maxExtents.Z - minExtents.Z,
			minExtents + minExtents.VectTo(maxExtents).ScaledBy(0.5f)
		);
	}
	/// <inheritdoc cref="FromSmallestEnclosingCuboid(ReadOnlySpan{PositionedCuboid})" />
	public static PositionedCuboid FromSmallestEnclosingCuboid(params ReadOnlySpan<PositionedRotatedCuboid> subCuboids) {
		if (subCuboids.Length <= 0) return new(0f, Location.Origin);
		
		var minExtents = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
		var maxExtents = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
		
		for (var i = 0; i < subCuboids.Length; ++i) {
			var corners = subCuboids[i].Corners;
			for (var c = 0; c < corners.Count; ++c) {
				var corner = corners[c];
				minExtents = new Location(
					Single.Min(minExtents.X, corner.X),  	
					Single.Min(minExtents.Y, corner.Y),  	
					Single.Min(minExtents.Z, corner.Z)  	
				);
				maxExtents = new Location(
					Single.Max(maxExtents.X, corner.X),  	
					Single.Max(maxExtents.Y, corner.Y),  	
					Single.Max(maxExtents.Z, corner.Z)  	
				);
			}
		}
		
		return new PositionedCuboid(
			maxExtents.X - minExtents.X,	
			maxExtents.Y - minExtents.Y,	
			maxExtents.Z - minExtents.Z,
			minExtents + minExtents.VectTo(maxExtents).ScaledBy(0.5f)
		);
	}
	/// <inheritdoc cref="FromSmallestEnclosingCuboid(ReadOnlySpan{PositionedCuboid})" />
	/// <typeparam name="TCuboidList">The type of the list of cuboids.</typeparam>
	// ReSharper disable once MethodOverloadWithOptionalParameter
	public static PositionedCuboid FromSmallestEnclosingCuboid<TCuboidList>(TCuboidList subCuboids, MethodOverloadStub ignoreMe = default) where TCuboidList : IReadOnlyList<PositionedRotatedCuboid> {
		if (subCuboids.Count <= 0) return new(0f, Location.Origin);
		
		var minExtents = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
		var maxExtents = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
		
		for (var i = 0; i < subCuboids.Count; ++i) {
			var corners = subCuboids[i].Corners;
			for (var c = 0; c < corners.Count; ++c) {
				var corner = corners[c];
				minExtents = new Location(
					Single.Min(minExtents.X, corner.X),  	
					Single.Min(minExtents.Y, corner.Y),  	
					Single.Min(minExtents.Z, corner.Z)  	
				);
				maxExtents = new Location(
					Single.Max(maxExtents.X, corner.X),  	
					Single.Max(maxExtents.Y, corner.Y),  	
					Single.Max(maxExtents.Z, corner.Z)  	
				);
			}
		}
		
		return new PositionedCuboid(
			maxExtents.X - minExtents.X,	
			maxExtents.Y - minExtents.Y,	
			maxExtents.Z - minExtents.Z,
			minExtents + minExtents.VectTo(maxExtents).ScaledBy(0.5f)
		);
	}
	/// <summary>
	/// Returns the smallest axis-aligned <see cref="PositionedCuboid"/> that can fully enclose <paramref name="rotatable"/> in any rotation.
	/// </summary>
	/// <param name="rotatable">The cuboid to find the enclosing axis-aligned cuboid for.</param>
	public static PositionedCuboid FromSmallestEnclosingAxisAligned(PositionedCuboid rotatable) => rotatable.SmallestEnclosingSphere.SmallestEnclosingCube;
	/// <inheritdoc cref="FromSmallestEnclosingAxisAligned(PositionedCuboid)" />
	public static PositionedCuboid FromSmallestEnclosingAxisAligned(PositionedRotatedCuboid rotatable) => rotatable.SmallestEnclosingSphere.SmallestEnclosingCube;

	/// <summary>
	/// Calculates the smallest axis-aligned <see cref="PositionedCuboid"/> that encloses all of <paramref name="vertices"/>, expanded by <paramref name="additionalMargin"/> on every side.
	/// </summary>
	/// <typeparam name="TVertex">The vertex type.</typeparam>
	/// <param name="vertices">The vertices to enclose.</param>
	/// <param name="additionalMargin">An extra amount to add to every half-extent of the resultant cuboid (see <see cref="ICuboid{TSelf}.WithAllExtentsAdjustedBy"/>).</param>
	public static PositionedCuboid FromBoundingBoxCalculation<TVertex>(ReadOnlySpan<TVertex> vertices, float additionalMargin) where TVertex : IMeshVertex {
		return FromBoundingBoxCalculation(vertices).WithAllExtentsAdjustedBy(additionalMargin);
	}
	/// <summary>
	/// Calculates the smallest axis-aligned <see cref="PositionedCuboid"/> that encloses all of <paramref name="vertices"/>.
	/// </summary>
	/// <typeparam name="TVertex">The vertex type.</typeparam>
	/// <param name="vertices">The vertices to enclose. If empty, <see cref="UnitCubeAtOrigin"/> is returned.</param>
	public static PositionedCuboid FromBoundingBoxCalculation<TVertex>(params ReadOnlySpan<TVertex> vertices) where TVertex : IMeshVertex {
		if (vertices.Length == 0) return PositionedCuboid.UnitCubeAtOrigin;
		
		var (minX, minY, minZ) = vertices[0].Location;
		var (maxX, maxY, maxZ) = vertices[0].Location;
		for (var i = 1; i < vertices.Length; ++i) {
			var loc = vertices[i].Location;
			if (loc.X < minX) minX = loc.X;
			if (loc.Y < minY) minY = loc.Y;
			if (loc.Z < minZ) minZ = loc.Z;
			if (loc.X > maxX) maxX = loc.X;
			if (loc.Y > maxY) maxY = loc.Y;
			if (loc.Z > maxZ) maxZ = loc.Z;
		}
		
		return new(
			maxX - minX,
			maxY - minY,
			maxZ - minZ,
			new Vect(minX + maxX, minY + maxY, minZ + maxZ).ScaledBy(0.5f).AsLocation()
		);
	}
	
	/// <inheritdoc cref="FromBoundingBoxCalculation{TVertex}(ReadOnlySpan{TVertex},float)" />
	public static PositionedCuboid FromBoundingBoxCalculation(ReadOnlySpan<Location> vertices, float additionalMargin) {
		return FromBoundingBoxCalculation(vertices).WithAllExtentsAdjustedBy(additionalMargin);
	}
	/// <inheritdoc cref="FromBoundingBoxCalculation{TVertex}(ReadOnlySpan{TVertex})" />
	public static PositionedCuboid FromBoundingBoxCalculation(params ReadOnlySpan<Location> vertices) {
		if (vertices.Length == 0) return UnitCubeAtOrigin;
		
		var (minX, minY, minZ) = vertices[0];
		var (maxX, maxY, maxZ) = vertices[0];
		for (var i = 1; i < vertices.Length; ++i) {
			var loc = vertices[i];
			if (loc.X < minX) minX = loc.X;
			if (loc.Y < minY) minY = loc.Y;
			if (loc.Z < minZ) minZ = loc.Z;
			if (loc.X > maxX) maxX = loc.X;
			if (loc.Y > maxY) maxY = loc.Y;
			if (loc.Z > maxZ) maxZ = loc.Z;
		}
		
		return new(
			maxX - minX,
			maxY - minY,
			maxZ - minZ,
			new Vect(minX + maxX, minY + maxY, minZ + maxZ).ScaledBy(0.5f).AsLocation()
		);
	}
	/// <summary>
	/// Constructs the axis-aligned <see cref="PositionedCuboid"/> whose two opposite corners are <paramref name="cornerA"/> and <paramref name="cornerB"/>.
	/// </summary>
	/// <param name="cornerA">One corner of the cuboid.</param>
	/// <param name="cornerB">The corner diagonally opposite <paramref name="cornerA"/>.</param>
	public static PositionedCuboid FromOppositeCorners(Location cornerA, Location cornerB) {
		var boundedRay = new BoundedRay(cornerA, cornerB);
		var extentsVect = boundedRay.StartToEndVect.Absolute;
		return new PositionedCuboid(extentsVect.X, extentsVect.Y, extentsVect.Z, boundedRay.MiddlePoint);
	}

	/// <summary>
	/// Converts this shape to an unpositioned <see cref="Cuboid"/>, discarding <see cref="Position"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid ToStandardCuboid() => _impl.BaseShape;
	/// <summary>
	/// Rotates this cuboid by <paramref name="rotation"/>; equivalent to <see cref="ToTranslatedRotatedCuboid(Rotation)"/>.
	/// </summary>
	/// <param name="rotation">The rotation for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid WithRotation(Rotation rotation) => ToTranslatedRotatedCuboid(rotation);
	/// <summary>
	/// Converts this cuboid to a <see cref="PositionedRotatedCuboid"/>, additionally rotated by <paramref name="rotation"/>.
	/// </summary>
	/// <param name="rotation">The rotation for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid ToTranslatedRotatedCuboid(Rotation rotation) => new(_impl.BaseShape, Position, rotation);

	/// <summary>
	/// The smallest <see cref="PositionedSphere"/>, sharing this cuboid's <see cref="Position"/>, that fully encloses it.
	/// </summary>
	public PositionedSphere SmallestEnclosingSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.SmallestEnclosingSphere, Position);
	}
	/// <summary>
	/// The largest <see cref="PositionedSphere"/>, sharing this cuboid's <see cref="Position"/>, that fits entirely within it.
	/// </summary>
	public PositionedSphere LargestEnclosedSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.LargestEnclosedSphere, Position);
	}

	/// <summary>
	/// Calculates the distance between this cuboid and <paramref name="sphere"/>.
	/// </summary>
	/// <param name="sphere">The sphere to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(PositionedSphere sphere) => sphere.DistanceFrom(this);
	float IDistanceMeasurable<PositionedSphere>.DistanceSquaredFrom(PositionedSphere sphere) { var dist = DistanceFrom(sphere); return dist * dist; }
	/// <summary>
	/// Calculates the distance between this cuboid and <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The other cuboid to measure against.</param>
	public float DistanceFrom(PositionedCuboid cuboid) {
		var d = Position - cuboid.Position;
		var dx = MathF.Max(0f, MathF.Abs(d.X) - HalfWidth - cuboid.HalfWidth);
		var dy = MathF.Max(0f, MathF.Abs(d.Y) - HalfHeight - cuboid.HalfHeight);
		var dz = MathF.Max(0f, MathF.Abs(d.Z) - HalfDepth - cuboid.HalfDepth);
		return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
	}
	float IDistanceMeasurable<PositionedCuboid>.DistanceSquaredFrom(PositionedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	/// <summary>
	/// Calculates the distance between this cuboid and <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The other cuboid to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(PositionedRotatedCuboid cuboid) => cuboid.DistanceFrom(this);
	float IDistanceMeasurable<PositionedRotatedCuboid>.DistanceSquaredFrom(PositionedRotatedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	/// <summary>
	/// Determines whether this cuboid intersects <paramref name="sphere"/>.
	/// </summary>
	/// <param name="sphere">The sphere to test against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(PositionedSphere sphere) => sphere.IsIntersectedBy(this);
	/// <summary>
	/// Determines whether this cuboid intersects <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The other cuboid to test against.</param>
	public bool IsIntersectedBy(PositionedCuboid cuboid) {
		var d = Position - cuboid.Position;
		return MathF.Abs(d.X) < HalfWidth + cuboid.HalfWidth
			&& MathF.Abs(d.Y) < HalfHeight + cuboid.HalfHeight
			&& MathF.Abs(d.Z) < HalfDepth + cuboid.HalfDepth;
	}
	/// <summary>
	/// Determines whether this cuboid intersects <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The other cuboid to test against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(PositionedRotatedCuboid cuboid) => cuboid.IsIntersectedBy(this);

	#region Deferring Members
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid MovedBy(Vect v) => _impl.MovedBy(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	/// <summary>
	/// Returns this cuboid with its base extents scaled along the cardinal axes by the individual components given in <paramref name="v"/>.
	/// </summary>
	/// <param name="v">The scaling vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid ScaledBy(Vect v) => new(_impl.BaseShape.ScaledBy(v), Position);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid Clamp(PositionedCuboid min, PositionedCuboid max) => _impl.Clamp(min, max);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedCuboid other && Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedCuboid other) => _impl.Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedCuboid other, float tolerance) => _impl.Equals(other, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Location location) => _impl.PointClosestTo(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Location location) => _impl.DistanceFrom(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Location location) => _impl.DistanceSquaredFrom(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(Location location) => _impl.Contains(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray? ReflectionOf(Ray ray) => _impl.ReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray FastReflectionOf(Ray ray) => _impl.FastReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(Ray ray) => _impl.IncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(Ray ray) => _impl.FastIncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay? ReflectionOf(BoundedRay ray) => _impl.ReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay FastReflectionOf(BoundedRay ray) => _impl.FastReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(BoundedRay ray) => _impl.IncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(BoundedRay ray) => _impl.FastIncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Line line) => _impl.ClosestPointOn(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Ray ray) => _impl.ClosestPointOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(BoundedRay ray) => _impl.ClosestPointOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Line line) => _impl.PointClosestTo(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Ray ray) => _impl.PointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(BoundedRay ray) => _impl.PointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Line line) => _impl.DistanceFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Line line) => _impl.DistanceSquaredFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Ray ray) => _impl.DistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Ray ray) => _impl.DistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(BoundedRay ray) => _impl.DistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(BoundedRay ray) => _impl.DistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(BoundedRay ray) => _impl.Contains(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Line line) => _impl.IsIntersectedBy(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Ray ray) => _impl.IsIntersectedBy(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(BoundedRay ray) => _impl.IsIntersectedBy(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Line line) => _impl.IntersectionWith(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Line line) => _impl.FastIntersectionWith(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => _impl.IntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) => _impl.FastIntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => _impl.IntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) => _impl.FastIntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Plane plane) => _impl.DistanceFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Plane plane) => _impl.DistanceSquaredFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SignedDistanceFrom(Plane plane) => _impl.SignedDistanceFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Plane plane) => _impl.PointClosestTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Plane plane) => _impl.ClosestPointOn(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PlaneObjectRelationship RelationshipTo(Plane plane) => _impl.RelationshipTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Location point) => _impl.SurfacePointClosestTo(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Location point) => _impl.SurfaceDistanceFrom(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Location point) => _impl.SurfaceDistanceSquaredFrom(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Line line) => _impl.SurfacePointClosestTo(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Line line) => _impl.ClosestPointToSurfaceOn(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Line line) => _impl.SurfaceDistanceFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Line line) => _impl.SurfaceDistanceSquaredFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Ray ray) => _impl.SurfacePointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Ray ray) => _impl.ClosestPointToSurfaceOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Ray ray) => _impl.SurfaceDistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Ray ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(BoundedRay ray) => _impl.SurfacePointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(BoundedRay ray) => _impl.ClosestPointToSurfaceOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(BoundedRay ray) => _impl.SurfaceDistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(BoundedRay ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Plane plane) => _impl.SurfacePointClosestTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Plane plane) => _impl.ClosestPointToSurfaceOn(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Parse(string s, IFormatProvider? provider) => TranslatedConvexShape<Cuboid>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedCuboid result) {
		var returnVal = TranslatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedConvexShape<Cuboid>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedCuboid result) {
		var returnVal = TranslatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedCuboid src) => TranslatedConvexShape<Cuboid>.SerializeToBytes(dest, src);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedConvexShape<Cuboid>.DeserializeFromBytes(src);
	/// <inheritdoc/>
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedConvexShape<Cuboid>.SerializationByteSpanLength;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedCuboid left, PositionedCuboid right) => left._impl == right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedCuboid left, PositionedCuboid right) => left._impl != right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Random() => TranslatedConvexShape<Cuboid>.Random();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator *(PositionedCuboid left, float right) => left._impl * right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator /(PositionedCuboid left, float right) => left._impl / right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator *(float left, PositionedCuboid right) => left * right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Random(PositionedCuboid minInclusive, PositionedCuboid maxExclusive) => TranslatedConvexShape<Cuboid>.Random(minInclusive, maxExclusive);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Interpolate(PositionedCuboid start, PositionedCuboid end, float distance) => TranslatedConvexShape<Cuboid>.Interpolate(start, end, distance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator +(PositionedCuboid left, Vect right) => left._impl + right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator -(PositionedCuboid left, Vect right) => left._impl - right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator +(Vect left, PositionedCuboid right) => left + right._impl;
	Location IConvexShape.GetRandomInternalLocation() => ((IConvexShape) _impl).GetRandomInternalLocation();
	#endregion
}

/// <summary>
/// Extension methods relating to <see cref="PositionedCuboid"/>.
/// </summary>
public static class PositionedCuboidExtensions {
	/// <summary>
	/// Calculates the smallest axis-aligned <see cref="PositionedCuboid"/> that encloses the bounding boxes of every mesh in <paramref name="this"/>.
	/// </summary>
	/// <param name="this">The collection of meshes to enclose.</param>
	public static unsafe PositionedCuboid CalculateCombinedBoundingBox<TKey>(this IndirectEnumerable<TKey, Mesh> @this) {
		static IndirectEnumerable<IndirectEnumerable<TKey, Mesh>, PositionedCuboid> Map(IndirectEnumerable<TKey, Mesh> input) {
			static int GetCount(IndirectEnumerable<TKey, Mesh> i) => i.Count;
			static int GetVersion(IndirectEnumerable<TKey, Mesh> _) => 0;
			static PositionedCuboid GetItem(IndirectEnumerable<TKey, Mesh> i, int index) => i[index].BoundingBox;
			
			return new(
				input,
				0,
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
		
		return PositionedCuboid.FromSmallestEnclosingCuboid(Map(@this));
	}
	
	/// <summary>
	/// Calculates the smallest axis-aligned <see cref="PositionedCuboid"/> that encloses the bounding boxes of every model instance's mesh in <paramref name="this"/>.
	/// </summary>
	/// <param name="this">The collection of model instances to enclose.</param>
	public static unsafe PositionedCuboid CalculateCombinedBoundingBox<TKey>(this IndirectEnumerable<TKey, ModelInstance> @this) {
		static IndirectEnumerable<IndirectEnumerable<TKey, ModelInstance>, PositionedCuboid> Map(IndirectEnumerable<TKey, ModelInstance> input) {
			static int GetCount(IndirectEnumerable<TKey, ModelInstance> i) => i.Count;
			static int GetVersion(IndirectEnumerable<TKey, ModelInstance> _) => 0;
			static PositionedCuboid GetItem(IndirectEnumerable<TKey, ModelInstance> i, int index) => i[index].Mesh.BoundingBox;
			
			return new(
				input,
				0,
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
		
		return PositionedCuboid.FromSmallestEnclosingCuboid(Map(@this));
	}
	
	/// <summary>
	/// Calculates the smallest axis-aligned <see cref="PositionedCuboid"/> that encloses the bounding boxes of every model's mesh in <paramref name="this"/>.
	/// </summary>
	/// <param name="this">The collection of models to enclose.</param>
	public static unsafe PositionedCuboid CalculateCombinedBoundingBox<TKey>(this IndirectEnumerable<TKey, Model> @this) {
		static IndirectEnumerable<IndirectEnumerable<TKey, Model>, PositionedCuboid> Map(IndirectEnumerable<TKey, Model> input) {
			static int GetCount(IndirectEnumerable<TKey, Model> i) => i.Count;
			static int GetVersion(IndirectEnumerable<TKey, Model> _) => 0;
			static PositionedCuboid GetItem(IndirectEnumerable<TKey, Model> i, int index) => i[index].Mesh.BoundingBox;
			
			return new(
				input,
				0,
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
		
		return PositionedCuboid.FromSmallestEnclosingCuboid(Map(@this));
	}
}