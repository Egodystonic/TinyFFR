// Created on 2026-06-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

// Internal vertex format used for primitive rendering buffers (e.g. wireframe). Mirrors the native
// 'MeshVertexPrimitive' struct (Position FLOAT3 @ 0, Color FLOAT4 @ 12, Tangent FLOAT4 @ 28).
// For wireframe meshes the Color channel carries per-corner barycentric coordinates.
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
readonly record struct MeshVertexPrimitive {
	internal const int ExpectedSerializedSize = 44;
	readonly float _locX, _locY, _locZ;
	readonly float _colR, _colG, _colB, _colA;
	readonly float _tanX, _tanY, _tanZ, _tanW;

	public Location Location {
		get => new(_locX, _locY, _locZ);
		init {
			_locX = value.X;
			_locY = value.Y;
			_locZ = value.Z;
		}
	}
	public Vector4 Color {
		get => new(_colR, _colG, _colB, _colA);
		init {
			_colR = value.X;
			_colG = value.Y;
			_colB = value.Z;
			_colA = value.W;
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

	public MeshVertexPrimitive(Location location, Vector4 color, Quaternion tangentRotation) {
		Location = location;
		Color = color;
		TangentRotation = tangentRotation;
	}
}
