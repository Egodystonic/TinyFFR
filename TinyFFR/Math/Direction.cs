// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Direction : IVect<Direction>, IDescriptiveStringProvider {
	internal const float WValue = 0f;
	public static readonly Direction None = new();
	public static readonly Direction Forward = new(0f, 0f, 1f);
	public static readonly Direction Backward = new(0f, 0f, -1f);
	public static readonly Direction Up = new(0f, 1f, 0f);
	public static readonly Direction Down = new(0f, -1f, 0f);
	public static readonly Direction Left = new(1f, 0f, 0f);
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

	public static ReadOnlySpan<Direction> AllCardinals => _allCardinals;
	public static ReadOnlySpan<Direction> AllIntercardinals => _allIntercardinals;
	public static ReadOnlySpan<Direction> AllDiagonals => _allDiagonals;
	public static ReadOnlySpan<Direction> AllOrientations => _allOrientations;

	internal readonly Vector4 AsVector4;

	/* No init accessor on these properties. It's not intuitive that Direction tries to keep itself normalized, so
	 * setting e.g. new Direction(1f, 2f, 3f) with { X = 4f, Y = 5f } is very hard to reason about.
	 */
	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
	}
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
	}
	public float Z {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
	}

	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Direction this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction() : this(0f, 0f, 0f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction(float x, float y, float z) : this(NormalizeOrZero(new Vector4(x, y, z, WValue))) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Direction(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(Vector3 v) => new(new Vector4(v, WValue));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(float x, float y, float z) => new(new Vector4(x, y, z, WValue));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromOrientation(Orientation orientation) => new(orientation.GetAxisSign(Axis.X), orientation.GetAxisSign(Axis.Y), orientation.GetAxisSign(Axis.Z));

	// TODO xmldoc -- by default follows right hand rule (index finger = dirA, middle finger = dirB, thumb = result)
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
	public static Direction FromDualOrthogonalization(Direction dirA, Direction dirB, bool rightHanded) {
		return rightHanded ? FromDualOrthogonalization(dirA, dirB) : FromDualOrthogonalization(dirB, dirA);
	}
	// TODO xmldoc expects that dirA and dirB are definitely orthogonal already and not None
	public static Direction FastFromDualOrthogonalization(Direction dirA, Direction dirB) {
		return FromVector3(Vector3.Cross(dirA.ToVector3(), dirB.ToVector3()));
	}

	// TODO xmldoc: Imagine a plane, and imagine one direction along the plane is set as "zero". This factory method lets you specify directions as a polar angle offset from the zero direction on the plane
	// TODO xmldoc: Angle is clockwise looking along the plane normal (much like rotations)
	public static Direction FromPlaneAndPolarAngle(Plane plane, Direction zeroDegreesDirection, Angle polarAngle) {
		if (zeroDegreesDirection.ParallelizedWith(plane) == null) zeroDegreesDirection = plane.Normal.AnyOrthogonal();
		var converter = plane.CreateDimensionConverter(Location.Origin, zeroDegreesDirection);
		return FromVector3(converter.ConvertVect(XYPair<float>.FromPolarAngle(polarAngle)).ToVector3());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromNearestDirectionInSpan(Direction targetDir, ReadOnlySpan<Direction> span) => span[GetIndexOfNearestDirectionInSpan(targetDir, span)];
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3(Vector3 v) => new(NormalizeOrZero(new Vector4(v, WValue)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Renormalize(Direction d) => new(NormalizeOrZero(d.AsVector4));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	public static implicit operator Direction((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);
	#endregion

	#region Random
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
	public static Direction Random(Direction minInclusive, Direction maxExclusive) {
		return (minInclusive >> maxExclusive).ScaledBy(RandomUtils.NextSingle()) * minInclusive;
	}
	public static Direction Random(Direction coneCentre, Angle coneAngleMax) => Random(coneCentre, coneAngleMax, Angle.Zero);
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
	public static Direction Random(Plane plane) => Random(plane, plane.Normal.AnyOrthogonal(), Angle.FullCircle);
	public static Direction Random(Plane plane, Direction arcCentre, Angle arcAngle) {
		if (arcCentre.ParallelizedWith(plane) == null) arcCentre = plane.Normal.AnyOrthogonal();
		var halfAngle = arcAngle * 0.5f;
		return FromPlaneAndPolarAngle(plane, arcCentre, Angle.Random(-halfAngle, halfAngle));
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	public static void SerializeToBytes(Span<byte> dest, Direction src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Z);
	}

	public static Direction DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return FromVector3PreNormalized(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversion
	public override string ToString() => this.ToString(null, null);

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Direction other) => AsVector4.Equals(other.AsVector4);
	public bool Equals(Direction other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Direction left, Direction right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Direction left, Direction right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Direction other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}