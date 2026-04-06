// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;

namespace Egodystonic.TinyFFR;

// public readonly partial struct WorldSphere : IWorldConvexShape<WorldSphere, Sphere> {
// 	public Sphere BaseShape { get; init; }
// 	public Location CenterPoint { get; init; }
//
// 	public WorldSphere(Sphere baseShape, Location centerPoint) {
// 		BaseShape = baseShape;
// 		CenterPoint = centerPoint;
// 	}
//
// 	public float Radius {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.Radius;
// 	}
// 	public float RadiusSquared {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.RadiusSquared;
// 	}
// 	public float Volume {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.Volume;
// 	}
// 	public float SurfaceArea {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.SurfaceArea;
// 	}
// 	public float Circumference {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.Circumference;
// 	}
// 	public float Diameter {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => BaseShape.Diameter;
// 	}
// }