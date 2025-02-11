// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
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
	public Rotation TangentRotation {
		get => Rotation.FromQuaternionPreNormalized(new(_tanX, _tanY, _tanZ, _tanW));
		init {
			var quat = value.AsQuaternion;
			_tanX = quat.X;
			_tanY = quat.Y;
			_tanZ = quat.Z;
			_tanW = quat.W;
		}
	}
	public float TangentHandedness {
		get => _tanW;
		init => _tanW = value;
	}

	public MeshVertex(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal)
		: this(location, textureCoords, CalculateTangentRotation(tangent, bitangent, normal)) { }
	public MeshVertex(Location location, XYPair<float> textureCoords, Rotation tangentRotation) {
		Location = location;
		TextureCoords = textureCoords;
		TangentRotation = tangentRotation;
		// _tanX = 0f;
		// _tanY = 1f;
		// _tanZ = 0f;
		// _tanW = 3.05185094e-05f;
	}

	public static Rotation CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) {
		CalculateTangentRotation(
			tangent.ToVector3(), 
			bitangent.ToVector3(), 
			normal.ToVector3(), 
			out var resultQuat
		).ThrowIfFailure();
		return Rotation.FromQuaternionPreNormalized(resultQuat);
	}

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "calculate_tangent_rotation")]
	static extern InteropResult CalculateTangentRotation(
		Vector3 tangent,
		Vector3 bitangent,
		Vector3 normal,
		out Quaternion outRot
	);
}