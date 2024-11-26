// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Quaternion
public readonly partial struct Rotation : IMathPrimitive<Rotation>, IDescriptiveStringProvider {
	public const string ToStringMiddleSection = " around ";
	public static readonly Rotation None = new(Identity);

	internal readonly Quaternion AsQuaternion;

	public Angle Angle { // TODO indicate this is clockwise everywhere
		get => Angle.FromRadians(MathF.Acos(AsQuaternion.W) * 2f);
	}

	public Direction Axis {
		// Although we can extract the axis by dividing X/Y/Z by sin(acos(W)) and in theory skip the normalization of the direction, this is actually faster (and less prone to FP error around extreme values of W)
		get => MathF.Abs(AsQuaternion.W) >= 1f ? Direction.None : new(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z);
	}

	internal Vector4 AsVector4 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(in AsQuaternion));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation() => AsQuaternion = Identity;

	public Rotation(Angle angle, Direction axis) { // TODO make it clear that the resultant Rotation Angle/Axis will be auto-normalized by the nature of Quaternion math (e.g. negative angle results in positive angle with flipped axis)
		if (angle == Angle.Zero || axis == Direction.None) AsQuaternion = Identity;
		else AsQuaternion = CreateFromAxisAngle(axis.ToVector3(), angle.Radians);
	} 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Rotation(Quaternion q) => AsQuaternion = q;

	#region Factories and Conversions
	public static Rotation FromStartAndEndDirection(Direction startDirection, Direction endDirection) {
		var dot = Vector4.Dot(startDirection.AsVector4, endDirection.AsVector4);
		if (dot > -0.9999f) return FromQuaternion(new(Vector3.Cross(startDirection.ToVector3(), endDirection.ToVector3()), dot + 1f));

		// If we're rotating exactly 180 degrees there are infinitely many arcs of "shortest" path, so the math breaks down.
		// Therefore we just pick any perpendicular vector and rotate around that.
		var perpVec = startDirection.AnyOrthogonal();
		return new(Angle.HalfCircle, perpVec);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => new(NormalizeOrIdentity(q));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() => AsQuaternion; // Q: Why not just make AsQuaternion a public prop? A: To keep the "To<numericsTypeHere>" pattern consistent with vector abstraction types

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternionPreNormalized(Quaternion q) => new(q);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation Renormalize(Rotation r) => FromQuaternion(r.AsQuaternion);

	public void Deconstruct(out Angle angle, out Direction axis) {
		angle = Angle;
		axis = Axis;
	}

	public static implicit operator Rotation((Angle Angle, Direction Axis) operand) => new(operand.Angle, operand.Axis);
	public static implicit operator Rotation((Direction Axis, Angle Angle) operand) => new(operand.Angle, operand.Axis);
	#endregion

	#region Random
	public static Rotation Random() {
		return FromQuaternion(new(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		));
	}
	public static Rotation Random(Rotation minInclusive, Rotation maxExclusive) {
		var difference = minInclusive.Minus(maxExclusive);
		return minInclusive + difference.ScaledBy(RandomUtils.NextSingle());
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 4;

	public static void SerializeToBytes(Span<byte> dest, Rotation src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.AsQuaternion.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.AsQuaternion.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.AsQuaternion.Z);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 3)..], src.AsQuaternion.W);
	}

	public static Rotation DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return FromQuaternionPreNormalized(new(
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
	public bool EqualsForDirection(Rotation other, Direction targetDirection) {
		var thisResult = Rotate(targetDirection);
		var otherResult = other.Rotate(targetDirection);
		return thisResult.Equals(otherResult);
	}
	public bool EqualsForDirection(Rotation other, Direction targetDirection, float tolerance) {
		var thisResult = Rotate(targetDirection);
		var otherResult = other.Rotate(targetDirection);
		return thisResult.Equals(otherResult, tolerance);
	}

	public bool Equals(Rotation other, float tolerance) {
		static bool Compare(Quaternion a, Quaternion b, float t) {
			return MathF.Abs(a.X - b.X) <= t
				&& MathF.Abs(a.Y - b.Y) <= t
				&& MathF.Abs(a.Z - b.Z) <= t
				&& MathF.Abs(a.W - b.W) <= t;
		}

		return Compare(AsQuaternion, other.AsQuaternion, tolerance) || Compare(AsQuaternion, -other.AsQuaternion, tolerance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Rotation other) => AsQuaternion.Equals(other.AsQuaternion) || AsQuaternion.Equals(-other.AsQuaternion);
	public override bool Equals(object? obj) => obj is Rotation other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsQuaternion.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Rotation left, Rotation right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Rotation left, Rotation right) => !left.Equals(right);
	#endregion
}