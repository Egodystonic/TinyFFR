// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Location : IVect<Location> {
	internal const float WValue = 1f;
	public static readonly Location Origin = new(0f, 0f, 0f);

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
	public Location() : this(0f, 0f, 0f) { } 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location(float x, float y, float z) : this(new Vector4(x, y, z, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Location(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location FromVector3(Vector3 v) => new(new Vector4(v, WValue));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	public static implicit operator Location((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Location(Direction directionOperand) => new(directionOperand.AsVector4 with { W = WValue });
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Location(Vect vectOperand) => new(vectOperand.AsVector4 with { W = WValue });
	#endregion

	#region Span Conversion
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Location src) => MemoryMarshal.Cast<Location, float>(new ReadOnlySpan<Location>(in src))[..3];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ConvertFromSpan(ReadOnlySpan<float> src) => FromVector3(new Vector3(src));
	#endregion

	#region String Conversion
	public override string ToString() => this.ToString(null, null);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	public static bool TryParse(string? s, IFormatProvider? provider, out Location result) {
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
	public static Location Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Location result) {
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
	public bool Equals(Location other) => AsVector4.Equals(other.AsVector4);
	public bool Equals(Location other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Location left, Location right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Location left, Location right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Location other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsWithinDistance(Location other, float distance) => (this - other).LengthSquared <= distance * distance;
	#endregion
}