// Created on 2024-12-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class KeyboardOrMouseKeyEventTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<KeyboardOrMouseKeyEvent>(8);

	[Test]
	public void ShouldCorrectlyLayOutFields() {
		ReadOnlySpan<KeyboardOrMouseKeyEvent> eSpan = [
			new(KeyboardOrMouseKey.A, true),
			new(KeyboardOrMouseKey.B, false),
		];

		var byteSpan = MemoryMarshal.AsBytes(eSpan);

		Assert.AreEqual(KeyboardOrMouseKey.A, MemoryMarshal.AsRef<KeyboardOrMouseKey>(byteSpan[0..4]));
		Assert.AreEqual(true, (bool) MemoryMarshal.AsRef<InteropBool>(byteSpan[4..8]));
		Assert.AreEqual(KeyboardOrMouseKey.B, MemoryMarshal.AsRef<KeyboardOrMouseKey>(byteSpan[8..12]));
		Assert.AreEqual(false, (bool) MemoryMarshal.AsRef<InteropBool>(byteSpan[12..16]));
	}
}