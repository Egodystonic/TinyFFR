// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Direction : IVect<Direction> {
	const float WValue = 0f;
	public static readonly Direction None = new();
	public static readonly Direction Forward = new(0f, 0f, 1f);
	public static readonly Direction Backward = new(0f, 0f, -1f);
	public static readonly Direction Up = new(0f, 1f, 0f);
	public static readonly Direction Down = new(0f, -1f, 0f);
	public static readonly Direction Left = new(1f, 0f, 0f);
	public static readonly Direction Right = new(-1f, 0f, 0f);
	public static readonly IReadOnlyCollection<Direction> AllCardinals = new[] { Left, Right, Up, Down, Forward, Backward };

	internal readonly Vector4 AsVector4;

	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4 = MathUtils.NormalizeOrZero(AsVector4 with { X = value });
	}
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4 = MathUtils.NormalizeOrZero(AsVector4 with { Y = value });
	}
	public float Z {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4 = MathUtils.NormalizeOrZero(AsVector4 with { Z = value });
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction() { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction(float x, float y, float z) : this(new Vector3(x, y, z)) { }
	public Direction(Vector3 v) : this(MathUtils.NormalizeOrZero(new Vector4(v.X, v.Y, v.Z, WValue))) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Direction(Vector4 v) { AsVector4 = v; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(float x, float y, float z) => FromVector3PreNormalized(new Vector3(x, y, z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(ReadOnlySpan<float> xyz) => FromVector3PreNormalized(new Vector3(xyz));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(Vector3 v) => new(new Vector4(v, WValue));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3(Vector3 v) => new(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Direction src) => MemoryMarshal.Cast<Direction, float>(new ReadOnlySpan<Direction>(src))[..3];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ConvertFromSpan(ReadOnlySpan<float> src) => FromVector3PreNormalized(src);

	public override string ToString() => this.ToString(null, null);

	/*
	 * We don't use FromVector3PreNormalized for these methods that parse from a string because it's possible (likely?) that the
	 * string representation has lost some precision and therefore the re-parsed value won't actually be unit-length.
	 */
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(string s, IFormatProvider? provider = null) => new(IVect.ParseVector3String(s, provider));

	public static bool TryParse(string? s, IFormatProvider? provider, out Direction result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = new(vec3);
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => new(IVect.ParseVector3String(s, provider));

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Direction result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = new(vec3);
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
}