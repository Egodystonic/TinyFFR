// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class ObjectPoolTest {
	class MockDisposable : IDisposable {
		public bool IsDisposed { get; set; } = false;
		public void Dispose() => IsDisposed = true;
	}
	
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
		_argPool.Dispose(false);
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
		_argPool.Dispose(invokeDisposeOnEachItemBeforeRelease: false);

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
	public void ShouldCorrectlyDisposeContainedItemsWhenRequested() {
		var items = new List<MockDisposable>();
		
		static MockDisposable CreateDisposable(List<MockDisposable> list) {
			var result = new MockDisposable();
			list.Add(result);
			return result;
		}

		var pool = new ObjectPool<MockDisposable, List<MockDisposable>>(&CreateDisposable, items, 4);

		var rented1 = pool.Rent();
		var rented2 = pool.Rent();
		pool.Return(rented1);
		pool.Return(rented2);

		pool.Dispose(true);

		Assert.IsTrue(rented1.IsDisposed);
		Assert.IsTrue(rented2.IsDisposed);
		foreach (var item in items) Assert.IsTrue(item.IsDisposed);
		
		items.Clear();
		pool = new ObjectPool<MockDisposable, List<MockDisposable>>(&CreateDisposable, items, 4);

		rented1 = pool.Rent();
		rented2 = pool.Rent();
		pool.Return(rented1);
		pool.Return(rented2);

		pool.ReleasePooledObjects(true);

		Assert.IsTrue(rented1.IsDisposed);
		Assert.IsTrue(rented2.IsDisposed);
		foreach (var item in items) Assert.IsTrue(item.IsDisposed);
		
		
		
		
		items.Clear();
		pool = new ObjectPool<MockDisposable, List<MockDisposable>>(&CreateDisposable, items, 4);

		rented1 = pool.Rent();
		rented2 = pool.Rent();
		pool.Return(rented1);
		pool.Return(rented2);

		pool.Dispose(false);

		Assert.IsFalse(rented1.IsDisposed);
		Assert.IsFalse(rented2.IsDisposed);
		foreach (var item in items) Assert.IsFalse(item.IsDisposed);
		
		items.Clear();
		pool = new ObjectPool<MockDisposable, List<MockDisposable>>(&CreateDisposable, items, 4);

		rented1 = pool.Rent();
		rented2 = pool.Rent();
		pool.Return(rented1);
		pool.Return(rented2);

		pool.ReleasePooledObjects(false);

		Assert.IsFalse(rented1.IsDisposed);
		Assert.IsFalse(rented2.IsDisposed);
		foreach (var item in items) Assert.IsFalse(item.IsDisposed);
	}

	[Test]
	public void ShouldCorrectlyReleasePooledObjects() {
		var trackers = new List<MockDisposable>();
		static MockDisposable CreateTracker(List<MockDisposable> list) {
			var tracker = new MockDisposable();
			list.Add(tracker);
			return tracker;
		}

		var pool = new ObjectPool<MockDisposable, List<MockDisposable>>(&CreateTracker, trackers, 0);

		var rented1 = pool.Rent();
		pool.Return(rented1);

		pool.ReleasePooledObjects(true);
		Assert.IsTrue(rented1.IsDisposed);

		var rented2 = pool.Rent();
		Assert.That(rented2, Is.Not.SameAs(rented1));
		pool.Return(rented2);

		pool.ReleasePooledObjects(false);
		Assert.IsFalse(rented2.IsDisposed);

		var rented3 = pool.Rent();
		Assert.That(rented3, Is.Not.SameAs(rented2));

		pool.Dispose(false);
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