// Created on 2024-11-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ArrayPoolBackedMapTest {
	const int NumValues = 1000;
	ArrayPoolBackedMap<int, int> _map = null!;

	[SetUp]
	public void SetUpTest() {
		_map = new();
		for (var i = 0; i < NumValues; ++i) _map.Add(i, i * 2);
	}

	[TearDown]
	public void TearDownTest() {
		_map.Dispose();
	}

	[Test]
	public void ShouldCorrectlyEnumerate() {
		var keys = new List<int>();
		var values = new List<int>();
		
		foreach (var kvp in _map) {
			keys.Add(kvp.Key);
			values.Add(kvp.Value);
		}

		Assert.AreEqual(NumValues, keys.Count);
		Assert.AreEqual(NumValues, values.Count);

		var keyLedger = new bool[NumValues];
		
		for (var i = 0; i < NumValues; ++i) {
			Assert.AreEqual(keys[i] * 2, values[i]);
			keyLedger[keys[i]] = true;
		}

		Assert.IsTrue(keyLedger.All(v => v));
		
		keyLedger = new bool[NumValues];

		for (var i = 0; i < NumValues; ++i) {
			var kvp = _map.GetPairAtIndex(i);
			Assert.AreEqual(kvp.Key * 2, kvp.Value);
			keyLedger[kvp.Key] = true;
		}

		Assert.IsTrue(keyLedger.All(v => v));

		keyLedger = new bool[NumValues];
		foreach (var key in _map.Keys) {
			keyLedger[key] = true;
		}
		Assert.IsTrue(keyLedger.All(v => v));

		keyLedger = new bool[NumValues];
		foreach (var value in _map.Values) {
			keyLedger[value / 2] = true;
		}
		Assert.IsTrue(keyLedger.All(v => v));
	}

	[Test]
	public void ShouldCorrectlySetCount() {
		Assert.AreEqual(NumValues, _map.Count);
		_map.Remove(1);
		Assert.AreEqual(NumValues - 1, _map.Count);
		_map.Remove(1);
		Assert.AreEqual(NumValues - 1, _map.Count);
		_map.Remove(-1);
		Assert.AreEqual(NumValues - 1, _map.Count);
		_map.Remove(2);
		Assert.AreEqual(NumValues - 2, _map.Count);
		_map.Add(NumValues, NumValues * 2);
		Assert.AreEqual(NumValues - 1, _map.Count);
	}

	[Test]
	public void ShouldCorrectlyAddItems() {
		_map.Add(NumValues, NumValues * 2);
		Assert.AreEqual(NumValues * 2, _map[NumValues]);
		_map.Add(new(NumValues + 1, NumValues * 2 + 2));
		Assert.AreEqual(NumValues * 2 + 2, _map[NumValues + 1]);
		_map[2] = 3;
		Assert.AreEqual(3, _map[2]);
	}

	[Test]
	public void ShouldCorrectlyClear() {
		_map.Clear();
		Assert.AreEqual(0, _map.Count);
		var result = 0;
		foreach (var kvp in _map) result += 1;
		Assert.AreEqual(0, result);
		
		_map.Add(1, 2);
		_map.Add(2, 4);
		_map.Add(3, 6);
		_map.ClearWithoutZeroingMemory();
		Assert.AreEqual(0, _map.Count);
		foreach (var kvp in _map) result += 1;
		Assert.AreEqual(0, result);
	}

	[Test]
	public void ShouldCorrectlyRemove() {
		void AssertItemMissing(int key) {
			Assert.IsFalse(_map.ContainsKey(key));
			Assert.IsTrue(_map.All(kvp => kvp.Key != key));
		}

		Assert.IsTrue(_map.Remove(3));
		Assert.AreEqual(NumValues - 1, _map.Count);
		AssertItemMissing(3);
		Assert.IsFalse(_map.Remove(3));
		Assert.AreEqual(NumValues - 1, _map.Count);
		Assert.IsTrue(_map.Remove(new KeyValuePair<int, int>(4, 8)));
		Assert.AreEqual(NumValues - 2, _map.Count);
		AssertItemMissing(4);
		Assert.IsFalse(_map.Remove(new KeyValuePair<int, int>(4, 8)));
		Assert.IsFalse(_map.Remove(new KeyValuePair<int, int>(5, 8)));
		Assert.AreEqual(10, _map[5]);
	}

	[Test]
	public void ShouldCorrectlyRetrieveValues() {
		Assert.IsTrue(_map.TryGetValue(3, out var v));
		Assert.AreEqual(6, v);
		Assert.IsFalse(_map.TryGetValue(-1, out v));
		Assert.AreEqual(6, _map[3]);
	}

	[Test]
	public void ShouldCorrectlyCopy() {
		var tooSmall = new KeyValuePair<int, int>[NumValues - 1];
		var bigEnough = new KeyValuePair<int, int>[NumValues + 1];

		Assert.Catch(() => _map.CopyTo(tooSmall, 0));
		Assert.Catch(() => _map.CopyTo(bigEnough, 2));
		Assert.Catch(() => _map.CopyTo(bigEnough, -1));
		_map.CopyTo(bigEnough, 1);
		Assert.AreEqual(default(KeyValuePair<int, int>), bigEnough[0]);

		var keyLedger = new bool[NumValues];

		for (var i = 0; i < NumValues; ++i) {
			var kvp = bigEnough[i + 1];
			Assert.AreEqual(kvp.Key * 2, kvp.Value);
			keyLedger[kvp.Key] = true;
		}
		Assert.IsTrue(keyLedger.All(v => v));

		var intArr = new int[NumValues];

		Assert.Catch(() => _map.CopyKeysTo(intArr[1..]));
		_map.CopyKeysTo(intArr);
		keyLedger = new bool[NumValues];
		for (var i = 0; i < NumValues; ++i) {
			keyLedger[intArr[i]] = true;
		}
		Assert.IsTrue(keyLedger.All(v => v));

		Assert.Catch(() => _map.CopyValuesTo(intArr[1..]));
		_map.CopyValuesTo(intArr);
		keyLedger = new bool[NumValues];
		for (var i = 0; i < NumValues; ++i) {
			keyLedger[intArr[i] / 2] = true;
		}
		Assert.IsTrue(keyLedger.All(v => v));
	}

	[Test]
	public void ShouldCorrectlyDetermineContains() {
		Assert.AreEqual(true, _map.ContainsKey(0));
		Assert.AreEqual(true, _map.ContainsKey(NumValues - 1));
		Assert.AreEqual(true, _map.Contains(new(4, 8)));
		Assert.AreEqual(false, _map.ContainsKey(-1));
		Assert.AreEqual(false, _map.Contains(new(4, 7)));
	}
}