// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a single direction in 3D space (e.g. "Left", "Forward", "None", etc).
/// </summary>
/// <remarks>
/// <para>
/// Direction is a specialized <see cref="IVect"/> that maintains itself as being unit-length (or <see cref="None"/>) at all times.
/// </para>
/// <para>
/// Constructing a direction from direct component values (e.g. <c>new Direction(0.707f, 0f, 0.707f)</c>) is rarely useful;
/// most of the time you'll want to construct a direction from a specific operation:
/// <ul>
/// <li>Find a Direction that's orthogonal (perpendicular) to two others: <c>FromDualOrthogonalization(...)</c></li>
/// <li>Find the nearest direction in a set: <c>FromNearestDirectionInSpan(...)</c></li>
/// <li>Rotate another direction: <c>dir.RotatedBy(...)</c></li>
/// <li>Get a random direction: <c>Random(...)</c></li>
/// <li>...Plus a lot of mathematical functions (e.g. operations on planes, rays, spheres, etc)</li>
/// </ul>
/// </para>
/// </remarks>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)]
public readonly partial struct Direction : IVect<Direction>, IDescriptiveStringProvider {
	internal const float WValue = 0f;
	/// <summary>
	/// A special-case non-existent direction, with all components equal to <c>0f</c>.
	/// </summary>
	/// <remarks>
	/// None is a valid input and output to/from many built-in functions. However, read the XMLDoc for any given function
	/// to check that it gracefully handles None as there are some exceptions. 
	/// </remarks>
	public static readonly Direction None = new();
	/// <summary>
	/// The direction with components <c>(0f, 0f, 1f)</c>.
	/// </summary>
	public static readonly Direction Forward = new(0f, 0f, 1f);
	/// <summary>
	/// The direction with components <c>(0f, 0f, -1f)</c>.
	/// </summary>
	public static readonly Direction Backward = new(0f, 0f, -1f);
	/// <summary>
	/// The direction with components <c>(0f, 1f, 0f)</c>.
	/// </summary>
	public static readonly Direction Up = new(0f, 1f, 0f);
	/// <summary>
	/// The direction with components <c>(0f, -1f, 0f)</c>.
	/// </summary>
	public static readonly Direction Down = new(0f, -1f, 0f);
	/// <summary>
	/// The direction with components <c>(1f, 0f, 0f)</c>.
	/// </summary>
	public static readonly Direction Left = new(1f, 0f, 0f);
	/// <summary>
	/// The direction with components <c>(-1f, 0f, 0f)</c>.
	/// </summary>
	public static readonly Direction Right = new(-1f, 0f, 0f);
	static readonly Direction[] _allCardinals = {
		new(1, 0, 0),   new(0, 1, 0),   new(0, 0, 1),
		new(-1, 0, 0),  new(0, -1, 0),  new(0, 0, -1),
	};
	static readonly Direction[] _allIntercardinals = {
		new(1, 1, 0),   new(1, 0, 1),   new(0, 1, 1),
		new(-1, -1, 0), new(-1, 0, -1), new(0, -1, -1),
		new(-1, 1, 0),  new(-1, 0, 1),	new(0, -1, 1),
		new(1, -1, 0),  new(1, 0, -1),	new(0, 1, -1),
	};
	static readonly Direction[] _allDiagonals = {
		new(-1, 1, 1),  new(1, -1, 1),  new(1, 1, -1),
		new(1, -1, -1), new(-1, 1, -1), new(-1, -1, 1),
		new(1, 1, 1),   new(-1, -1, -1)
	};
	static readonly Direction[] _allOrientations = {
		_allCardinals[0],
		_allCardinals[1],
		_allCardinals[2],
		_allCardinals[3],
		_allCardinals[4],
		_allCardinals[5],
		new(1, 1, 0),   new(0, 1, 1),   new(1, 0, 1),
		new(-1, -1, 0), new(0, -1, -1),	new(-1, 0, -1),
		new(1, -1, 0),  new(0, 1, -1),	new(1, 0, -1),
		new(-1, 1, 0),	new(0, -1, 1),	new(-1, 0, 1),
		_allDiagonals[0],
		_allDiagonals[1],
		_allDiagonals[2],
		_allDiagonals[3],
		_allDiagonals[4],
		_allDiagonals[5],
		_allDiagonals[6],
		_allDiagonals[7],
	};

	/// <summary>
	/// The six axis-aligned directions: <see cref="Left"/>, <see cref="Right"/>, <see cref="Up"/>, <see cref="Down"/>, <see cref="Forward"/>, and <see cref="Backward"/>.
	/// </summary>
	public static ReadOnlySpan<Direction> AllCardinals => _allCardinals;
	/// <summary>
	/// The twelve directions that lie exactly between two adjacent cardinal directions (e.g. exactly between <see cref="Up"/> and <see cref="Left"/>).
	/// </summary>
	public static ReadOnlySpan<Direction> AllIntercardinals => _allIntercardinals;
	/// <summary>
	/// The eight directions that lie exactly between three adjacent cardinal directions (e.g. exactly between <see cref="Up"/>, <see cref="Left"/>, and <see cref="Forward"/>).
	/// </summary>
	public static ReadOnlySpan<Direction> AllDiagonals => _allDiagonals;
	/// <summary>
	/// The union of <see cref="AllCardinals"/>, <see cref="AllIntercardinals"/>, and <see cref="AllDiagonals"/> (26 directions in total).
	/// </summary>
	public static ReadOnlySpan<Direction> AllOrientations => _allOrientations;

	internal readonly Vector4 AsVector4;

	/* No init accessor on these properties. It's not intuitive that Direction tries to keep itself normalized, so
	 * setting e.g. new Direction(1f, 2f, 3f) with { X = 4f, Y = 5f } is very hard to reason about.
	 */
	/// <inheritdoc />
	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
	}
	/// <inheritdoc />
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
	}
	/// <inheritdoc />
	public float Z {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
	}

	/// <inheritdoc />
	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	/// <inheritdoc />
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	/// <inheritdoc />
	public Direction this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	/// <summary>
	/// Constructs a new <see cref="Direction"/> equal to <see cref="None"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction() : this(0f, 0f, 0f) { }
	/// <summary>
	/// Constructs a new <see cref="Direction"/> from the given <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/> components.
	/// The given components are normalized to unit length as part of construction; meaning their ratio is preserved but the actual resultant <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/>
	/// properties will be normalized.
	/// </summary>
	/// <remarks>
	/// If all three components are <c>0f</c> (or otherwise sum to a zero-length vector), the result is <see cref="None"/>.
	/// If you already know your components are unit-length and want to skip the normalization step, use <see cref="FromVector3PreNormalized(float,float,float)"/> instead.
	/// </remarks>
	/// <param name="x">The X component, prior to normalization.</param>
	/// <param name="y">The Y component, prior to normalization.</param>
	/// <param name="z">The Z component, prior to normalization.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction(float x, float y, float z) : this(NormalizeOrZero(new Vector4(x, y, z, WValue))) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Direction(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	/// <summary>
	/// Constructs a new <see cref="Direction"/> directly from the given already-unit-length <paramref name="v"/>, skipping normalization.
	/// </summary>
	/// <remarks>
	/// This is a faster alternative to <see cref="FromVector3"/> for when you already know the
	/// components describe a unit-length vector.
	/// Be warned that if the components are not actually unit-length, the resultant <see cref="Direction"/> will not behave correctly
	/// in most interactions that use it, potentially causing difficult-to-track mathematical errors throughout your application;
	/// so if in doubt opt to use <see cref="FromVector3"/> instead.
	/// </remarks>
	/// <param name="v">The already-normalized vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(Vector3 v) => new(new Vector4(v, WValue));
	/// <summary>
	/// Constructs a new <see cref="Direction"/> directly from the given already-unit-length components, skipping normalization.
	/// </summary>
	/// <remarks>
	/// This is a faster alternative to the <see cref="Direction(float,float,float)"/> constructor for when you already know the
	/// components describe a unit-length vector.
	/// Be warned that if the components are not actually unit-length, the resultant <see cref="Direction"/> will not behave correctly
	/// in most interactions that use it, potentially causing difficult-to-track mathematical errors throughout your application;
	/// so if in doubt opt to use <see cref="FromVector3"/> instead.
	/// </remarks>
	/// <param name="x">The X component, already normalized.</param>
	/// <param name="y">The Y component, already normalized.</param>
	/// <param name="z">The Z component, already normalized.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(float x, float y, float z) => new(new Vector4(x, y, z, WValue));

	/// <summary>
	/// Converts a compass-style <see cref="Orientation"/> to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="orientation">The orientation to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromOrientation(Orientation orientation) => new(orientation.GetAxisSign(Axis.X), orientation.GetAxisSign(Axis.Y), orientation.GetAxisSign(Axis.Z));

	/// <summary>
	/// Finds a <see cref="Direction"/> that is orthogonal (perpendicular) to both <paramref name="dirA"/> and <paramref name="dirB"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The result follows the right-hand rule: if the index finger of your right hand points along <paramref name="dirA"/>
	/// and your middle finger points along <paramref name="dirB"/>, the result points in the same direction as your thumb.
	/// </para>
	/// <para>
	/// If <paramref name="dirA"/> and <paramref name="dirB"/> are exactly parallel or exactly opposite (and neither is <see cref="None"/>),
	/// there are infinitely many valid orthogonal directions; in that case this method returns an arbitrary but consistent
	/// direction orthogonal to <paramref name="dirA"/> (see <see cref="AnyOrthogonal"/>).
	/// </para>
	/// </remarks>
	/// <param name="dirA">The first direction. If this is <see cref="None"/>, the result is <see cref="None"/>.</param>
	/// <param name="dirB">The second direction. If this is <see cref="None"/>, the result is <see cref="None"/>.</param>
	public static Direction FromDualOrthogonalization(Direction dirA, Direction dirB) {
		const float PreNormalizedCrossLengthSquaredTolerance = 1E-5f;
		const float ParallelInputsCrossLengthSquaredTolerance = 1E-8f;

		var cross = Vector3.Cross(dirA.ToVector3(), dirB.ToVector3());
		var crossLengthSquared = cross.LengthSquared();

		if (MathF.Abs(crossLengthSquared - 1f) <= PreNormalizedCrossLengthSquaredTolerance) return FromVector3PreNormalized(cross);
		else if (crossLengthSquared >= ParallelInputsCrossLengthSquaredTolerance) return FromVector3(cross);
		else if (dirA == None || dirB == None) return None;
		else return dirA.AnyOrthogonal();
	}
	/// <summary>
	/// Finds a <see cref="Direction"/> that is orthogonal (perpendicular) to both <paramref name="dirA"/> and <paramref name="dirB"/>, choosing the handedness of the result.
	/// </summary>
	/// <remarks>
	/// See <see cref="FromDualOrthogonalization(Direction,Direction)"/> for the right-handed behaviour and degenerate-input handling;
	/// passing <see langword="false"/> for <paramref name="rightHanded"/> is equivalent to negating that result.
	/// </remarks>
	/// <param name="dirA">The first direction.</param>
	/// <param name="dirB">The second direction.</param>
	/// <param name="rightHanded">Whether to follow the right-hand rule (<see langword="true"/>) or the left-hand rule (<see langword="false"/>)
	/// when deciding which of the two possible orthogonal directions to return.</param>
	public static Direction FromDualOrthogonalization(Direction dirA, Direction dirB, bool rightHanded) {
		return rightHanded ? FromDualOrthogonalization(dirA, dirB) : FromDualOrthogonalization(dirB, dirA);
	}
	/// <summary>
	/// A faster, less robust alternative to <see cref="FromDualOrthogonalization(Direction,Direction)"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="FromDualOrthogonalization(Direction,Direction)"/>, this method does not correct for near-parallel
	/// inputs or handle <see cref="None"/> specially: it assumes <paramref name="dirA"/> and <paramref name="dirB"/> are
	/// already genuinely orthogonal and that neither is <see cref="None"/>. Only use this overload when you can already
	/// guarantee those conditions and need to avoid the extra checks <see cref="FromDualOrthogonalization(Direction,Direction)"/> performs.
	/// The returned value of this function is undefined when any condition above is broken.
	/// </remarks>
	/// <param name="dirA">The first direction. Must not be <see cref="None"/>, and must be orthogonal to <paramref name="dirB"/>.</param>
	/// <param name="dirB">The second direction. Must not be <see cref="None"/>, and must be orthogonal to <paramref name="dirA"/>.</param>
	public static Direction FastFromDualOrthogonalization(Direction dirA, Direction dirB) {
		return FromVector3(Vector3.Cross(dirA.ToVector3(), dirB.ToVector3()));
	}

	/// <summary>
	/// Returns the direction lying in <paramref name="plane"/> that is <paramref name="polarAngle"/> around from <paramref name="zeroDegreesDirection"/>.
	/// </summary>
	/// <remarks>
	/// Picture looking directly at <paramref name="plane"/> with its normal axis pointing at you: <paramref name="zeroDegreesDirection"/> is
	/// treated as the 0° direction within the plane, and <paramref name="polarAngle"/> is measured anticlockwise from
	/// there (the same convention used elsewhere for <see cref="Rotation"/>).
	/// </remarks>
	/// <param name="plane">The plane the resultant direction should lie in.</param>
	/// <param name="zeroDegreesDirection">The direction within <paramref name="plane"/> to treat as the 0° reference.
	/// If this is <see cref="None"/> or exactly orthogonal to <paramref name="plane"/>, an arbitrary direction within the plane is substituted instead.</param>
	/// <param name="polarAngle">The angle, anticlockwise from <paramref name="zeroDegreesDirection"/> (when the plane's normal is pointing at you), of the direction to return.</param>
	public static Direction FromPlaneAndPolarAngle(Plane plane, Direction zeroDegreesDirection, Angle polarAngle) {
		if (zeroDegreesDirection.ParallelizedWith(plane) == null) zeroDegreesDirection = plane.Normal.AnyOrthogonal();
		var converter = plane.CreateDimensionConverter(Location.Origin, zeroDegreesDirection);
		return FromVector3(converter.ConvertVect(XYPair<float>.FromPolarAngle(polarAngle)).ToVector3());
	}

	/// <summary>
	/// Returns whichever element of <paramref name="span"/> is closest (by angle) to <paramref name="targetDir"/>.
	/// </summary>
	/// <param name="targetDir">The direction to find the nearest match for.</param>
	/// <param name="span">The candidate directions to search. Must not be empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromNearestDirectionInSpan(Direction targetDir, ReadOnlySpan<Direction> span) => span[GetIndexOfNearestDirectionInSpan(targetDir, span)];

	/// <summary>
	/// Converts a raw <see cref="Vector3"/> to a <see cref="Direction"/>, normalizing it to unit length.
	/// </summary>
	/// <remarks>
	/// If <paramref name="v"/> is a zero-length vector, the result is <see cref="None"/> rather than an invalid or <c>NaN</c> direction.
	/// If you already know <paramref name="v"/> is unit-length, <see cref="FromVector3PreNormalized(Vector3)"/> is a faster alternative that skips normalization.
	/// </remarks>
	/// <param name="v">The vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3(Vector3 v) => new(NormalizeOrZero(new Vector4(v, WValue)));

	/// <summary>
	/// Returns <paramref name="d"/> re-normalized to unit length.
	/// </summary>
	/// <remarks>
	/// Directions are kept unit-length by construction, but repeated operations can
	/// accrue floating-point drift over time. This method corrects that drift back to exactly unit length.
	/// If <paramref name="d"/> is <see cref="None"/>, the result is also <see cref="None"/>.
	/// </remarks>
	/// <param name="d">The direction to re-normalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Renormalize(Direction d) => new(NormalizeOrZero(d.AsVector4));

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	/// <inheritdoc />
	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	/// <inheritdoc />
	/// <remarks>
	/// The given components are normalized to unit length as part of the conversion, identically to the <see cref="Direction(float,float,float)"/> constructor.
	/// </remarks>
	public static implicit operator Direction((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);
	#endregion

	#region Random
	/// <summary>
	/// Produces a random direction, uniformly distributed over the surface of a unit sphere (i.e. every possible direction is equally likely).
	/// </summary>
	/// <returns>A new <see cref="Direction"/>. This method never returns <see cref="None"/>.</returns>
	public static Direction Random() {
		/* Maintainer's note:
		 * Previously this sampled a point inside a unit cube and then projected on the sphere surface, but that overrepresents the 8 diagonal axes in the distribution
		 * (as there is more space in the unit cube around those corners outside of the sphere than anywhere else). 
		 * 
		 * Instead here is the Marsaglia Polar Method for uniform distribution around a unit sphere's surface, it assumes the Achimedes Hat Theorem and then projects
		 * a randomly selected point on the 2D circle (with radius 1) upwards knowing that all surface area is distributed evenly according to that theorem.
		 *
		 * Wiki article: https://en.wikipedia.org/wiki/Marsaglia_polar_method
		 *
		 * I did wonder whether sumSq >= 1 should in fact be sumSq > 1 (as at the moment it's possible to get result (0, 0, 1) but not (0, 0, -1) which feels
		 * anti-uniform). Here's Claude's answer:
		 *
		 * Uniformity is a property of the probability measure, not of the set of points that are reachable. Adding or removing a measure-zero set — a single point, or a curve — never changes a distribution.
		 * The south pole (0, 0, −1) has probability zero of being hit regardless, so its absence can't make the distribution non-uniform, and its presence can't make it "more" uniform.
		 * A perfectly uniform distribution on the sphere is completely unbothered by whether one individual point is attainable.
		 *
		 * So switching >= to > accepts the boundary circle u² + v² = 1, but that circle is a 1D curve in the 2D disk — it has area zero.
		 * In exact real arithmetic the change is a no-op for the distribution: same uniform measure, plus one now-reachable point that still gets chosen with probability zero.
		 *
		 * In floating point it's actually a hair worse, not better. Look at what the map does on that boundary: when sumSq is exactly 1, scalar = 2·√(1−1) = 0, so the return is (u·0, v·0, 1−2) = (0, 0, −1) —
		 * the south pole, no matter what direction (u, v) pointed.
		 * The entire boundary circle collapses onto that single point. Now, a handful of float pairs do compute sumSq to exactly 1.0f — most obviously (±1, 0) and (0, ±1). With >= those are correctly discarded.
		 * With > they'd all be accepted and every one of them would return the exact same point (0, 0, −1).
		 * That gives the south pole a tiny excess weight — a point-mass, the one genuinely non-uniform thing you could introduce here. Vanishingly small, but it's a nudge in the wrong direction.
		 *
		 * So the current code has it right: rejecting >= 1 throws away the degenerate boundary rather than piling it onto one pole.
		 * The distribution you already have is the uniform one. What tripped your intuition is the natural feeling that "uniform" should mean "every point is reachable and symmetric" —
		 * but for continuous distributions, symmetry of the measure is what matters, and that's intact even with a pole missing.
		 */
		while (true) {
			var u = RandomUtils.NextSingleNegOneToOneInclusive();
			var v = RandomUtils.NextSingleNegOneToOneInclusive();
			var uvSquared = u * u + v * v;
			if (uvSquared >= 1f) continue;
			var hatScalar = 2f * MathF.Sqrt(1f - uvSquared);
			return new(u * hatScalar, v * hatScalar, 1f - 2f * uvSquared);
		}
	}
	/// <summary>
	/// Produces a random direction that lies somewhere on the shortest arc between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <remarks>
	/// Directions have no natural ordering, so this does
	/// not mean "any direction between two bounds" in a volumetric sense: the result always lies exactly on the geodesic
	/// (the shortest path around a great circle) connecting <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </remarks>
	/// <param name="minInclusive">One end of the arc to pick from. This value itself can be returned.</param>
	/// <param name="maxExclusive">The other end of the arc to pick from. This exact value will not be returned.</param>
	public static Direction Random(Direction minInclusive, Direction maxExclusive) {
		return (minInclusive >> maxExclusive).ScaledBy(RandomUtils.NextSingle()) * minInclusive;
	}
	/// <summary>
	/// Produces a random direction within a cone around <paramref name="coneCentre"/>.
	/// </summary>
	/// <remarks>
	/// Equivalent to <c>Random(coneCentre, coneAngleMax, Angle.Zero)</c>; see <see cref="Random(Direction,Angle,Angle)"/> for details.
	/// </remarks>
	/// <param name="coneCentre">The direction at the centre of the cone to pick from. If this is <see cref="None"/>, the result is equivalent to calling <see cref="Random()"/>.</param>
	/// <param name="coneAngleMax">The maximum angle, from <paramref name="coneCentre"/>, that the result can be.</param>
	public static Direction Random(Direction coneCentre, Angle coneAngleMax) => Random(coneCentre, coneAngleMax, Angle.Zero);
	/// <summary>
	/// Produces a random direction within an angular band around <paramref name="coneCentre"/>.
	/// </summary>
	/// <remarks>
	/// The result's angle from <paramref name="coneCentre"/> is uniformly distributed between <paramref name="coneAngleMin"/>
	/// and <paramref name="coneAngleMax"/>, and its rotation around <paramref name="coneCentre"/> (i.e. where on the cone's
	/// circular cross-section it falls) is uniformly random. Leaving <paramref name="coneAngleMin"/> at <see cref="Angle.Zero"/>
	/// (the default when using <see cref="Random(Direction,Angle)"/>) samples a solid cone; setting it above zero excludes
	/// a smaller inner cone, producing a hollow conical shell instead.
	/// </remarks>
	/// <param name="coneCentre">The direction at the centre of the cone to pick from. If this is <see cref="None"/>, the result is equivalent to calling <see cref="Random()"/>.</param>
	/// <param name="coneAngleMax">The maximum angle, from <paramref name="coneCentre"/>, that the result can be. This value is clamped internally between 0° and 180°.</param>
	/// <param name="coneAngleMin">The minimum angle, from <paramref name="coneCentre"/>, that the result can be. This value is clamped internally between 0° and 180°.</param>
	public static Direction Random(Direction coneCentre, Angle coneAngleMax, Angle coneAngleMin) {
		if (coneCentre == None) return Random();

		// Uniform in the cosine of the polar angle rather than the angle itself; otherwise results cluster towards the cone's centre
		var maxCosine = coneAngleMax.ClampZeroToHalfCircle().Cosine;
		var minCosine = coneAngleMin.ClampZeroToHalfCircle().Cosine;
		var offset = coneCentre * (coneCentre >> coneCentre.AnyOrthogonal()) with {
			Angle = Angle.FromCosine(RandomUtils.NextSingleInclusive(maxCosine, minCosine))
		};
		return offset * new Rotation(Angle.Random(Angle.Zero, Angle.FullCircle), coneCentre);
	}
	/// <summary>
	/// Produces a random direction lying within <paramref name="plane"/>.
	/// </summary>
	/// <remarks>
	/// Equivalent to <c>Random(plane, plane.Normal.AnyOrthogonal(), Angle.FullCircle)</c> — i.e. every direction within the plane is equally likely.
	/// </remarks>
	/// <param name="plane">The plane the resultant direction should lie in.</param>
	public static Direction Random(Plane plane) => Random(plane, plane.Normal.AnyOrthogonal(), Angle.FullCircle);
	/// <summary>
	/// Produces a random direction lying within <paramref name="plane"/>, within an arc around <paramref name="arcCentre"/>.
	/// </summary>
	/// <remarks>
	/// The result is uniformly distributed within the arc that spans <paramref name="arcAngle"/> in total, centred on
	/// <paramref name="arcCentre"/> (i.e. extending <paramref name="arcAngle"/> / 2 to either side).
	/// </remarks>
	/// <param name="plane">The plane the resultant direction should lie in.</param>
	/// <param name="arcCentre">The direction, lying within <paramref name="plane"/>, at the centre of the arc to pick from.
	/// If this is <see cref="None"/> or exactly orthogonal to <paramref name="plane"/>, any random direction on <paramref name="plane"/> is returned instead.</param>
	/// <param name="arcAngle">The total angular width of the arc to pick from.</param>
	public static Direction Random(Plane plane, Direction arcCentre, Angle arcAngle) {
		if (arcCentre.ParallelizedWith(plane) == null) return Random(plane);
		var halfAngle = arcAngle * 0.5f;
		return FromPlaneAndPolarAngle(plane, arcCentre, Angle.Random(-halfAngle, halfAngle));
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Direction src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Z);
	}

	/// <inheritdoc />
	public static Direction DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return FromVector3PreNormalized(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversion
	/// <inheritdoc />
	public override string ToString() => this.ToString(null, null);

	/// <inheritdoc />
	public string ToStringDescriptive() {
		const float ZeroAngleMaxProximity = 1E-3f;
		var (orientation, direction) = NearestOrientation;
		var angle = (this == None || direction == None) ? Angle.Zero : (this ^ direction);
		if (angle < ZeroAngleMaxProximity) return $"{ToString()} ({orientation})";
		else return $"{ToString()} ({orientation} +{angle:N0})";
	}

	/*
	 * We don't use FromVector3PreNormalized for these methods that parse from a string because it's likely that the
	 * string representation has lost some precision and therefore the re-parsed value won't actually be unit-length.
	 */
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Direction result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = FromVector3(vec3);
			return true;
		}
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Direction result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = FromVector3(vec3);
			return true;
		}
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Direction other) => AsVector4.Equals(other.AsVector4);
	/// <summary>
	/// Determines whether this direction is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares the <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> components independently, each within
	/// <paramref name="tolerance"/>; it is not the same as checking whether the angle between the two directions is small
	/// (for that, use <see cref="AngleTo(Direction)"/> or <see cref="IsWithinAngleTo"/> instead).
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(Direction other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Direction left, Direction right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Direction left, Direction right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Direction other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}