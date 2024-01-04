// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Direction : IVect<Direction> {
	internal const float WValue = 0f;
	public static readonly Direction None = new();
	public static readonly Direction Forward = new(0f, 0f, 1f);
	public static readonly Direction Backward = new(0f, 0f, -1f);
	public static readonly Direction Up = new(0f, 1f, 0f);
	public static readonly Direction Down = new(0f, -1f, 0f);
	public static readonly Direction Left = new(1f, 0f, 0f);
	public static readonly Direction Right = new(-1f, 0f, 0f);
	public static readonly IReadOnlyCollection<Direction> AllCardinals = new[] { Left, Right, Up, Down, Forward, Backward };

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
		static void GetCardinalAngles(Direction @this, Span<(Direction, Angle)> destSpan) {
			for (var i = 0; i < AllCardinals.Count; ++i) destSpan[i] = (AllCardinals.ElementAt(i), AllCardinals.ElementAt(i) ^ @this);
		}
		static string GetCardinalEnglishName(Direction cardinal) {
			static string? CheckAndReturn(Direction input, Direction test, [CallerArgumentExpression(nameof(test))] string? argName = null) {
				if (input.Equals(test, 0.1f)) return argName;
				else return null;
			}

			return CheckAndReturn(cardinal, Forward)
				?? CheckAndReturn(cardinal, Up)
				?? CheckAndReturn(cardinal, Down)
				?? CheckAndReturn(cardinal, Backward)
				?? CheckAndReturn(cardinal, Left)
				?? CheckAndReturn(cardinal, Right)
				?? "None";
		}

		if (this == None) return "None";

		Span<(Direction Cardinal, Angle Angle)> cardinalTuples = stackalloc (Direction, Angle)[AllCardinals.Count];
		GetCardinalAngles(this, cardinalTuples);

		var closestTuple = (Cardinal: None, Angle: Angle.FullCircle);
		for (var i = 0; i < cardinalTuples.Length; ++i) {
			if (cardinalTuples[i].Angle < closestTuple.Angle) closestTuple = cardinalTuples[i];
		}
		if (closestTuple.Angle.Equals(Angle.Zero, 0.1f)) return $"{GetCardinalEnglishName(closestTuple.Cardinal)} exactly";

		var nextClosestTuple = (Cardinal: None, Angle: Angle.FullCircle);
		for (var i = 0; i < cardinalTuples.Length; ++i) {
			if (cardinalTuples[i].Cardinal == closestTuple.Cardinal) continue;
			if (cardinalTuples[i].Angle < nextClosestTuple.Angle) nextClosestTuple = cardinalTuples[i];
		}

		if (closestTuple.Angle.Equals(45f, 0.1f)) {
			return $"Between {GetCardinalEnglishName(closestTuple.Cardinal)} and {GetCardinalEnglishName(nextClosestTuple.Cardinal)}";
		}
		else {
			return $"{closestTuple.Angle.ToString("N1", CultureInfo.InvariantCulture)} from {GetCardinalEnglishName(closestTuple.Cardinal)} " +
				   $"(mostly towards {GetCardinalEnglishName(nextClosestTuple.Cardinal)})";
		}
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
}