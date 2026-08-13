// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
readonly record struct MeshVertexImGui {
	internal const int ExpectedSerializedSize = 20;
	internal const int PositionByteOffset = 0;
	internal const int TextureCoordsByteOffset = 8;
	internal const int ColorByteOffset = 16;

	readonly float _posX, _posY;
	readonly float _texU, _texV;
	readonly TexelRgba32 _color;

	public XYPair<float> Position {
		get => new(_posX, _posY);
		init {
			_posX = value.X;
			_posY = value.Y;
		}
	}
	public XYPair<float> TextureCoords {
		get => new(_texU, _texV);
		init {
			_texU = value.X;
			_texV = value.Y;
		}
	}
	public TexelRgba32 Color {
		get => _color;
		init => _color = value;
	}

	public MeshVertexImGui(XYPair<float> position, XYPair<float> textureCoords, TexelRgba32 color) {
		Position = position;
		TextureCoords = textureCoords;
		Color = color;
	}
}
