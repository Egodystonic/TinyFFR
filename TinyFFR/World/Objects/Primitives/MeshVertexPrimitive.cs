// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.World;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
readonly record struct MeshVertexPrimitive {
	internal const int ExpectedSerializedSize = 44;
	readonly float _locX, _locY, _locZ;
	readonly float _colorR, _colorG, _colorB, _colorA;
	readonly float _tanX, _tanY, _tanZ, _tanW;

	public Location Location {
		get => new(_locX, _locY, _locZ);
		init {
			_locX = value.X;
			_locY = value.Y;
			_locZ = value.Z;
		}
	}
	public ColorVect PremultipliedAlphaColor {
		get => new(_colorR, _colorG, _colorB, _colorA);
		init {
			_colorR = value.Red;
			_colorG = value.Green;
			_colorB = value.Blue;
			_colorA = value.Alpha;
		}
	}
	public Quaternion TangentRotation {
		get => new(_tanX, _tanY, _tanZ, _tanW);
		init {
			_tanX = value.X;
			_tanY = value.Y;
			_tanZ = value.Z;
			_tanW = value.W;
		}
	}

	public MeshVertexPrimitive(Location location, ColorVect premultipliedAlphaColor, Direction tangent, Direction bitangent, Direction normal)
		: this(location, premultipliedAlphaColor, CalculateTangentRotation(tangent, bitangent, normal)) { }
	public MeshVertexPrimitive(Location location, ColorVect premultipliedAlphaColor, Quaternion tangentRotation) {
		Location = location;
		PremultipliedAlphaColor = premultipliedAlphaColor;
		TangentRotation = tangentRotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) => IMeshVertex.CalculateTangentRotation(tangent, bitangent, normal);
}