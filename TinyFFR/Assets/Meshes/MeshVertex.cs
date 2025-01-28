// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 36)]
public readonly record struct MeshVertex {
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
	public Direction Tangent {
		get => new(_tanX, _tanY, _tanZ);
		init {
			_tanX = value.X;
			_tanY = value.Y;
			_tanZ = value.Z;
		}
	}
	public float TangentHandedness {
		get => _tanW;
		init => _tanW = value;
	}

	public MeshVertex(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal)
		: this(location, textureCoords, tangent, MathF.Sign(normal.Cross(tangent).Dot(bitangent))) { }
	public MeshVertex(Location location, XYPair<float> textureCoords, Direction tangent, float tangentHandedness) {
		Location = location;
		TextureCoords = textureCoords;
		Tangent = tangent;
		TangentHandedness = tangentHandedness;
	}
}