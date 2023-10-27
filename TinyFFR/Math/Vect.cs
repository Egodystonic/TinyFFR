// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Vect : IVect<Vect>, IComparable<Vect>, IComparisonOperators<Vect, Vect, bool> {
	const float WValue = 0f;
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect() { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect(float x, float y, float z) : this(new Vector3(x, y, z)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect(Vector3 v) : this(new Vector4(v, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Vect(Vector4 v) { AsVector4 = v; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromDirectionAndDistance(Direction direction, float distance) => direction * distance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromVector3(Vector3 v) => new(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Vect src) => MemoryMarshal.Cast<Vect, float>(new ReadOnlySpan<Vect>(src))[..3];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect ConvertFromSpan(ReadOnlySpan<float> src) => new(new Vector3(src));

	public override string ToString() => this.ToString(null, null);

	public string ToStringDescriptive() {
		return $"{Direction.ToStringDescriptive()} with length {Length.ToString("N1", CultureInfo.InvariantCulture)}";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect Parse(string s, IFormatProvider? provider = null) => new(IVect.ParseVector3String(s, provider));

	public static bool TryParse(string? s, IFormatProvider? provider, out Vect result) {
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
	public static Vect Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => new(IVect.ParseVector3String(s, provider));

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vect result) {
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Direction directionOperand) => new(directionOperand.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Location locationOperand) => new(locationOperand.AsVector4 with { W = WValue });
}