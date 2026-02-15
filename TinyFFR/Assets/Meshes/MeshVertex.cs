// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
public readonly record struct MeshVertex : IMeshVertex {
	internal const int ExpectedSerializedSize = 36;
	readonly float _locX, _locY, _locZ;
	readonly float _texU, _texV;
	readonly float _tanX, _tanY, _tanZ, _tanW;

	public Location Location {
		get => new(_locX, _locY, _locZ);
		init {
			_locX = value.X;
			_locY = value.Y;
			_locZ = value.Z;
		}
	}
	public XYPair<float> TextureCoords {
		get => new(_texU, _texV);
		init {
			_texU = value.X;
			_texV = value.Y;
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

	public MeshVertex(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal)
		: this(location, textureCoords, CalculateTangentRotation(tangent, bitangent, normal)) { }
	public MeshVertex(Location location, XYPair<float> textureCoords, Quaternion tangentRotation) {
		Location = location;
		TextureCoords = textureCoords;
		TangentRotation = tangentRotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) => IMeshVertex.CalculateTangentRotation(tangent, bitangent, normal);
}