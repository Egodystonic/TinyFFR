// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Quaternion
public readonly partial struct Rotation : ILinearAlgebraComposite<Rotation> {
	public static readonly Rotation None = new(Quaternion.Identity);

	internal readonly Quaternion AsQuaternion;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation() { AsQuaternion = Quaternion.Identity; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation(ReadOnlySpan<float> xyzw) : this(new Quaternion(xyzw[0], xyzw[1], xyzw[2], xyzw[3])) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation(float x, float y, float z, float w) : this(new Quaternion(x, y, z, w)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Rotation(Quaternion q) { AsQuaternion = q; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromAngleAroundAxis(Direction axis, Angle angle) => new(Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle.AsRadians));

	public static Rotation FromStartAndEndDirection(Direction startDirection, Direction endDirection) {
		var dot = Dot(startDirection.AsVector4, endDirection.AsVector4);
		if (dot > -0.9999f) return new(Quaternion.Normalize(new(Vector3.Cross(startDirection.ToVector3(), endDirection.ToVector3()), dot + 1f)));

		// If we're rotating exactly 180 degrees there are infinitely many arcs of "shortest" path, so the math breaks down.
		// Therefore we just pick any perpendicular vector and rotate around that.
		var perpVec = startDirection.GetAnyPerpendicularDirection();
		return FromAngleAroundAxis(perpVec, 0.5f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => new(q);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() => AsQuaternion; // Q: Why not just make AsQuaternion a public prop? A: To keep the "To<numericsTypeHere>" pattern consistent with vector abstraction types

	
}