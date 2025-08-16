// Created on 2025-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Runtime.CompilerServices;
using System.Text;

namespace Egodystonic.TinyFFR.Interop;

[TestFixture]
unsafe class InteropStringBufferTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlySetProperties() {
		using var buffer = new InteropStringBuffer(30, false);
		Assert.AreEqual(30, buffer.Length);

		Random.Shared.NextBytes(buffer.AsSpan);
			
		for (var i = 0; i < buffer.Length; ++i) {
			Assert.AreEqual(buffer.AsSpan[i], buffer.AsPointer[i]);
			Assert.AreEqual(buffer.AsSpan[i], Unsafe.Add(ref buffer.AsRef, i));
		}

		using var buffer2 = new InteropStringBuffer(30, true);
		Assert.AreEqual(31, buffer2.Length);

		Random.Shared.NextBytes(buffer2.AsSpan);

		for (var i = 0; i < buffer2.Length; ++i) {
			Assert.AreEqual(buffer2.AsSpan[i], buffer2.AsPointer[i]);
			Assert.AreEqual(buffer2.AsSpan[i], Unsafe.Add(ref buffer2.AsRef, i));
		}
	}

	[Test]
	public void ShouldCorrectlyAndSafelyConvert() {
		using var buffer = new InteropStringBuffer(5, true);
		var dest = new char[5];

		Assert.AreEqual(1, buffer.ConvertFromUtf16(""));
		Assert.AreEqual(0, buffer.AsSpan[0]);
		Assert.AreEqual(0, buffer.GetUtf16Length());
		Assert.AreEqual(0, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("", buffer.ToString());

		Assert.AreEqual(4, buffer.ConvertFromUtf16("abc"));
		Assert.AreEqual("a"u8.ToArray().Single(), buffer.AsSpan[0]);
		Assert.AreEqual("b"u8.ToArray().Single(), buffer.AsSpan[1]);
		Assert.AreEqual("c"u8.ToArray().Single(), buffer.AsSpan[2]);
		Assert.AreEqual(0, buffer.AsSpan[3]);
		Assert.AreEqual(3, buffer.GetUtf16Length());
		Assert.AreEqual(3, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("abc", new String(dest[..3]));
		Assert.AreEqual("abc", buffer.ToString());

		Assert.AreEqual(6, buffer.ConvertFromUtf16("abcde"));
		Assert.AreEqual(0, buffer.AsSpan[5]);
		Assert.AreEqual(5, buffer.GetUtf16Length());
		Assert.AreEqual(5, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("abcde", new String(dest[..5]));
		Assert.AreEqual("abcde", buffer.ToString());

		Assert.AreEqual(6, buffer.ConvertFromUtf16("abcdef"));
		Assert.AreEqual(0, buffer.AsSpan[5]);
		Assert.AreEqual(5, buffer.GetUtf16Length());
		Assert.AreEqual(5, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("abcde", new String(dest[..5]));
		Assert.AreEqual("abcde", buffer.ToString());

		Assert.AreEqual(1, buffer.ConvertFromUtf16OrThrowIfBufferTooSmall("", ""));
		Assert.AreEqual(0, buffer.AsSpan[0]);
		Assert.AreEqual(0, buffer.GetUtf16Length());
		Assert.AreEqual(0, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("", buffer.ToString());

		Assert.AreEqual(4, buffer.ConvertFromUtf16OrThrowIfBufferTooSmall("abc", ""));
		Assert.AreEqual("a"u8.ToArray().Single(), buffer.AsSpan[0]);
		Assert.AreEqual("b"u8.ToArray().Single(), buffer.AsSpan[1]);
		Assert.AreEqual("c"u8.ToArray().Single(), buffer.AsSpan[2]);
		Assert.AreEqual(0, buffer.AsSpan[3]);
		Assert.AreEqual(3, buffer.GetUtf16Length());
		Assert.AreEqual(3, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("abc", new String(dest[..3]));
		Assert.AreEqual("abc", buffer.ToString());

		Assert.AreEqual(6, buffer.ConvertFromUtf16OrThrowIfBufferTooSmall("abcde", ""));
		Assert.AreEqual(0, buffer.AsSpan[5]);
		Assert.AreEqual(5, buffer.GetUtf16Length());
		Assert.AreEqual(5, buffer.ConvertToUtf16(dest));
		Assert.AreEqual("abcde", new String(dest[..5]));
		Assert.AreEqual("abcde", buffer.ToString());

		Assert.Catch(() => buffer.ConvertFromUtf16OrThrowIfBufferTooSmall("abcdef", ""));
	}
}