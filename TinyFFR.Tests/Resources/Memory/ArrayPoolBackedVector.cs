// Created on 2024-11-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ArrayPoolBackedVectorTest {
	ArrayPoolBackedVector<string> _vector = null!;

	[SetUp]
	public void SetUpTest() {
		_vector = new(1) {
			"hello",
			"i",
			"am",
			"a",
			"test",
			"vector"
		};
	}

	[TearDown]
	public void TearDownTest() {
		_vector.Dispose();
	}

	[Test]
	public void ShouldCorrectlyEnumerate() {
		var result = "";
		foreach (var str in _vector) result += str + " ";
		Assert.AreEqual(
			"hello i am a test vector",
			result[..^1]
		);
	}

	[Test]
	public void ShouldCorrectlyExposeAsSpan() {
		var span = _vector.AsSpan;
		Assert.AreEqual(6, span.Length);
		Assert.AreEqual("hello", span[0]);
		Assert.AreEqual("i", span[1]);
		Assert.AreEqual("am", span[2]);
		Assert.AreEqual("a", span[3]);
		Assert.AreEqual("test", span[4]);
		Assert.AreEqual("vector", span[5]);
	}

	[Test]
	public void ShouldCorrectlySetCount() {
		Assert.AreEqual(6, _vector.Count);
		_vector.RemoveLast();
		Assert.AreEqual(5, _vector.Count);
		_vector.RemoveAt(0);
		Assert.AreEqual(4, _vector.Count);
		_vector.Remove("i");
		Assert.AreEqual(3, _vector.Count);
		_vector.Remove("j");
		Assert.AreEqual(3, _vector.Count);
		_vector.Add("123");
		Assert.AreEqual(4, _vector.Count);
	}

	[Test]
	public void ShouldCorrectlyAddItems() {
		_vector.Add("qwerty");
		Assert.AreEqual(7, _vector.Count);
		Assert.AreEqual("qwerty", _vector[6]);
		_vector.Add("uiop");
		Assert.AreEqual(8, _vector.Count);
		Assert.AreEqual("qwerty", _vector[6]);
		Assert.AreEqual("uiop", _vector[7]);
	}

	[Test]
	public void ShouldCorrectlyClear() {
		_vector.Clear();
		Assert.AreEqual(0, _vector.Count);
		var result = "";
		foreach (var str in _vector) result += str + " ";
		Assert.AreEqual("", result);
		Assert.AreEqual(0, _vector.AsSpan.Length);

		_vector.Add("test1");
		_vector.Add("test2");
		_vector.Add("test3");
		_vector.ClearWithoutZeroingMemory();
		Assert.AreEqual(0, _vector.Count);
		foreach (var str in _vector) result += str + " ";
		Assert.AreEqual("", result);
		Assert.AreEqual(0, _vector.AsSpan.Length);
	}

	[Test]
	public void ShouldCorrectlyRemove() {
		Assert.IsTrue(_vector.Remove("am"));
		Assert.AreEqual(5, _vector.Count);
		Assert.AreEqual("hello i a test vector", String.Join(" ", _vector));
		Assert.IsFalse(_vector.Remove("am"));
		Assert.AreEqual(5, _vector.Count);
		Assert.AreEqual("hello i a test vector", String.Join(" ", _vector));
		_vector.RemoveAt(4);
		Assert.AreEqual(4, _vector.Count);
		Assert.AreEqual("hello i a test", String.Join(" ", _vector));
		_vector.RemoveAt(0);
		Assert.AreEqual(3, _vector.Count);
		Assert.AreEqual("i a test", String.Join(" ", _vector));
		Assert.AreEqual("test", _vector.RemoveLast());
		Assert.AreEqual(2, _vector.Count);
		Assert.AreEqual("i a", String.Join(" ", _vector));
		Assert.AreEqual(true, _vector.TryRemoveLast(out var removedItem));
		Assert.AreEqual("a", removedItem);
		Assert.AreEqual(1, _vector.Count);
		Assert.AreEqual("i", String.Join(" ", _vector));
		Assert.AreEqual(true, _vector.TryRemoveLast(out removedItem));
		Assert.AreEqual("i", removedItem);
		Assert.AreEqual(0, _vector.Count);
		Assert.AreEqual("", String.Join(" ", _vector));
		Assert.AreEqual(false, _vector.TryRemoveLast(out removedItem));
		Assert.Catch(() => _vector.RemoveLast());
	}

	[Test]
	public void ShouldCorrectlyInsert() {
		_vector.Insert(0, "fluff");
		Assert.AreEqual("fluff hello i am a test vector", String.Join(" ", _vector));
		_vector.Insert(3, "squanch");
		Assert.AreEqual("fluff hello i squanch am a test vector", String.Join(" ", _vector));
		_vector.Insert(8, "oof");
		Assert.AreEqual("fluff hello i squanch am a test vector oof", String.Join(" ", _vector));
		Assert.Catch(() => _vector.Insert(10, "oof"));
	}

	[Test]
	public void ShouldCorrectlyIndex() {
		void TestIndex(string? expectation, int index) {
			if (expectation != null) {
				Assert.AreEqual(expectation, _vector[index]);
				_vector.GetValueByRef(index) += " test";
				Assert.AreEqual(expectation + " test", _vector[index]);
				Assert.AreEqual(index, _vector.IndexOf(expectation + " test"));
			}
			else {
				Assert.Catch(() => _ = _vector[index]);
				Assert.Catch(() => _ = _vector.GetValueByRef(index));
			}
		}
		TestIndex("hello", 0);
		TestIndex("i", 1);
		TestIndex("vector", 5);
		TestIndex(null, -1);
		TestIndex(null, 6);
	}

	[Test]
	public void ShouldCorrectlyCopy() {
		var tooSmall = new string[5];
		var bigEnough = new string[7];

		Assert.Catch(() => _vector.CopyTo(tooSmall, 0));
		Assert.Catch(() => _vector.CopyTo(bigEnough, 2));
		Assert.Catch(() => _vector.CopyTo(bigEnough, -1));
		_vector.CopyTo(bigEnough, 1);

		Assert.AreEqual(null, bigEnough[0]);
		Assert.AreEqual("hello", bigEnough[1]);
		Assert.AreEqual("i", bigEnough[2]);
		Assert.AreEqual("am", bigEnough[3]);
		Assert.AreEqual("a", bigEnough[4]);
		Assert.AreEqual("test", bigEnough[5]);
		Assert.AreEqual("vector", bigEnough[6]);
	}

	[Test]
	public void ShouldCorrectlyDetermineContains() {
		Assert.AreEqual(true, _vector.Contains("hello"));
		Assert.AreEqual(true, _vector.Contains("i"));
		Assert.AreEqual(true, _vector.Contains("vector"));
		Assert.AreEqual(false, _vector.Contains("vector1"));
		Assert.AreEqual(false, _vector.Contains(null!));
	}
}