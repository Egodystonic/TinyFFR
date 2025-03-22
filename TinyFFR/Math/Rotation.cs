// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Rotation : IMathPrimitive<Rotation>, IDescriptiveStringProvider {
	public const string ToStringMiddleSection = " around ";
	public static readonly Rotation None = new(Angle.Zero, Direction.None);

	readonly Vector4 _axis3dAndAngleRadians;
	
	// TODO indicate this is clockwise looking along the axis direction
	public Angle Angle {
		get => Angle.FromRadians(_axis3dAndAngleRadians.W);
		init => _axis3dAndAngleRadians.W = value.Radians;
	} 
	public Direction Axis {
		get => Direction.FromVector3PreNormalized(_axis3dAndAngleRadians.AsVector3());
		init => _axis3dAndAngleRadians = new Vector4(value.ToVector3(), _axis3dAndAngleRadians.W);
	}

	public Rotation(Angle angle, Direction axis) : this(new Vector4(axis.ToVector3(), angle.Radians)) { }

	Rotation(Vector4 axis3DAndAngleRadians) {
		_axis3dAndAngleRadians = axis3DAndAngleRadians;
	}

	#region Factories and Conversions
	public static Rotation FromStartAndEndDirection(Direction startDirection, Direction endDirection) {
		// var dot = Vector4.Dot(startDirection.AsVector4, endDirection.AsVector4);
		// if (dot > -0.9999f) return FromQuaternion(new(Vector3.Cross(startDirection.ToVector3(), endDirection.ToVector3()), dot + 1f));
		//
		// // If we're rotating exactly 180 degrees there are infinitely many arcs of "shortest" path, so the math breaks down.
		// // Therefore we just pick any perpendicular vector and rotate around that.
		// var perpVec = startDirection.AnyOrthogonal();
		// return new(Angle.HalfCircle, perpVec);
		return new(startDirection.AngleTo(endDirection), Direction.FromDualOrthogonalization(startDirection, endDirection));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() {
		if (Angle == Angle.Zero || Axis == Direction.None) return Identity;
		else return CreateFromAxisAngle(Axis.ToVector3(), Angle.Radians);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => FromQuaternionPreNormalized(NormalizeOrIdentity(q));

	public static Rotation FromQuaternionPreNormalized(Quaternion q) {
		return new(
			Angle.FromRadians(MathF.Acos(q.W) * 2f),
			MathF.Abs(q.W) >= 1f ? Direction.None : new(q.X, q.Y, q.Z)
		);
	}

	public void Deconstruct(out Angle angle, out Direction axis) {
		angle = Angle;
		axis = Axis;
	}
	#endregion

	#region Random
	public static Rotation Random() => new(Angle.Random(), Direction.Random());

	public static Rotation Random(Rotation minInclusive, Rotation maxExclusive) {
		return new(Angle.Random(minInclusive.Angle, maxExclusive.Angle), Direction.Random(minInclusive.Axis, maxExclusive.Axis));
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 4;

	public static void SerializeToBytes(Span<byte> dest, Rotation src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src._axis3dAndAngleRadians.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src._axis3dAndAngleRadians.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src._axis3dAndAngleRadians.Z);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 3)..], src._axis3dAndAngleRadians.W);
	}

	public static Rotation DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(new Vector4(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 3)..])
		));
	}
	#endregion

	#region String Conversion
	public string ToStringDescriptive() => $"{Angle}{ToStringMiddleSection}{Axis.ToStringDescriptive()}";
	
	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Angle.ToString(format, formatProvider)}{ToStringMiddleSection}{Axis.ToString(format, formatProvider)}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		var (angle, axis) = this;
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		// Angle
		writeSuccess = angle.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Middle Section
		writeSuccess = destination.TryWrite(provider, $"{ToStringMiddleSection}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Axis
		writeSuccess = axis.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static Rotation Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Rotation result) => TryParse(s.AsSpan(), provider, out result);

	public static Rotation Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var indexOfMiddlePart = s.IndexOf(ToStringMiddleSection);
		var angle = Angle.Parse(s[..indexOfMiddlePart], provider);
		var axis = Direction.Parse(s[(indexOfMiddlePart + ToStringMiddleSection.Length)..], provider);
		return new(angle, axis);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rotation result) {
		result = default;

		var indexOfMiddlePart = s.IndexOf(ToStringMiddleSection);
		if (indexOfMiddlePart < 0) return false;
		if (indexOfMiddlePart + ToStringMiddleSection.Length >= s.Length) return false;

		if (!Angle.TryParse(s[..indexOfMiddlePart], provider, out var angle)) return false;
		if (!Direction.TryParse(s[(indexOfMiddlePart + ToStringMiddleSection.Length)..], provider, out var axis)) return false;

		result = new(angle, axis);
		return true;
	}
	#endregion

	#region Equality
	public bool IsEquivalentForAllDirectionsTo(Rotation other) {
		var thisQuat = ToQuaternion();
		var otherQuat = other.ToQuaternion();
		return thisQuat.Equals(otherQuat) || thisQuat.Equals(-otherQuat);
	}
	public bool IsEquivalentForAllDirectionsTo(Rotation other, float tolerance) {
		static bool CompareQuats(Quaternion a, Quaternion b, float t) {
			return MathF.Abs(a.X - b.X) <= t
				&& MathF.Abs(a.Y - b.Y) <= t
				&& MathF.Abs(a.Z - b.Z) <= t
				&& MathF.Abs(a.W - b.W) <= t;
		}

		var thisQuat = ToQuaternion();
		var otherQuat = other.ToQuaternion();
		return CompareQuats(thisQuat, otherQuat, tolerance) || CompareQuats(thisQuat, -otherQuat, tolerance);
	}

	public bool IsEquivalentForSingleDirectionTo(Rotation other, Direction targetDirection) {
		var thisResult = Rotate(targetDirection);
		var otherResult = other.Rotate(targetDirection);
		return thisResult.Equals(otherResult);
	}
	public bool IsEquivalentForSingleDirectionTo(Rotation other, Direction targetDirection, float tolerance) {
		var thisResult = Rotate(targetDirection);
		var otherResult = other.Rotate(targetDirection);
		return thisResult.Equals(otherResult, tolerance);
	}

	public bool Equals(Rotation other, float tolerance) {
		return Angle.Equals(other.Angle, tolerance) && Axis.Equals(other.Axis, tolerance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Rotation other) => _axis3dAndAngleRadians.Equals(other._axis3dAndAngleRadians);
	public override bool Equals(object? obj) => obj is Rotation other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => _axis3dAndAngleRadians.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Rotation left, Rotation right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Rotation left, Rotation right) => !left.Equals(right);
	#endregion
}