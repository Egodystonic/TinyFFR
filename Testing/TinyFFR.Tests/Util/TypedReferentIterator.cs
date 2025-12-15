// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using StrIterator = Egodystonic.TinyFFR.IndirectEnumerable<string, char>;

namespace Egodystonic.TinyFFR;

[TestFixture]
unsafe class IndirectEnumerableTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static int GetCountFunc(string @this) => @this.Length;
	static char GetItemFunc(string @this, int index) => @this[index];

	StrIterator CreateIterator(string str) => new(str, str.Length, &GetCountFunc, &GetCountFunc, &GetItemFunc);

	[Test]
	public void ShouldThrowIfGivenNullFunctionPointers() {
		Assert.Throws<ArgumentNullException>(() => _ = new StrIterator("", 0, null, &GetCountFunc, &GetItemFunc));
		Assert.Throws<ArgumentNullException>(() => _ = new StrIterator("", 0, &GetCountFunc, null, &GetItemFunc));
		Assert.Throws<ArgumentNullException>(() => _ = new StrIterator("", 0, &GetCountFunc, &GetCountFunc, null));
	}

	[Test]
	public void ShouldThrowIfBadlyConstructed() {
		Assert.Throws<InvalidObjectException>(() => _ = default(StrIterator).Count);
		Assert.Throws<InvalidObjectException>(() => _ = default(StrIterator)[0]);
		Assert.Throws<InvalidObjectException>(() => _ = default(StrIterator).ElementAt(0));
		Assert.Throws<InvalidObjectException>(() => default(StrIterator).CopyTo(new Span<char>()));
		Assert.Throws<InvalidObjectException>(() => _ = default(StrIterator).TryCopyTo(new Span<char>()));
		// ReSharper disable once NotDisposedResourceIsReturned
		Assert.Throws<InvalidObjectException>(() => _ = default(StrIterator).GetEnumerator());
	}

	[Test]
	public void ShouldCorrectlyIndexAndEnumerate() {
		Assert.AreEqual(0, CreateIterator("").Count);
		Assert.AreEqual(0, CreateIterator("").ToArray().Length);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateIterator("")[0]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateIterator("")[1]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateIterator("")[-1]);

		Assert.AreEqual(5, CreateIterator("hello").Count);
		Assert.AreEqual(5, CreateIterator("hello").ToArray().Length);
		Assert.AreEqual("hello", new String(CreateIterator("hello").ToArray()));
		Assert.AreEqual('h', CreateIterator("hello")[0]);
		Assert.AreEqual('e', CreateIterator("hello")[1]);
		Assert.AreEqual('l', CreateIterator("hello")[2]);
		Assert.AreEqual('l', CreateIterator("hello")[3]);
		Assert.AreEqual('o', CreateIterator("hello")[4]);
		Assert.AreEqual('h', CreateIterator("hello").ElementAt(0));
		Assert.AreEqual('e', CreateIterator("hello").ElementAt(1));
		Assert.AreEqual('l', CreateIterator("hello").ElementAt(2));
		Assert.AreEqual('l', CreateIterator("hello").ElementAt(3));
		Assert.AreEqual('o', CreateIterator("hello").ElementAt(4));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateIterator("").ElementAt(5));
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = CreateIterator("").ElementAt(-1));
	}

	[Test]
	public void ShouldCorrectlyCopyToSpan() {
		var dest = new[] { '1', '2', '3', '4', '5' };
		CreateIterator("").CopyTo(dest);
		Assert.AreEqual('1', dest[0]);
		Assert.AreEqual('2', dest[1]);
		Assert.AreEqual('3', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);

		CreateIterator("abc").CopyTo(dest);
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('b', dest[1]);
		Assert.AreEqual('c', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);

		Assert.IsTrue(CreateIterator("").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('b', dest[1]);
		Assert.AreEqual('c', dest[2]);
		Assert.AreEqual('4', dest[3]);
		Assert.AreEqual('5', dest[4]);
		Assert.IsTrue(CreateIterator("apple").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('p', dest[1]);
		Assert.AreEqual('p', dest[2]);
		Assert.AreEqual('l', dest[3]);
		Assert.AreEqual('e', dest[4]);
		Assert.IsFalse(CreateIterator("sextet").TryCopyTo(dest));
		Assert.AreEqual('a', dest[0]);
		Assert.AreEqual('p', dest[1]);
		Assert.AreEqual('p', dest[2]);
		Assert.AreEqual('l', dest[3]);
		Assert.AreEqual('e', dest[4]);
	}

	[Test]
	public void ShouldProtectAgainstReferentStateChanges() {
		static int GetCount(List<int> input) => input.Count;
		static int GetVersion(List<int> input) => input.Sum();
		static int GetItem(List<int> input, int index) => input[index];

		var testList = new List<int> { 1, 2, 3 };

		var iterator = new IndirectEnumerable<List<int>, int>(
			testList,
			6,
			&GetCount,
			&GetVersion,
			&GetItem
		);

		Assert.DoesNotThrow(() => iterator.CopyTo(new int[3]));
		Assert.DoesNotThrow(() => _ = iterator.TryCopyTo(new int[3]));
		Assert.DoesNotThrow(() => _ = iterator.Count);
		Assert.DoesNotThrow(() => iterator.ElementAt(0));
		Assert.DoesNotThrow(() => _ = iterator.Sum());
		Assert.DoesNotThrow(() => _ = iterator[0]);

		testList.Add(4);

		Assert.Catch<InvalidOperationException>(() => iterator.CopyTo(new int[3]));
		Assert.Catch<InvalidOperationException>(() => _ = iterator.TryCopyTo(new int[3]));
		Assert.Catch<InvalidOperationException>(() => _ = iterator.Count);
		Assert.Catch<InvalidOperationException>(() => iterator.ElementAt(0));
		Assert.Catch<InvalidOperationException>(() => _ = iterator.Sum());
		Assert.Catch<InvalidOperationException>(() => _ = iterator[0]);
	}
}