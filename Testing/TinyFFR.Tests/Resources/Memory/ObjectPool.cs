// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class ObjectPoolTest {
	const int InitialPoolCount = 10;
	const int ArgPoolArg = 5;
	ObjectPool<List<string>> _simplePool = null!;
	ObjectPool<List<string>, int> _argPool = null!;
	VectorPool<string> _vectorPool = null!;
	MapPool<string, string> _mapPool = null!;

	[SetUp]
	public void SetUpTest() {
		_simplePool = new(&DefaultCreateNewListMethod, InitialPoolCount);
		_argPool = new(&ArgCreateNewListMethod, ArgPoolArg, InitialPoolCount);
		_vectorPool = new(true, &CreateNewVectorMethod);
		_mapPool = new(true, &CreateNewMapMethod);
	}

	[TearDown]
	public void TearDownTest() {
		_simplePool.Dispose();
		_argPool.Dispose();
	}

	static List<string> DefaultCreateNewListMethod() {
		return new List<string> { "hello" };
	}
	static List<string> ArgCreateNewListMethod(int arg) {
		var result = new List<string>();
		for (var i = 0; i < arg; ++i) result.Add(new String('a', i));
		return result;
	}
	static ArrayPoolBackedVector<string> CreateNewVectorMethod() {
		var result = new ArrayPoolBackedVector<string>();
		for (var i = 0; i < 10; ++i) result.Add(i.ToString());
		return result;
	}
	static ArrayPoolBackedMap<string, string> CreateNewMapMethod() {
		var result = new ArrayPoolBackedMap<string, string>();
		for (var i = 0; i < 10; ++i) result.Add(i + "key", i + "val");
		return result;
	}

	[Test]
	public void ShouldCorrectlyInitializeRentedItems() {
		for (var i = 0; i < InitialPoolCount * 2; ++i) {
			var list = _simplePool.Rent();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("hello", list[0]);
		}

		for (var i = 0; i < InitialPoolCount * 2; ++i) {
			var list = _argPool.Rent();
			Assert.AreEqual(ArgPoolArg, list.Count);
			for (var j = 0; j < ArgPoolArg; ++j) {
				Assert.AreEqual(new String('a', j), list[j]);
			}
		}
	}

	[Test]
	public void ShouldNotDoubleLeaseItems() {
		static List<string> NullaryCreationFunc() => new();
		static List<string> UnaryCreationFunc(int _) => new();

		const int NumIterations = 10_000;

		_simplePool.Dispose();
		_argPool.Dispose();

		_simplePool = new(&NullaryCreationFunc);
		_argPool = new(&UnaryCreationFunc, 0);

		var rentedBuffersFromSimplePool = new List<List<string>>();
		var rentedBuffersFromArgPool = new List<List<string>>();

		for (var i = 0; i < NumIterations; ++i) {
			if (rentedBuffersFromSimplePool.Count > 0 && Random.Shared.Next(2) == 0) {
				var indexToReturn = Random.Shared.Next(rentedBuffersFromSimplePool.Count);
				var simpleList = rentedBuffersFromSimplePool[indexToReturn];
				var argList = rentedBuffersFromArgPool[indexToReturn];
				rentedBuffersFromSimplePool.RemoveAt(indexToReturn);
				rentedBuffersFromArgPool.RemoveAt(indexToReturn);
				Assert.AreEqual(1, simpleList.Count);
				Assert.AreEqual(1, argList.Count);
				simpleList.Clear();
				argList.Clear();
				_simplePool.Return(simpleList);
				_argPool.Return(argList);
			}
			else {
				var simpleList = _simplePool.Rent();
				simpleList.Add("a");
				rentedBuffersFromSimplePool.Add(simpleList);

				var argList = _argPool.Rent();
				argList.Add("a");
				rentedBuffersFromArgPool.Add(argList);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateAndClearVectors() {
		var rentedObjects = new List<ArrayPoolBackedVector<string>>();

		for (var i = 0; i < 100; ++i) {
			var v = _vectorPool.Rent();
			Assert.AreEqual(10, v.Count);
			rentedObjects.Add(v);
		}

		foreach (var r in rentedObjects) _vectorPool.Return(r);

		for (var i = 0; i < 100; ++i) {
			var v = _vectorPool.Rent();
			Assert.AreEqual(0, v.Count);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateAndClearMaps() {
		var rentedObjects = new List<ArrayPoolBackedMap<string, string>>();

		for (var i = 0; i < 100; ++i) {
			var v = _mapPool.Rent();
			Assert.AreEqual(10, v.Count);
			rentedObjects.Add(v);
		}

		foreach (var r in rentedObjects) _mapPool.Return(r);

		for (var i = 0; i < 100; ++i) {
			var v = _mapPool.Rent();
			Assert.AreEqual(0, v.Count);
		}
	}
}