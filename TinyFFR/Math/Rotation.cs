// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Quaternion
public readonly partial struct Rotation : IMathPrimitive<Rotation, float> {
	public const string ToStringMiddleSection = " around ";
	public static readonly Rotation None = new(Identity);

	internal readonly Quaternion AsQuaternion;

	public Angle Angle { // TODO indicate this is clockwise everywhere
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.FromRadians(MathF.Acos(AsQuaternion.W)) * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init {
			// This if check helps prevent non-unit quaternions if someone invokes with{} for this property on Rotation.None
			if (MathF.Abs(AsQuaternion.W) > 0.999f) AsQuaternion = Identity;
			else AsQuaternion = CreateFromAxisAngle(Axis.ToVector3(), value.Radians);
		}
	}

	public Direction Axis {
		get {
			var halfAngleRadians = MathF.Acos(AsQuaternion.W);
			if (halfAngleRadians < 0.0001f) return Direction.None;
			else return Direction.FromPreNormalizedComponents(new Vector3(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z) / MathF.Sin(halfAngleRadians));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init {
			// This if check helps prevent non-unit quaternions if someone invokes with{} for this property on a non-zero Rotation with a value of Direction.None
			if (value.Equals(Direction.None, 0.0001f)) AsQuaternion = Identity;
			else AsQuaternion = CreateFromAxisAngle(value.ToVector3(), Angle.Radians);
		}
	}

	internal Vector4 AsVector4 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(in AsQuaternion));
	}

	// TODO for the lerp/slerp ... We probably need to expose them here but I wonder if a dedicated Lerper object could be smarter about e.g. a 180deg rotation around an axis
	// TODO I don't think a generalized lerper is the right thing here -- instead just provide a static method that helps lerp around an axis/angle (or provides something that helps with that to reduce calcuations)
	// TODO A generalized interpolator type might be useful (research interface vs delegate and weigh against garbage management etc)-- this can abstract over functions and timing etc
	// TODO interpolator/timeinterpolator -- interpolator should use a delegate* to get virtualisation for free; timeinterpolator maybe could optionally plug in to a global time ticker
	// TODO will probably use an IInterpolatable
	// TODO do we need an interpolator 'object' instead of a static class? It does allow people to hot-swap the interpolation strat...
	// TODO also same with a randomizer I think

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation() { AsQuaternion = Identity; }
	
	public Rotation(Angle angle, Direction axis) { // TODO make it clear that the resultant Rotation Angle/Axis will be auto-normalized by the nature of Quaternion math
		if (angle.Equals(0f, 0.0001f, normalizeAngles: false) || axis.Equals(Direction.None, 0.0001f)) AsQuaternion = Identity;
		else AsQuaternion = CreateFromAxisAngle(axis.ToVector3(), angle.Radians);
	} 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Rotation(Quaternion q) { AsQuaternion = q; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromAngleAroundAxis(Angle angle, Direction axis) => new(angle, axis);

	public static Rotation FromStartAndEndDirection(Direction startDirection, Direction endDirection) {
		var dot = Vector4.Dot(startDirection.AsVector4, endDirection.AsVector4);
		if (dot > -0.9999f) return new(Normalize(new(Vector3.Cross(startDirection.ToVector3(), endDirection.ToVector3()), dot + 1f)));

		// If we're rotating exactly 180 degrees there are infinitely many arcs of "shortest" path, so the math breaks down.
		// Therefore we just pick any perpendicular vector and rotate around that.
		var perpVec = startDirection.GetAnyPerpendicularDirection();
		return FromAngleAroundAxis(Angle.HalfCircle, perpVec);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => new(NormalizeOrIdentity(q));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternionPreNormalized(Quaternion q) => new(q);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() => AsQuaternion; // Q: Why not just make AsQuaternion a public prop? A: To keep the "To<numericsTypeHere>" pattern consistent with vector abstraction types

	public void ExtractAngleAndAxis(out Angle angle, out Direction axis) {
		var halfAngleRadians = MathF.Acos(AsQuaternion.W);
		
		if (halfAngleRadians < 0.0001f) {
			axis = Direction.None;
			angle = Angle.Zero;
			return;
		}

		axis = Direction.FromPreNormalizedComponents(new Vector3(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z) / MathF.Sin(halfAngleRadians));
		angle = Angle.FromRadians(halfAngleRadians * 2f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Rotation src) => MemoryMarshal.Cast<Rotation, float>(new ReadOnlySpan<Rotation>(in src))[..4];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation ConvertFromSpan(ReadOnlySpan<float> src) => new(new Quaternion(src[0], src[1], src[2], src[3]));

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Angle.ToString(format, formatProvider)}{ToStringMiddleSection}{Axis.ToString(format, formatProvider)}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		ExtractAngleAndAxis(out var angle, out var axis);
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
		return FromAngleAroundAxis(angle, axis);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rotation result) {
		result = default;

		var indexOfMiddlePart = s.IndexOf(ToStringMiddleSection);
		if (indexOfMiddlePart < 0) return false;
		if (indexOfMiddlePart + ToStringMiddleSection.Length >= s.Length) return false;

		if (!Angle.TryParse(s[..indexOfMiddlePart], provider, out var angle)) return false;
		if (!Direction.TryParse(s[(indexOfMiddlePart + ToStringMiddleSection.Length)..], provider, out var axis)) return false;

		result = FromAngleAroundAxis(angle, axis);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsForDirection(Rotation other, Direction targetDirection) => EqualsForDirection(other, targetDirection, 0f);
	public bool EqualsForDirection(Rotation other, Direction targetDirection, float tolerance) {
		var thisResult = Rotate(targetDirection);
		var otherResult = other.Rotate(targetDirection);
		return thisResult.Equals(otherResult, tolerance);
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