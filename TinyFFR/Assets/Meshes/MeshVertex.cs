// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct MeshVertex : IEquatable<MeshVertex> {
	readonly float _locX, _locY, _locZ;
	readonly float _texU, _texV;

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

	public MeshVertex(Location location, XYPair<float> textureCoords) {
		Location = location;
		TextureCoords = textureCoords;
	}

	public override string ToString() => $"{nameof(MeshVertex)}: {nameof(Location)} {Location}; {nameof(TextureCoords)} {TextureCoords}";

	public bool Equals(MeshVertex other) {
		return _locX.Equals(other._locX) && _locY.Equals(other._locY) && _locZ.Equals(other._locZ) && _texU.Equals(other._texU) && _texV.Equals(other._texV);
	}

	public override bool Equals(object? obj) {
		return obj is MeshVertex other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(_locX, _locY, _locZ, _texU, _texV);
	}

	public static bool operator ==(MeshVertex left, MeshVertex right) { return left.Equals(right); }
	public static bool operator !=(MeshVertex left, MeshVertex right) { return !left.Equals(right); }
}