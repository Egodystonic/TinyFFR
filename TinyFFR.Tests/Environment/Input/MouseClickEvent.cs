// Created on 2024-12-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class MouseClickEventTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<MouseClickEvent>(16);

	[Test]
	public void ShouldCorrectlyLayOutFields() {
		ReadOnlySpan<MouseClickEvent> eSpan = [
			new((1, 2), MouseKey.MouseLeft, 3),
			new((4, 5), MouseKey.MouseRight, 6),
		];

		var byteSpan = MemoryMarshal.AsBytes(eSpan);

		Assert.AreEqual(new XYPair<int>(1, 2), MemoryMarshal.AsRef<XYPair<int>>(byteSpan[0..8]));
		Assert.AreEqual(MouseKey.MouseLeft, MemoryMarshal.AsRef<MouseKey>(byteSpan[8..12]));
		Assert.AreEqual(3, MemoryMarshal.AsRef<int>(byteSpan[12..16]));
		Assert.AreEqual(new XYPair<int>(4, 5), MemoryMarshal.AsRef<XYPair<int>>(byteSpan[16..24]));
		Assert.AreEqual(MouseKey.MouseRight, MemoryMarshal.AsRef<MouseKey>(byteSpan[24..28]));
		Assert.AreEqual(6, MemoryMarshal.AsRef<int>(byteSpan[28..32]));
	}
}