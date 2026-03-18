using System.Reflection;

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ArrayPoolBackedStringKeyMapTest {
	const int DefaultKvpCount = 10;
	ArrayPoolBackedStringKeyMap<int> _map = null!;

	[SetUp]
	public void SetUpTest() {
		_map = new();
		for (var i = 0; i < DefaultKvpCount; ++i) _map.Add($"key{i}", i * 2);
	}

	[TearDown]
	public void TearDownTest() {
		_map.Dispose();
	}

	[Test]
	public void ShouldCorrectlyTrackValues() {
		Assert.AreEqual(DefaultKvpCount, _map.Count);
		_map.Remove("key1");
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		_map.Remove("key1");
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		_map.Remove("nonexistent");
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		_map.Remove("key2");
		Assert.AreEqual(DefaultKvpCount - 2, _map.Count);
		_map.Add("newkey", 100);
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		_map.Clear();
		Assert.AreEqual(0, _map.Count);
		
		
		for (var i = 0; i < DefaultKvpCount; ++i) {
			Assert.IsFalse(_map.ContainsKey($"key{i}"));
			_map.Add($"key{i}", i * 2);
			Assert.IsTrue(_map.ContainsKey($"key{i}"));
		}
		Assert.IsFalse(_map.ContainsKey("nonexistent"));
		Assert.IsFalse(_map.ContainsKey("key10"));

		_map.Add("added", 50);
		Assert.IsTrue(_map.ContainsKey("added"));

		_map.Remove("key5");
		Assert.IsFalse(_map.ContainsKey("key5"));
	}

	[Test]
	public void ShouldCorrectlyAddAndSetValues() {
		_map.Add("extra", 42);
		Assert.AreEqual(42, _map["extra"]);
		Assert.AreEqual(DefaultKvpCount + 1, _map.Count);

		Assert.Throws<InvalidOperationException>(() => _map.Add("key0", 0));
		Assert.Throws<InvalidOperationException>(() => _map.Add("extra", 0));
		
		Assert.AreEqual(42, _map["extra"]);
		Assert.AreEqual(DefaultKvpCount + 1, _map.Count);
		
		_map["key3"] = 333;
		Assert.AreEqual(333, _map["key3"]);
		Assert.AreEqual(DefaultKvpCount + 1, _map.Count);

		_map["brandnew"] = 123;
		Assert.AreEqual(123, _map["brandnew"]);
		Assert.AreEqual(DefaultKvpCount + 2, _map.Count);
	}

	[Test]
	public void ShouldCorrectlyRetrieveValues() {
		for (var i = 0; i < DefaultKvpCount; ++i) {
			Assert.AreEqual(i * 2, _map[$"key{i}"]);
		}

		Assert.IsTrue(_map.TryGetValue("key3", out var v));
		Assert.AreEqual(6, v);
		Assert.IsFalse(_map.TryGetValue("nonexistent", out _));

		Assert.Throws<KeyNotFoundException>(() => { _ = _map["nonexistent"]; });
	}

	[Test]
	public void ShouldCorrectlyRemoveValues() {
		Assert.IsTrue(_map.ContainsKey("key3"));
		Assert.IsTrue(_map.Remove("key3"));
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		Assert.IsFalse(_map.ContainsKey("key3"));

		Assert.IsFalse(_map.Remove("key3"));
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);
		Assert.IsFalse(_map.ContainsKey("key3"));

		Assert.IsFalse(_map.Remove("nonexistent"));
		Assert.AreEqual(DefaultKvpCount - 1, _map.Count);

		for (var i = 0; i < DefaultKvpCount; ++i) {
			if (i == 3) continue;
			Assert.AreEqual(i * 2, _map[$"key{i}"]);
		}
		
		_map.Add("key3", 6);
		Assert.AreEqual(DefaultKvpCount, _map.Count);
		Assert.IsTrue(_map.ContainsKey("key3"));
		
		for (var i = 0; i < DefaultKvpCount; ++i) {
			Assert.AreEqual(i * 2, _map[$"key{i}"]);
		}
	}

	[Test]
	public void ShouldCorrectlyEnumerate() {	
		var resultBuffer = new bool[DefaultKvpCount];

		foreach (var kvp in _map) {
			var keyStr = kvp.Key.AsNewStringObject;
			Assert.IsTrue(keyStr.StartsWith("key"));
			var index = int.Parse(keyStr[3..]);
			Assert.AreEqual(index * 2, kvp.Value);
			resultBuffer[index] = true;
		}
		Assert.IsTrue(resultBuffer.All(v => v));

		resultBuffer = new bool[DefaultKvpCount];
		foreach (var key in _map.Keys) {
			var keyStr = key.AsNewStringObject;
			var index = int.Parse(keyStr[3..]);
			resultBuffer[index] = true;
		}
		Assert.IsTrue(resultBuffer.All(v => v));

		resultBuffer = new bool[DefaultKvpCount];
		foreach (var value in _map.Values) {
			Assert.AreEqual(0, value % 2);
			resultBuffer[value / 2] = true;
		}
		Assert.IsTrue(resultBuffer.All(v => v));
		
		resultBuffer = new bool[DefaultKvpCount];
		for (var i = 0; i < _map.Count; ++i) {
			var kvp = _map.GetPairAtIndex(i);
			var keyStr = kvp.Key.AsNewStringObject;
			var index = int.Parse(keyStr[3..]);
			Assert.AreEqual(index * 2, kvp.Value);
			resultBuffer[index] = true;
		}
		Assert.IsTrue(resultBuffer.All(v => v));
		
		Assert.Throws<ArgumentOutOfRangeException>(() => _map.GetPairAtIndex(-1));
		Assert.Throws<ArgumentOutOfRangeException>(() => _map.GetPairAtIndex(DefaultKvpCount));
	}

	[Test, Timeout(300_000), Explicit]
	public void ShouldCorrectlyHandleHashCollisions() {
		var hashCodeHandlesMapFieldRef = typeof(ArrayPoolBackedStringKeyMap<int>).GetField("_hashCodeToHandlesMap", BindingFlags.Instance | BindingFlags.NonPublic)!;
		
		void ExecuteChecks(int expectedCount) {
			Console.WriteLine("Executing checks with expected count of " + expectedCount + "...");
			Assert.AreEqual(expectedCount, _map.Count);
			for (var i = 0; i < expectedCount; ++i) {
				Assert.AreEqual(i, _map[i.ToString()]);
			}
		}
		
		// This loop is designed to keep adding strings until we get a hash collision. It's theoretically possible that we never get one, but generally we do.
		// We only check for collisions once every 16384 adds to avoid slowing things down with the reflection check.
		// We have a timeout on this test; after 5 mins it's usually more prudent to re-roll the test with a different seed for the hash code algo.
		_map.Clear();
		for (var i = 0; i < Int32.MaxValue; ++i) {
			if ((i & (16384 - 1)) == 0) {
				if ((((ArrayPoolBackedMap<int, ArrayPoolBackedVector<ManagedStringPool.RentedStringHandle>>) hashCodeHandlesMapFieldRef.GetValue(_map)!)!).Count != _map.Count) {
					ExecuteChecks(i);
					return;
				}
			}
			_map.Add(i.ToString(), i);
		}
	}
}
