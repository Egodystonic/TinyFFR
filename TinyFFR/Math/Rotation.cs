// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a three-dimensional rotation, encoded as an angle/axis pair.
/// </summary>
/// <remarks>
/// A rotation object is a description of <i>how to rotate</i> something; e.g. "72° around the Left axis".
/// <para>
/// A Rotation does <b>not</b> represent any specific orientation/direction by itself, instead it represents the operation to apply to any existing direction/orientation to get a <i>new</i> direction/orientation.
/// For example, "facing forward" is just a direction or orientation but "turn from forward to right" is a <i>Rotation</i> (in this case possibly encoded as "90° around the Down axis").
/// </para>
/// <para>
/// You can construct a rotation like so:
/// <ul>
/// <li>From its angle &amp; axis directly (<c>new Rotation(72f, Direction.Left)</c>)</li>
/// <li>From the transition between two directions (<c>Rotation.FromStartAndEndDirection(Direction.Up, Direction.Right)</c>)</li>
/// <li>By combining multiple rotations (<c>rotationOne + rotationTwo + rotationThree</c>)</li>
/// <li>...And a few various other mechanisms</li>
/// </ul>
/// </para>
/// <para>
/// You can then apply a rotation like so:
/// <ul>
/// <li>To a direction or vect (<c>var newDir = direction * rotation;</c></li>
/// <li>To a geometric primitive (<c>var newPlane = plane * rotation;</c></li>
/// <li>To a model instance or camera (<c>camOrInstance.RotateBy(rotation);</c></li>
/// <li>...And a few other places</li>
/// </ul>
/// </para>
/// <para>
/// A note on Quaternions:
/// Angle / axis representation was chosen as it is the most user friendly &amp; least error-prone. However in some circumstances
/// it can be slow for certain operations required frequently or in bulk. Therefore, some APIs in TinyFFR accept both a Rotation and a <see cref="Quaternion"/>.
/// In these cases you may wish to work with Quaternions directly; Rotation has many built-in static members that help you convert between the two with ease.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)]
public readonly partial struct Rotation : IMathPrimitive<Rotation>, IDescriptiveStringProvider {
	public const string ToStringMiddleSection = " around ";
	public static readonly Rotation None = new(Angle.Zero, Direction.None);

	readonly Vector4 _axis3dAndAngleRadians;
	
	// TODO indicate this is anticlockwise when the axis direction is pointing at you
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
		return new(startDirection.AngleTo(endDirection), Direction.FromDualOrthogonalization(startDirection, endDirection));
	}
	
	public static Rotation FromStartAndEndOrientation(Direction startingForwardDirection, Direction startingUpDirection, Direction endForwardDirection, Direction endUpDirection, bool enforceOrthogonality = true) {
		const float MinAngleRadiansForNearAntiparallelUpCorrection = 178f * (MathF.Tau / 360f);

		if (enforceOrthogonality) {
			startingUpDirection = startingUpDirection.OrthogonalizedAgainst(startingForwardDirection) ?? startingForwardDirection.AnyOrthogonal();
			endUpDirection = endUpDirection.OrthogonalizedAgainst(endForwardDirection) ?? endForwardDirection.AnyOrthogonal();
		}
		var forwardRot = startingForwardDirection >> endForwardDirection;
		var carriedUp = startingUpDirection * forwardRot;
		if ((carriedUp ^ endUpDirection) >= Angle.FromRadians(MinAngleRadiansForNearAntiparallelUpCorrection)) {
			return forwardRot + new Rotation(carriedUp.SignedAngleTo(endUpDirection, endForwardDirection), endForwardDirection);
		}
		return forwardRot + (carriedUp >> endUpDirection);
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
	/// <summary>
	/// Determines whether this rotation and <paramref name="other"/> always produce the same end result when
	/// applied to anything regardless of the target's starting direction/orientation.
	/// </summary>
	/// <remarks>
	/// Unlike a standard <see cref="Equals(Rotation)"/> check, this method takes in to account rotations
	/// that are exact "mirrors" of each other (i.e. <c>90° around Left</c> vs <c>-90° around Right</c>)
	/// or those that differ only by multiples of 360°, etc.
	/// </remarks>
	/// <param name="other">The other rotation to compare to.</param>
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

	/// <summary>
	/// Determines whether this rotation and <paramref name="other"/> produce the same result when
	/// applied to the given <paramref name="targetDirection"/>.
	/// </summary>
	/// <remarks>
	/// This function essentially helps you determine if this rotation and <paramref name="other"/>
	/// are ineffective or have the same effect against a specific direction (e.g. perhaps their
	/// rotation axis is colinear with it). 
	/// </remarks>
	/// <param name="other">The other rotation to compare to.</param>
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