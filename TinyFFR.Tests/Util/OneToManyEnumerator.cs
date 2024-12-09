// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using StrEnumerator = Egodystonic.TinyFFR.ReferentEnumerator<string, char>;

namespace Egodystonic.TinyFFR;

[TestFixture]
unsafe class ReferentEnumeratorTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static int GetCountFunc(string @this) => @this.Length;
	static char GetItemFunc(string @this, int index) => @this[index];

	StrEnumerator CreateEnumerator(string str) => new(str, &GetCountFunc, &GetItemFunc);

	[Test]
	public void ShouldThrowIfGivenNullFunctionPointers() {
		Assert.Throws<ArgumentNullException>(() => _ = new StrEnumerator("", null, &GetItemFunc));
		Assert.Throws<ArgumentNullException>(() => _ = new StrEnumerator("", &GetCountFunc, null));
	}

	[Test]
	public void ShouldThrowIfBadlyConstructed() {
		Assert.Throws<InvalidObjectException>(() => _ = default(StrEnumerator).Count);
		Assert.Throws<InvalidObjectException>(() => _ = default(StrEnumerator)[0]);
		Assert.Throws<InvalidObjectException>(() => _ = default(StrEnumerator).ElementAt(0));
		Assert.Throws<InvalidObjectException>(() => default(StrEnumerator).CopyTo(new Span<char>()));
		Assert.Throws<InvalidObjectException>(() => _ = default(StrEnumerator).TryCopyTo(new Span<char>()));
		// ReSharper disable once NotDisposedResourceIsReturned
		Assert.Throws<InvalidObjectException>(() => _ = default(StrEnumerator).GetEnumerator());
	}

	[Test]
	public void ShouldCorrectlyIndexAndEnumerate() {
		Assert.AreEqual(0, CreateEnumerator("").Count);
		Assert.AreEqual(0, CreateEnumerator("").ToArray().Length);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateEnumerator("")[0]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateEnumerator("")[1]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateEnumerator("")[-1]);

		Assert.AreEqual(5, CreateEnumerator("hello").Count);
		Assert.AreEqual(5, CreateEnumerator("hello").ToArray().Length);
		Assert.AreEqual("hello", new String(CreateEnumerator("hello").ToArray()));
		Assert.AreEqual('h', CreateEnumerator("hello")[0]);
		Assert.AreEqual('e', CreateEnumerator("hello")[1]);
		Assert.AreEqual('l', CreateEnumerator("hello")[2]);
		Assert.AreEqual('l', CreateEnumerator("hello")[3]);
		Assert.AreEqual('o', CreateEnumerator("hello")[4]);
		Assert.AreEqual('h', CreateEnumerator("hello").ElementAt(0));
		Assert.AreEqual('e', CreateEnumerator("hello").ElementAt(1));
		Assert.AreEqual('l', CreateEnumerator("hello").ElementAt(2));
		Assert.AreEqual('l', CreateEnumerator("hello").ElementAt(3));
		Assert.AreEqual('o', CreateEnumerator("hello").ElementAt(4));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateEnumerator("").ElementAt(5));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateEnumerator("").ElementAt(-1));
	}

	[Test]
	public void ShouldCorrectlyCopyToSpan() {
		var dest = new char[5] { '1', '2', '3', '4', '5' };
		CreateEnumerator("").CopyTo(dest);
		Assert.AreEqual('1', dest[0]);
		Assert.AreEqual('2', dest[1]);
		Assert.AreEqual('3', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);

		CreateEnumerator("abc").CopyTo(dest);
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('b', dest[1]);
		Assert.AreEqual('c', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);

		Assert.IsTrue(CreateEnumerator("").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('b', dest[1]);
		Assert.AreEqual('c', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);
		Assert.IsTrue(CreateEnumerator("apple").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('p', dest[1]);
		Assert.AreEqual('p', dest[2]);
		Assert.AreEqual('l', dest[3]);
		Assert.AreEqual('e', dest[4]);
		Assert.IsFalse(CreateEnumerator("sextet").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('p', dest[1]);
		Assert.AreEqual('p', dest[2]);
		Assert.AreEqual('l', dest[3]);
		Assert.AreEqual('e', dest[4]);
	}
}