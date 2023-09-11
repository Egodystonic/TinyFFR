// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Quaternion
public readonly partial struct Rotation {
	public static readonly Rotation None = new(Quaternion.Identity);

	internal readonly Quaternion AsQuaternion;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation() { AsQuaternion = Quaternion.Identity; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation(ReadOnlySpan<float> xyzw) : this(new Quaternion(xyzw[0], xyzw[1], xyzw[2], xyzw[3])) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Rotation(Quaternion q) { AsQuaternion = q; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation FromQuaternion(Quaternion q) => new(q);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Quaternion ToQuaternion() => AsQuaternion; // Q: Why not just make AsQuaternion a public prop? A: To keep the "To<numericsTypeHere>" pattern consistent with vector abstraction types

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Direction directionOperand) => new(directionOperand.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] 
	public static explicit operator Vect(Location locationOperand) => new(locationOperand.AsVector4 with { W = WValue });
}