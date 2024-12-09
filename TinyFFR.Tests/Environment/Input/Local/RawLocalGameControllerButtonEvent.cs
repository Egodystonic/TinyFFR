// Created on 2024-12-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class RawLocalGameControllerButtonEventTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<RawLocalGameControllerButtonEvent>(16);

	[Test]
	public void ShouldCorrectlyLayOutFields() {
		Span<byte> byteSpan = new byte[32];
		MemoryMarshal.Write(byteSpan[0..8], (nuint) 123);
		MemoryMarshal.Write(byteSpan[8..14], RawLocalGameControllerEventType.X);
		MemoryMarshal.Write(byteSpan[14..16], (short) 456);
		MemoryMarshal.Write(byteSpan[16..24], (nuint) 789);
		MemoryMarshal.Write(byteSpan[24..30], RawLocalGameControllerEventType.Y);
		MemoryMarshal.Write(byteSpan[30..32], (short) 100);

		Assert.AreEqual((nuint) 123, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[0..16]).Handle);
		Assert.AreEqual(RawLocalGameControllerEventType.X, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[0..16]).Type);
		Assert.AreEqual((short) 456, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[0..16]).NewValue);
		Assert.AreEqual((nuint) 789, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[16..32]).Handle);
		Assert.AreEqual(RawLocalGameControllerEventType.Y, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[16..32]).Type);
		Assert.AreEqual((short) 100, MemoryMarshal.AsRef<RawLocalGameControllerButtonEvent>(byteSpan[16..32]).NewValue);
	}
}