// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One triangle of a mesh, given as three indices in to that mesh's vertex list.
/// </summary>
/// <remarks>
/// <para>
/// The order of the three indices sets the triangle's <i>winding order</i>, which is what tells the renderer which side of the
/// triangle is its front. In TinyFFR the three vertices must appear in anticlockwise order when viewed from the front face;
/// a triangle wound the other way is only drawn when seen from behind.
/// </para>
/// <para>
/// Only the relative order matters, not which index comes first: <c>(A, B, C)</c>, <c>(B, C, A)</c> and <c>(C, A, B)</c> all
/// describe the same triangle facing the same way.
/// </para>
/// </remarks>
/// <param name="IndexA">The index in to the vertex list of this triangle's first vertex.</param>
/// <param name="IndexB">The index in to the vertex list of this triangle's second vertex.</param>
/// <param name="IndexC">The index in to the vertex list of this triangle's third vertex.</param>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(int) * 3)]
public readonly record struct VertexTriangle(int IndexA, int IndexB, int IndexC) {
	/// <summary>
	/// Returns a copy of this triangle with the same value added to all three of its indices.
	/// </summary>
	/// <remarks>
	/// This is useful when concatenating meshes: the second mesh's triangles must be shifted by the number of vertices already
	/// present so that they continue to index their own vertices in the combined list.
	/// </remarks>
	/// <param name="indexShift">The value to add to each index. May be negative, but the resulting indices must still be valid
	/// positions in whichever vertex list this triangle is used with.</param>
	public VertexTriangle ShiftedBy(int indexShift) => new(IndexA + indexShift, IndexB + indexShift, IndexC + indexShift);
	/// <summary>
	/// Returns a copy of this triangle with its winding order reversed, so that the face which was its front becomes its back.
	/// </summary>
	/// <remarks>
	/// Use this when geometry from another source was authored with the opposite winding convention and is therefore invisible
	/// from the side you expect to see it from.
	/// </remarks>
	public VertexTriangle Flipped() => new(IndexA, IndexC, IndexB);
	/// <inheritdoc />
	public override string ToString() => $"<{IndexA}, {IndexB}, {IndexC}>";
}
