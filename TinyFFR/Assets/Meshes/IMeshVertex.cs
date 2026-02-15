// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

interface IMeshVertex {
	public Location Location { get; init; }
	public XYPair<float> TextureCoords { get; init; }
	public Quaternion TangentRotation { get; init; }

	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) {
		CalculateTangentRotation(
			tangent.ToVector3(), 
			bitangent.ToVector3(), 
			normal.ToVector3(), 
			out var resultQuat
		).ThrowIfFailure();
		return resultQuat;
	}

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "calculate_tangent_rotation")]
	private static extern InteropResult CalculateTangentRotation(
		Vector3 tangent,
		Vector3 bitangent,
		Vector3 normal,
		out Quaternion outRot
	);
}