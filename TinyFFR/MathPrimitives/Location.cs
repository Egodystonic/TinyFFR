// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct Location : IVect<Location> {
	const float WValue = 1f;
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
	public Location() : this(Vector3.Zero) { } 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location(float x, float y, float z) : this(new Vector3(x, y, z)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location(Vector3 v) : this(new Vector4(v, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Location(Vector4 v) { AsVector4 = v; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location FromVector3(Vector3 v) => new(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Location src) => MemoryMarshal.Cast<Location, float>(new ReadOnlySpan<Location>(src))[..3];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ConvertFromSpan(ReadOnlySpan<float> src) => new(new Vector3(src));
}