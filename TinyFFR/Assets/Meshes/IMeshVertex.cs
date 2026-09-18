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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "calculate_tangent_rotation")]
	private static extern InteropResult CalculateTangentRotation(
		Vector3 tangent,
		Vector3 bitangent,
		Vector3 normal,
		out Quaternion outRot
	);
}