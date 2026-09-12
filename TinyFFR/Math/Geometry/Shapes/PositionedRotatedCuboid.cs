// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a <see cref="Cuboid"/> positioned at a specific <see cref="Position"/> and turned by a specific <see cref="Rotation"/> in world space.
/// </summary>
public readonly struct PositionedRotatedCuboid : ITranslatedRotatedConvexShape<PositionedRotatedCuboid, Cuboid>, ICuboid<PositionedRotatedCuboid>,
	IDistanceMeasurable<PositionedSphere>, IDistanceMeasurable<PositionedCuboid>, IDistanceMeasurable<PositionedRotatedCuboid>,
	IIntersectable<PositionedSphere>, IIntersectable<PositionedCuboid>, IIntersectable<PositionedRotatedCuboid> {
	/// <summary>
	/// A <see cref="Cuboid.UnitCube"/> positioned at <see cref="Location.Origin"/> with no rotation.
	/// </summary>
	public static readonly PositionedRotatedCuboid UnitCubeAtOriginUnrotated = new(Cuboid.UnitCube, Location.Origin, Rotation.None);
	readonly TranslatedRotatedConvexShape<Cuboid> _impl;

	/// <summary>
	/// The centre point of this cuboid.
	/// </summary>
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
	}

	/// <summary>
	/// The rotation applied to this cuboid, around its own <see cref="Position"/>.
	/// </summary>
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Rotation = value };
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
	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Location> Corners => new(this, GetIteratorVersion(this), &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(PositionedRotatedCuboid _) => 8;
	static Location GetCornerForEnumerator(PositionedRotatedCuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedRotatedCuboid, BoundedRay> Edges => new(this, GetIteratorVersion(this), &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(PositionedRotatedCuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(PositionedRotatedCuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Plane> Sides => new(this, GetIteratorVersion(this), &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(PositionedRotatedCuboid _) => 6;
	static Plane GetSideForEnumerator(PositionedRotatedCuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Location> Centroids => new(this, GetIteratorVersion(this), &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(PositionedRotatedCuboid _) => 6;
	static Location GetCentroidForEnumerator(PositionedRotatedCuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(PositionedRotatedCuboid _) => 0;

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
	public PositionedRotatedCuboid WithVolume(float newVolume) => new(_impl.BaseShape.WithVolume(newVolume), Position, Rotation);
	/// <inheritdoc />
	public PositionedRotatedCuboid WithSurfaceArea(float newSurfaceArea) => new(_impl.BaseShape.WithSurfaceArea(newSurfaceArea), Position, Rotation);
	/// <inheritdoc />
	public PositionedRotatedCuboid WithAllExtentsAdjustedBy(float adjustment) => new(_impl.BaseShape.WithAllExtentsAdjustedBy(adjustment), Position, Rotation);

	Cuboid ITranslatedRotatedShape<PositionedRotatedCuboid, Cuboid>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}

	Vect ITranslatedShape.Translation {
		get => Position.AsVect();
		init => Position = value.AsLocation();
	}

	/// <summary>
	/// Constructs a new <see cref="PositionedRotatedCuboid"/> with the given dimensions, positioned at <paramref name="centerPoint"/> and turned by <paramref name="rotation"/>.
	/// </summary>
	/// <param name="width">The size of the cuboid on the X axis (before rotation).</param>
	/// <param name="height">The size of the cuboid on the Y axis (before rotation).</param>
	/// <param name="depth">The size of the cuboid on the Z axis (before rotation).</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	/// <param name="rotation">The rotation of the resultant shape, around <paramref name="centerPoint"/>.</param>
	public PositionedRotatedCuboid(float width, float height, float depth, Location centerPoint, Rotation rotation) : this(new Cuboid(width, height, depth), centerPoint, rotation) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedRotatedCuboid"/> from an existing <see cref="Cuboid"/>, positioned at <paramref name="centerPoint"/> and turned by <paramref name="rotation"/>.
	/// </summary>
	/// <param name="baseShape">The unpositioned, unrotated cuboid.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	/// <param name="rotation">The rotation of the resultant shape, around <paramref name="centerPoint"/>.</param>
	public PositionedRotatedCuboid(Cuboid baseShape, Location centerPoint, Rotation rotation) : this(new(baseShape, centerPoint.AsVect(), rotation)) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedRotatedCuboid"/> directly from its underlying <see cref="TranslatedRotatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="impl">The underlying translated/rotated shape.</param>
	public PositionedRotatedCuboid(TranslatedRotatedConvexShape<Cuboid> impl) {
		_impl = impl;
	}

	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedRotatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedConvexShape<Cuboid>(PositionedRotatedCuboid operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedRotatedCuboid"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedRotatedCuboid(TranslatedRotatedConvexShape<Cuboid> operand) => new(operand);
	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedRotatedShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedShape<Cuboid>(PositionedRotatedCuboid operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedRotatedCuboid"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedRotatedCuboid(TranslatedRotatedShape<Cuboid> operand) => new(operand);

	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedConvexShape{TShape}"/> representation, discarding <see cref="Rotation"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedConvexShape<Cuboid>(PositionedRotatedCuboid operand) => (TranslatedConvexShape<Cuboid>) operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedShape{TShape}"/> representation, discarding <see cref="Rotation"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedShape<Cuboid>(PositionedRotatedCuboid operand) => (TranslatedShape<Cuboid>) operand._impl;

	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedCuboid"/>, discarding <see cref="Rotation"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator PositionedCuboid(PositionedRotatedCuboid operand) => new((TranslatedConvexShape<Cuboid>) operand._impl);
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedRotatedCuboid"/> with <see cref="Rotation.None"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedRotatedCuboid(PositionedCuboid operand) => new(operand.ToStandardCuboid(), operand.Position, Rotation.None);

	/// <summary>
	/// Constructs a new <see cref="PositionedRotatedCuboid"/> from its half-dimensions, positioned at <paramref name="centerPoint"/> and turned by <paramref name="rotation"/>.
	/// </summary>
	/// <param name="halfWidth">Half of the desired <see cref="Width"/> (before rotation).</param>
	/// <param name="halfHeight">Half of the desired <see cref="Height"/> (before rotation).</param>
	/// <param name="halfDepth">Half of the desired <see cref="Depth"/> (before rotation).</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	/// <param name="rotation">The rotation of the resultant shape, around <paramref name="centerPoint"/>.</param>
	public static PositionedRotatedCuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth, Location centerPoint, Rotation rotation) => new(Cuboid.FromHalfDimensions(halfWidth, halfHeight, halfDepth), centerPoint, rotation);

	/// <summary>
	/// Converts this shape to an unpositioned, unrotated <see cref="Cuboid"/>, discarding <see cref="Position"/> and <see cref="Rotation"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid ToStandardCuboid() => _impl.BaseShape;
	/// <summary>
	/// Converts this shape to a <see cref="PositionedCuboid"/>, discarding <see cref="Rotation"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedCuboid ToTranslatedCuboid() => new((TranslatedConvexShape<Cuboid>) _impl);

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
	/// The smallest axis-aligned <see cref="PositionedCuboid"/> that fully encloses this cuboid in its current rotation.
	/// </summary>
	public PositionedCuboid SmallestEnclosingNonRotatedCuboid {
		get {
			var corners = Corners;
			var minExtents = corners[0];
			var maxExtents = corners[0];
			for (var i = 1; i < corners.Count; ++i) {
				var corner = corners[i];
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
			return new PositionedCuboid(
				maxExtents.X - minExtents.X,
				maxExtents.Y - minExtents.Y,
				maxExtents.Z - minExtents.Z,
				minExtents + (maxExtents - minExtents).ScaledBy(0.5f)
			);
		}
	}
	
	// https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
	// https://gamedev.stackexchange.com/questions/44500/how-many-and-which-axes-to-use-for-3d-obb-collision-with-sat
	static bool DetectIntersectionViaSeparatingAxisTest(Cuboid a, Cuboid b, Vector3 centerDelta, Vector3 bAxisX, Vector3 bAxisY, Vector3 bAxisZ) {
		static bool AxisProjectionsDoNotOverlap(Cuboid a, Cuboid b, Vector3 centerDelta, Vector3 bx, Vector3 by, Vector3 bz, Vector3 axisToTest) {
			const float MinCrossLengthSquared = 1E-9f;
			if (axisToTest.LengthSquared() < MinCrossLengthSquared) return false; // cross products will be zero when axes are parallel
			var aProjectedLen = a.HalfWidth * MathF.Abs(axisToTest.X) + a.HalfHeight * MathF.Abs(axisToTest.Y) + a.HalfDepth * MathF.Abs(axisToTest.Z);
			var bProjectedLen = b.HalfWidth * MathF.Abs(Vector3.Dot(axisToTest, bx)) + b.HalfHeight * MathF.Abs(Vector3.Dot(axisToTest, by)) + b.HalfDepth * MathF.Abs(Vector3.Dot(axisToTest, bz));
			var centerDeltaProjectedLen = MathF.Abs(Vector3.Dot(centerDelta, axisToTest));
			return centerDeltaProjectedLen >= aProjectedLen + bProjectedLen;
		}
		
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(1f, 0f, 0f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(0f, 1f, 0f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(0f, 0f, 1f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisX)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisY)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisZ)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisZ))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisZ))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisZ))) return false;
		return true;
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
		if (IsIntersectedBy(cuboid)) return 0f;
		var min = Single.PositiveInfinity;
		foreach (var corner in Corners) min = MathF.Min(min, cuboid.DistanceFrom(corner));
		foreach (var corner in cuboid.Corners) min = MathF.Min(min, DistanceFrom(corner));
		foreach (var ea in Edges) {
			foreach (var eb in cuboid.Edges) min = MathF.Min(min, ea.DistanceFrom(eb));
		}
		return min;
	}
	float IDistanceMeasurable<PositionedCuboid>.DistanceSquaredFrom(PositionedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	/// <inheritdoc cref="DistanceFrom(PositionedCuboid)" />
	public float DistanceFrom(PositionedRotatedCuboid cuboid) {
		if (IsIntersectedBy(cuboid)) return 0f;
		var min = Single.PositiveInfinity;
		foreach (var corner in Corners) min = MathF.Min(min, cuboid.DistanceFrom(corner));
		foreach (var corner in cuboid.Corners) min = MathF.Min(min, DistanceFrom(corner));
		foreach (var ea in Edges) {
			foreach (var eb in cuboid.Edges) min = MathF.Min(min, ea.DistanceFrom(eb));
		}
		return min;
	}
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
		var reverseRot = Rotation.Reversed;

		return DetectIntersectionViaSeparatingAxisTest(
			_impl.BaseShape,
			cuboid.ToStandardCuboid(),
			((cuboid.Position - Position) * reverseRot).ToVector3(),
			Direction.Left.RotatedBy(reverseRot).ToVector3(),
			Direction.Up.RotatedBy(reverseRot).ToVector3(),
			Direction.Forward.RotatedBy(reverseRot).ToVector3()
		);
	}
	/// <inheritdoc cref="IsIntersectedBy(PositionedCuboid)" />
	public bool IsIntersectedBy(PositionedRotatedCuboid cuboid) {
		var reverseRot = Rotation.Reversed;

		return DetectIntersectionViaSeparatingAxisTest(
			_impl.BaseShape,
			cuboid.ToStandardCuboid(),
			((cuboid.Position - Position) * reverseRot).ToVector3(),
			Direction.Left.RotatedBy(cuboid.Rotation + reverseRot).ToVector3(),
			Direction.Up.RotatedBy(cuboid.Rotation + reverseRot).ToVector3(),
			Direction.Forward.RotatedBy(cuboid.Rotation + reverseRot).ToVector3()
		);
	}

	#region Deferring Members
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid MovedBy(Vect v) => _impl.MovedBy(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	/// <summary>
	/// Returns this cuboid with its base extents scaled along the cardinal axes by the individual components given in <paramref name="v"/>.
	/// </summary>
	/// <param name="v">The scaling vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid ScaledBy(Vect v) => new PositionedRotatedCuboid(_impl.BaseShape.ScaledBy(v), Position, Rotation);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid Clamp(PositionedRotatedCuboid min, PositionedRotatedCuboid max) => _impl.Clamp(min, max);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedRotatedCuboid other && Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedRotatedCuboid other) => _impl.Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedRotatedCuboid other, float tolerance) => _impl.Equals(other, tolerance);
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Parse(string s, IFormatProvider? provider) => TranslatedRotatedConvexShape<Cuboid>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedRotatedCuboid result) {
		var returnVal = TranslatedRotatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedRotatedConvexShape<Cuboid>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedRotatedCuboid result) {
		var returnVal = TranslatedRotatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedRotatedCuboid src) => TranslatedRotatedConvexShape<Cuboid>.SerializeToBytes(dest, src);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedRotatedConvexShape<Cuboid>.DeserializeFromBytes(src);
	/// <inheritdoc/>
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedRotatedConvexShape<Cuboid>.SerializationByteSpanLength;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedRotatedCuboid left, PositionedRotatedCuboid right) => left._impl == right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedRotatedCuboid left, PositionedRotatedCuboid right) => left._impl != right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Random() => TranslatedRotatedConvexShape<Cuboid>.Random();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(PositionedRotatedCuboid left, float right) => left._impl * right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator /(PositionedRotatedCuboid left, float right) => left._impl / right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(float left, PositionedRotatedCuboid right) => left * right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Random(PositionedRotatedCuboid minInclusive, PositionedRotatedCuboid maxExclusive) => TranslatedRotatedConvexShape<Cuboid>.Random(minInclusive, maxExclusive);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Interpolate(PositionedRotatedCuboid start, PositionedRotatedCuboid end, float distance) => TranslatedRotatedConvexShape<Cuboid>.Interpolate(start, end, distance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator +(PositionedRotatedCuboid left, Vect right) => left._impl + right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator -(PositionedRotatedCuboid left, Vect right) => left._impl - right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator +(Vect left, PositionedRotatedCuboid right) => left + right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(PositionedRotatedCuboid left, Rotation right) => left._impl * right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(Rotation left, PositionedRotatedCuboid right) => left * right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid RotatedBy(Rotation rot) => _impl.RotatedBy(rot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid RotatedBy(Quaternion rotQuat) => _impl.RotatedBy(rotQuat);
	Location IConvexShape.GetRandomInternalLocation() => ((IConvexShape) _impl).GetRandomInternalLocation();
	#endregion
}