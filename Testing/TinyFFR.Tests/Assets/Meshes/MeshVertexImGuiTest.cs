// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using NUnit.Framework;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshVertexImGuiTest {
	[Test]
	public void ShouldMatchImDrawVertLayoutExactly() {
		Assert.That(Unsafe.SizeOf<MeshVertexImGui>(), Is.EqualTo(20));
		Assert.That(MeshVertexImGui.ExpectedSerializedSize, Is.EqualTo(20));
		Assert.That(MeshVertexImGui.PositionByteOffset, Is.EqualTo(0));
		Assert.That(MeshVertexImGui.TextureCoordsByteOffset, Is.EqualTo(8));
		Assert.That(MeshVertexImGui.ColorByteOffset, Is.EqualTo(16));
	}

	[Test]
	public void ShouldRoundTripAllComponents() {
		var input = new MeshVertexImGui(new XYPair<float>(1.5f, -2.5f), new XYPair<float>(0.25f, 0.75f), new TexelRgba32(10, 20, 30, 40));

		Assert.That(input.Position, Is.EqualTo(new XYPair<float>(1.5f, -2.5f)));
		Assert.That(input.TextureCoords, Is.EqualTo(new XYPair<float>(0.25f, 0.75f)));
		Assert.That(input.Color, Is.EqualTo(new TexelRgba32(10, 20, 30, 40)));
	}

	[Test]
	public void ShouldWriteComponentsAtExpectedByteOffsets() {
		var input = new MeshVertexImGui(new XYPair<float>(1f, 2f), new XYPair<float>(3f, 4f), new TexelRgba32(1, 2, 3, 4));
		Span<MeshVertexImGui> vertexSpan = stackalloc MeshVertexImGui[1];
		vertexSpan[0] = input;
		var bytes = MemoryMarshal.AsBytes(vertexSpan);

		Assert.That(MemoryMarshal.Read<float>(bytes[MeshVertexImGui.PositionByteOffset..]), Is.EqualTo(1f));
		Assert.That(MemoryMarshal.Read<float>(bytes[(MeshVertexImGui.PositionByteOffset + 4)..]), Is.EqualTo(2f));
		Assert.That(MemoryMarshal.Read<float>(bytes[MeshVertexImGui.TextureCoordsByteOffset..]), Is.EqualTo(3f));
		Assert.That(MemoryMarshal.Read<float>(bytes[(MeshVertexImGui.TextureCoordsByteOffset + 4)..]), Is.EqualTo(4f));
		Assert.That(bytes[MeshVertexImGui.ColorByteOffset], Is.EqualTo(1));
		Assert.That(bytes[MeshVertexImGui.ColorByteOffset + 1], Is.EqualTo(2));
		Assert.That(bytes[MeshVertexImGui.ColorByteOffset + 2], Is.EqualTo(3));
		Assert.That(bytes[MeshVertexImGui.ColorByteOffset + 3], Is.EqualTo(4));
	}
}
