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
public readonly partial struct Vect : IVect<Vect>, IDescriptiveStringProvider {
	internal const float WValue = 0f;
	public static readonly Vect Zero = new(0f, 0f, 0f);

	internal readonly Vector4 AsVector4;

	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.X = value;
	}
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Y = value;
	}
	public float Z {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Z = value;
	}

	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Vect this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect() : this(0f, 0f, 0f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect(float x, float y, float z) : this(new Vector4(x, y, z, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Vect(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromDirectionAndDistance(Direction direction, float distance) => direction * distance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromVector3(Vector3 v) => new(new Vector4(v, WValue));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	public static implicit operator Vect((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Direction directionOperand) => new(directionOperand.AsVector4 with { W = WValue });
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Location locationOperand) => new(locationOperand.AsVector4 with { W = WValue });

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Vect IVect.AsVect() => this;
	#endregion

	#region Random
	public static Vect Random() {
		return new Vect(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange;
	}
	public static Vect Random(Vect minInclusive, Vect maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.X, maxExclusive.X),
			RandomUtils.NextSingle(minInclusive.Y, maxExclusive.Y),
			RandomUtils.NextSingle(minInclusive.Z, maxExclusive.Z)
		);
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	public static void SerializeToBytes(Span<byte> dest, Vect src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Z);
	}

	public static Vect DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversion
	public override string ToString() => this.ToString(null, null);

	public string ToStringDescriptive() {
		return $"{ToString()} (Direction {Direction.ToStringDescriptive()}, Length {Length.ToString(LengthSquared >= 10f ? "N1" : "N3", CultureInfo.InvariantCulture)})";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	public static bool TryParse(string? s, IFormatProvider? provider, out Vect result) {
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
	public static Vect Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vect result) {
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
	public bool Equals(Vect other) => AsVector4.Equals(other.AsVector4);
	public bool Equals(Vect other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Vect left, Vect right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Vect left, Vect right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Vect other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}