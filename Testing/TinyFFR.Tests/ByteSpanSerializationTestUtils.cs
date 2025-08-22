// Created on 2024-05-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Reflection;

namespace Egodystonic.TinyFFR;

static class ByteSpanSerializationTestUtils {
	public static void AssertSpanRoundTripConversion<T>(params T[] inputs) where T : IByteSpanSerializable<T> {
		foreach (var input in inputs) {
			var span = new byte[T.GetSerializationByteSpanLength(input)];
			T.SerializeToBytes(span, input);
			Assert.AreEqual(input, T.DeserializeFromBytes(span));
		}
	}

	public static void AssertDeclaredSpanLength<T>(T sampleItem) where T : IByteSpanSerializable<T> {
		var span = new byte[T.GetSerializationByteSpanLength(sampleItem)];
		Assert.DoesNotThrow(() => T.SerializeToBytes(span, sampleItem));

		var spanCopy = new byte[span.Length];
		span.CopyTo(spanCopy.AsSpan());
		for (var i = 0; i < span.Length; ++i) span[i]++;
		T.SerializeToBytes(span, sampleItem);
		Assert.IsTrue(span.SequenceEqual(spanCopy));

		Assert.That(() => T.SerializeToBytes(span[1..], sampleItem), Throws.Exception);
		Assert.That(() => T.DeserializeFromBytes(span[1..]), Throws.Exception);
		Assert.AreEqual(sampleItem, T.DeserializeFromBytes(span));
	}

	public static void AssertDeclaredSpanLength<T>() where T : struct, IFixedLengthByteSpanSerializable<T> {
		AssertDeclaredSpanLength(default(T));
		Assert.AreEqual(T.SerializationByteSpanLength, T.GetSerializationByteSpanLength(default));
	}

	public static void AssertBytes<T>(T sampleItem, params byte[] expectation) where T : IByteSpanSerializable<T> {
		Assert.AreEqual(expectation.Length, T.GetSerializationByteSpanLength(sampleItem));
		var span = new byte[T.GetSerializationByteSpanLength(sampleItem)];
		T.SerializeToBytes(span, sampleItem);
		Assert.IsTrue(span.SequenceEqual(expectation));
		Assert.AreEqual(sampleItem, T.DeserializeFromBytes(expectation));
	}

	public static void AssertLittleEndianSingles<T>(T sampleItem, params float[] expectation) where T : IByteSpanSerializable<T> {
		var expectationBytes = new byte[expectation.Length * sizeof(float)];
		for (var i = 0; i < expectation.Length; ++i) {
			BinaryPrimitives.WriteSingleLittleEndian(expectationBytes.AsSpan()[(i * sizeof(float))..], expectation[i]);
		}
		AssertBytes(sampleItem, expectationBytes);
	}

	public static void AssertLittleEndianInt32s<T>(T sampleItem, params int[] expectation) where T : IByteSpanSerializable<T> {
		var expectationBytes = new byte[expectation.Length * sizeof(int)];
		for (var i = 0; i < expectation.Length; ++i) {
			BinaryPrimitives.WriteInt32LittleEndian(expectationBytes.AsSpan()[(i * sizeof(int))..], expectation[i]);
		}
		AssertBytes(sampleItem, expectationBytes);
	}
}