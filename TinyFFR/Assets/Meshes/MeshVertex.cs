// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One vertex of an ordinary, non-animated mesh: where it is, where it maps to on a texture, and which way its surface faces.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
public readonly record struct MeshVertex : IMeshVertex {
	internal const int ExpectedSerializedSize = 36;
	readonly float _locX, _locY, _locZ;
	readonly float _texU, _texV;
	readonly float _tanX, _tanY, _tanZ, _tanW;

	/// <inheritdoc />
	public Location Location {
		get => new(_locX, _locY, _locZ);
		init {
			_locX = value.X;
			_locY = value.Y;
			_locZ = value.Z;
		}
	}
	/// <inheritdoc />
	public XYPair<float> TextureCoords {
		get => new(_texU, _texV);
		init {
			_texU = value.X;
			_texV = value.Y;
		}
	}
	/// <inheritdoc />
	public Quaternion TangentRotation {
		get => new(_tanX, _tanY, _tanZ, _tanW);
		init {
			_tanX = value.X;
			_tanY = value.Y;
			_tanZ = value.Z;
			_tanW = value.W;
		}
	}

	/// <summary>
	/// Constructs a new <see cref="MeshVertex"/>, deriving its tangent rotation from the three surface directions.
	/// </summary>
	/// <remarks>
	/// This is the convenient constructor: it takes the three directions that describe the surface at this vertex and works out
	/// the rotation that encodes them.
	/// </remarks>
	/// <param name="location">Where the vertex sits, relative to the mesh's own origin.</param>
	/// <param name="textureCoords">Where on a texture this vertex maps to, with <c>(0, 0)</c> the bottom-left corner.</param>
	/// <param name="tangent">The direction in which the texture's horizontal coordinate increases across the surface.</param>
	/// <param name="bitangent">The direction in which the texture's vertical coordinate increases across the surface.</param>
	/// <param name="normal">The direction pointing directly out of the front of the surface.</param>
	public MeshVertex(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal)
		: this(location, textureCoords, CalculateTangentRotation(tangent, bitangent, normal)) { }
	/// <summary>
	/// Constructs a new <see cref="MeshVertex"/> from an already-derived tangent rotation.
	/// </summary>
	/// <param name="location">Where the vertex sits, relative to the mesh's own origin.</param>
	/// <param name="textureCoords">Where on a texture this vertex maps to, with <c>(0, 0)</c> the bottom-left corner.</param>
	/// <param name="tangentRotation">The combined tangent, bitangent and normal, as a single rotation.</param>
	public MeshVertex(Location location, XYPair<float> textureCoords, Quaternion tangentRotation) {
		Location = location;
		TextureCoords = textureCoords;
		TangentRotation = tangentRotation;
	}

	/// <summary>
	/// Calculates the tangent rotation for a vertex from the three directions that describe its surface.
	/// </summary>
	/// <remarks>
	/// The <i>tangent</i> points along the direction in which a texture's horizontal coordinate increases across the surface;
	/// the <i>bitangent</i> along the direction in which its vertical coordinate increases; and the <i>normal</i> points
	/// directly out of the front of the surface.
	/// </remarks>
	/// <param name="tangent">The direction in which the texture's horizontal coordinate increases across the surface.</param>
	/// <param name="bitangent">The direction in which the texture's vertical coordinate increases across the surface.</param>
	/// <param name="normal">The direction pointing directly out of the front of the surface.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) => IMeshVertex.CalculateTangentRotation(tangent, bitangent, normal);
}