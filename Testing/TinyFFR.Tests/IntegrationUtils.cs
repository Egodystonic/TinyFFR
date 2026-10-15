// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR;

[TestFixture]
unsafe class IntegrationUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static TexelRgba32[] CreateRandomTexels(int count, int seed) {
		var rng = new Random(seed);
		var result = new TexelRgba32[count];
		for (var i = 0; i < count; ++i) result[i] = new TexelRgba32((byte) rng.Next(256), (byte) rng.Next(256), (byte) rng.Next(256), (byte) rng.Next(256));
		return result;
	}

	static void AssertBgra(ReadOnlySpan<TexelRgba32> source, ReadOnlySpan<byte> destination) {
		var expected = new byte[source.Length * 4];
		for (var i = 0; i < source.Length; ++i) {
			expected[i * 4 + 0] = source[i].B;
			expected[i * 4 + 1] = source[i].G;
			expected[i * 4 + 2] = source[i].R;
			expected[i * 4 + 3] = source[i].A;
		}
		Assert.IsTrue(expected.AsSpan().SequenceEqual(destination[..expected.Length]));
	}

	[Test]
	public void BlitRgbaToBgraShouldSwapRedAndBlueForEveryLength() {
		for (var length = 0; length < 70; ++length) {
			var source = CreateRandomTexels(length, length);
			var destination = new byte[length * 4 + 8];
			IntegrationUtils.BlitRgbaToBgra(source, destination);
			AssertBgra(source, destination);
			Assert.IsTrue(destination.AsSpan(length * 4).ToArray().All(b => b == 0), $"Wrote beyond the end at length {length}");
		}
	}

	[Test]
	public void BlitFrameShouldHonourStrideAndSwizzle() {
		foreach (var dimensions in new XYPair<int>[] { (1, 1), (7, 3), (640, 360), (1921, 1081) }) {
			var source = CreateRandomTexels(dimensions.Area, dimensions.X);
			foreach (var padding in new[] { 0, 12 }) {
				foreach (var swap in new[] { true, false }) {
					var stride = dimensions.X * 4 + padding;
					var destination = new byte[stride * dimensions.Y];
					fixed (byte* destinationPtr = destination) {
						IntegrationUtils.BlitFrame(source, dimensions, destinationPtr, stride, swap);
					}

					var expected = new byte[destination.Length];
					for (var y = 0; y < dimensions.Y; ++y) {
						var sourceRow = MemoryMarshal.AsBytes(source.AsSpan(y * dimensions.X, dimensions.X));
						var expectedRow = expected.AsSpan(y * stride, dimensions.X * 4);
						sourceRow.CopyTo(expectedRow);
						if (!swap) continue;
						for (var x = 0; x < expectedRow.Length; x += 4) (expectedRow[x], expectedRow[x + 2]) = (expectedRow[x + 2], expectedRow[x]);
					}
					Assert.IsTrue(expected.AsSpan().SequenceEqual(destination), $"{dimensions}, padding {padding}, swap {swap}");
				}
			}
		}
	}
}
