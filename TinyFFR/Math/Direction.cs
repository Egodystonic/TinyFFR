// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction() { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction(float x, float y, float z) : this(NormalizeOrZero(new Vector4(x, y, z, WValue))) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Direction(Vector4 v) { AsVector4 = v; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromPreNormalizedComponents(Vector3 v) => new(new Vector4(v, WValue));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromPreNormalizedComponents(float x, float y, float z) => new(new Vector4(x, y, z, WValue));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromOrientation(Orientation3D orientation) => new(orientation.GetAxisSign(Axis.X), orientation.GetAxisSign(Axis.Y), orientation.GetAxisSign(Axis.Z));

	public static Direction FromPerpendicularToBoth(Direction dirA, Direction dirB) { // TODO in Xmldoc note that this will return any perp to both, no guarantee on which one. If either is None, then it throws
		var cross = Vector3.Cross(dirA.ToVector3(), dirB.ToVector3());
		var crossLength = cross.LengthSquared();

		if (MathF.Abs(crossLength - 1f) <= 0.001f) return FromPreNormalizedComponents(cross);
		else if (crossLength >= 0.001f) return FromVector3(cross);
		else if (dirA.Equals(None, 0.001f) || dirB.Equals(None, 0.001f)) throw new ArgumentException($"Neither {nameof(Direction)} can be {nameof(None)}.");
		else return dirA.GetAnyPerpendicular();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3(Vector3 v) => new(NormalizeOrZero(new Vector4(v, WValue)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	public static implicit operator Direction((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Direction src) => MemoryMarshal.Cast<Direction, float>(new ReadOnlySpan<Direction>(in src))[..3];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ConvertFromSpan(ReadOnlySpan<float> src) => FromPreNormalizedComponents(new Vector3(src));

	public override string ToString() => this.ToString(null, null);

	public string ToStringDescriptive() {
		GetNearestOrientation(out var orientation, out var direction);
		var angle = (this == None || direction == None) ? Angle.Zero : (this ^ direction);
		return $"{ToString()} ({angle:N0} from {orientation})";
	}

	/*
	 * We don't use FromPreNormalizedComponents for these methods that parse from a string because it's possible (likely?) that the
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsWithinAngle(Direction other, Angle angle) => (this ^ other) <= angle; // TODO make it clear that this will throw exception if this or other are None

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Direction(Location locationOperand) => new(locationOperand.AsVector4 with { W = WValue });
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Direction(Vect vectOperand) => new(vectOperand.AsVector4 with { W = WValue });
}