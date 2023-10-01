// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Quaternion
public readonly partial struct Rotation : ILinearAlgebraConstruct<Rotation> {
	public static readonly Rotation None = new(Quaternion.Identity);

	internal readonly Quaternion AsQuaternion;

	public Direction Axis {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			ToAngleAroundAxis(out var result, out _);
			return result;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init {
			AsQuaternion = Quaternion.CreateFromAxisAngle(value.ToVector3(), Angle.Radians);
		}
	}

	public Angle Angle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			ToAngleAroundAxis(out _, out var result);
			return result;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init {
			AsQuaternion = Quaternion.CreateFromAxisAngle(Axis.ToVector3(), value.Radians);
		}
	}
	
	internal Vector4 AsVector4 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(AsQuaternion));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsQuaternion = Unsafe.As<Vector4, Quaternion>(ref value);
	}

	// TODO for the lerp/slerp ... We probably need to expose them here but I wonder if a dedicated Lerper object could be smarter about e.g. a 180deg rotation around an axis

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation() { AsQuaternion = Quaternion.Identity; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation(ReadOnlySpan<float> xyzw) : this(new Quaternion(xyzw[0], xyzw[1], xyzw[2], xyzw[3])) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation(float x, float y, float z, float w) : this(new Quaternion(x, y, z, w)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Rotation(Quaternion q) { AsQuaternion = q; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromAngleAroundAxis(Direction axis, Angle angle) => new(Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle.Radians));

	public static Rotation FromStartAndEndDirection(Direction startDirection, Direction endDirection) {
		var dot = Dot(startDirection.AsVector4, endDirection.AsVector4);
		if (dot > -0.9999f) return new(Quaternion.Normalize(new(Vector3.Cross(startDirection.ToVector3(), endDirection.ToVector3()), dot + 1f)));

		// If we're rotating exactly 180 degrees there are infinitely many arcs of "shortest" path, so the math breaks down.
		// Therefore we just pick any perpendicular vector and rotate around that.
		var perpVec = startDirection.GetAnyPerpendicularDirection();
		return FromAngleAroundAxis(perpVec, 0.5f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromYawPitchRoll(Angle yaw, Angle pitch, Angle roll) => new(Quaternion.CreateFromYawPitchRoll(yaw.Radians, pitch.Radians, roll.Radians));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => new(q);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() => AsQuaternion; // Q: Why not just make AsQuaternion a public prop? A: To keep the "To<numericsTypeHere>" pattern consistent with vector abstraction types

	public void ToAngleAroundAxis(out Direction axis, out Angle angle) {
		var halfAngleRadians = MathF.Acos(AsQuaternion.W);
		
		if (halfAngleRadians < 0.0001f) {
			axis = Direction.None;
			angle = Angle.None;
			return;
		}

		var cosecant = 1f / MathF.Sin(halfAngleRadians);
		axis = Direction.FromVector3PreNormalized(new Vector3(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z) * cosecant);
		angle = Angle.FromRadians(halfAngleRadians * 2f);
	}

	public void ToYawPitchRoll(out Angle yawAngle, out Angle pitchAngle, out Angle rollAngle) {
		var yawDir = new Direction(0f, 1f, 0f);
		var pitchDir = new Direction(1f, 0f, 0f);
		var rollDir = new Direction(0f, 0f, 1f);

		yawAngle = yawDir.AngleTo(yawDir * this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Rotation src) => MemoryMarshal.Cast<Rotation, float>(new ReadOnlySpan<Rotation>(src))[..4];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation ConvertFromSpan(ReadOnlySpan<float> src) => new(src);

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => AsVector4.ToString(format, formatProvider);

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		// <
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];

		// X
		writeSuccess = AsQuaternion.X.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Y
		writeSuccess = AsQuaternion.Y.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Z
		writeSuccess = AsQuaternion.Z.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// W
		writeSuccess = AsQuaternion.W.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// >
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}

	public static Rotation Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Rotation result) => TryParse(s.AsSpan(), provider, out result);

	public static Rotation Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var x = Single.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var y = Single.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var z = Single.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		var w = Single.Parse(s[..^1], provider); // Assume ends with VectorStringSuffixChar

		return new(x, y, z, w);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rotation result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var x)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var y)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var z)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var w)) return false;

		result = new(x, y, z, w);
		return true;
	}

	public bool Equals(Rotation other, float tolerance) {
		return MathF.Abs(AsQuaternion.X - other.AsQuaternion.X) <= tolerance
			&& MathF.Abs(AsQuaternion.Y - other.AsQuaternion.Y) <= tolerance
			&& MathF.Abs(AsQuaternion.Z - other.AsQuaternion.Z) <= tolerance
			&& MathF.Abs(AsQuaternion.W - other.AsQuaternion.W) <= tolerance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Rotation other) => AsQuaternion.Equals(other.AsQuaternion);
	public override bool Equals(object? obj) => obj is Rotation other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsQuaternion.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Rotation left, Rotation right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Rotation left, Rotation right) => !left.Equals(right);
}