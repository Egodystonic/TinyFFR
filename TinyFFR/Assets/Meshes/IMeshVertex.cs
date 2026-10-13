// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// The data every kind of mesh vertex carries: where it is, where it maps to on a texture, and which way its surface faces.
/// </summary>
public interface IMeshVertex {
	/// <summary>
	/// Where this vertex sits, relative to the mesh's own origin.
	/// </summary>
	public Location Location { get; init; }
	/// <summary>
	/// Where on a texture this vertex maps to.
	/// </summary>
	/// <remarks>
	/// <c>(0, 0)</c> is the texture's bottom-left corner and <c>(1, 1)</c> its top-right. Values outside that range are
	/// permitted and cause the texture to repeat.
	/// </remarks>
	public XYPair<float> TextureCoords { get; init; }
	/// <summary>
	/// Which way the surface faces at this vertex, and how a texture is oriented across it.
	/// </summary>
	/// <remarks>
	/// This single rotation encodes all three of the tangent, bitangent and normal directions at once. It is not intuitive to
	/// write by hand; use <see cref="CalculateTangentRotation(Direction, Direction, Direction)"/> to derive it from those three directions instead.
	/// </remarks>
	public Quaternion TangentRotation { get; init; }

	/// <summary>
	/// Calculates the tangent rotation for a vertex from the three directions that describe its surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <i>tangent</i> points along the direction in which a texture's horizontal coordinate increases across the surface (i.e. positive-U);
	/// the <i>bitangent</i> along the direction in which its vertical coordinate increases (i.e. positive-V); and the <i>normal</i> points
	/// directly out of the front of the surface.
	/// </para>
	/// <para>
	/// Together these say both which way the surface faces and which way up any texture on it sits, which is what lighting and
	/// normal mapping need in order to work.
	/// </para>
	/// </remarks>
	/// <param name="tangent">The direction in which the texture's horizontal coordinate increases.</param>
	/// <param name="bitangent">The direction in which the texture's vertical coordinate increases.</param>
	/// <param name="normal">The direction pointing directly out of the front of the surface.</param>
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) {
		CalculateTangentRotation(
			tangent.ToVector3(), 
			bitangent.ToVector3(), 
			normal.ToVector3(), 
			out var resultQuat
		).ThrowIfFailure();
		return resultQuat;
	}

	internal static Quaternion CalculateTangentRotationManaged(Vector3 tangent, Vector3 bitangent, Vector3 normal) {
		const float Bias = 1f / 32767f;
		ReadOnlySpan<int> nextIndex = [1, 2, 0];

		var derivedBitangent = Vector3.Cross(normal, tangent);
		Span<float> m = stackalloc float[9];
		m[0] = tangent.X; m[1] = tangent.Y; m[2] = tangent.Z;
		m[3] = derivedBitangent.X; m[4] = derivedBitangent.Y; m[5] = derivedBitangent.Z;
		m[6] = normal.X; m[7] = normal.Y; m[8] = normal.Z;

		Span<float> xyz = stackalloc float[3];
		float w;
		var trace = m[0] + m[4] + m[8];
		if (trace > 0f) {
			var s = MathF.Sqrt(trace + 1f);
			w = 0.5f * s;
			s = 0.5f / s;
			xyz[0] = (m[5] - m[7]) * s;
			xyz[1] = (m[6] - m[2]) * s;
			xyz[2] = (m[1] - m[3]) * s;
		}
		else {
			var i = 0;
			if (m[4] > m[0]) i = 1;
			if (m[8] > m[i * 3 + i]) i = 2;
			var j = nextIndex[i];
			var k = nextIndex[j];
			var s = MathF.Sqrt((m[i * 3 + i] - (m[j * 3 + j] + m[k * 3 + k])) + 1f);
			xyz[i] = 0.5f * s;
			if (s != 0f) s = 0.5f / s;
			w = (m[j * 3 + k] - m[k * 3 + j]) * s;
			xyz[j] = (m[i * 3 + j] + m[j * 3 + i]) * s;
			xyz[k] = (m[i * 3 + k] + m[k * 3 + i]) * s;
		}

		var result = Quaternion.Normalize(new Quaternion(xyz[0], xyz[1], xyz[2], w));
		if (result.W < 0f) result = Quaternion.Negate(result);
		if (result.W < Bias) {
			var factor = (float) Math.Sqrt(1d - (double) Bias * Bias);
			result = new Quaternion(result.X * factor, result.Y * factor, result.Z * factor, Bias);
		}
		if (Vector3.Dot(Vector3.Cross(tangent, normal), bitangent) < 0f) result = Quaternion.Negate(result);
		return result;
	}

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "calculate_tangent_rotation")]
	private static extern InteropResult CalculateTangentRotation(
		Vector3 tangent,
		Vector3 bitangent,
		Vector3 normal,
		out Quaternion outRot
	);
}