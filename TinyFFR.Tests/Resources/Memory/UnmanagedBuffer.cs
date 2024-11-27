// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class UnmanagedBufferTest {
	const int InitialLength = 30;
	const int Alignment = 16;
	UnmanagedBuffer<Vect> _buffer;

	[SetUp]
	public void SetUpTest() {
		_buffer = new(InitialLength, Alignment);
		for (var i = 0; i < InitialLength; ++i) {
			_buffer[i] = new(i, i * 2, i * 3);
		}
	}

	[TearDown]
	public void TearDownTest() {
		_buffer.Dispose();
	}

	[Test]
	public void ShouldCorrectlyEnumerate() {
		const int ArithmeticSum = ((InitialLength - 1) * InitialLength) / 2;
		var sum = new Vect(0f);
		
		foreach (var v in _buffer) sum += v;

		Assert.AreEqual(ArithmeticSum, sum.X);
		Assert.AreEqual(ArithmeticSum * 2, sum.Y);
		Assert.AreEqual(ArithmeticSum * 3, sum.Z);

		_buffer.DoubleSize();
		for (var i = 0; i < InitialLength; ++i) {
			_buffer[InitialLength + i] = new(i, i * 2, i * 3);
		}

		sum = new Vect(0f);
		foreach (var v in _buffer) sum += v;

		Assert.AreEqual(2 * ArithmeticSum, sum.X);
		Assert.AreEqual(2 * ArithmeticSum * 2, sum.Y);
		Assert.AreEqual(2 * ArithmeticSum * 3, sum.Z);
	}

	[Test]
	public void ShouldCorrectlyExposeBufferPointer() {
		var ptr = _buffer.BufferPointer;

		for (var i = 0; i < InitialLength; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), ptr[i]);
		}

		_buffer.DoubleSize();
		ptr = _buffer.BufferPointer;
		
		for (var i = 0; i < InitialLength; ++i) {
			ptr[i + InitialLength] = new Vect(i + InitialLength, (i + InitialLength) * 2, (i + InitialLength) * 3);
		}

		for (var i = 0; i < InitialLength * 2; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), ptr[i]);
		}
	}

	[Test]
	public void ShouldCorrectlyExposeSpan() {
		var span = _buffer.AsSpan;

		for (var i = 0; i < InitialLength; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), span[i]);
		}

		_buffer.DoubleSize();
		span = _buffer.AsSpan;

		for (var i = 0; i < InitialLength; ++i) {
			span[i + InitialLength] = new Vect(i + InitialLength, (i + InitialLength) * 2, (i + InitialLength) * 3);
		}

		for (var i = 0; i < InitialLength * 2; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), span[i]);
		}
	}

	[Test]
	public void ShouldCorrectlyExposeBufferStartRef() {
		ref var bsr = ref _buffer.BufferStartRef;

		bsr = new Vect(13f, 24f, 35f);
		Assert.AreEqual(new Vect(13f, 24f, 35f), _buffer[0]);

		_buffer.DoubleSize();
		bsr = ref _buffer.BufferStartRef;
		Assert.AreEqual(new Vect(13f, 24f, 35f), _buffer[0]);
		bsr = new Vect(130f, 240f, 350f);
		Assert.AreEqual(new Vect(130f, 240f, 350f), _buffer[0]);
	}

	[Test]
	public void ShouldBeCorrectlyAligned() {
		const int NumIterations = 10_000;

		var buffers = new List<UnmanagedBuffer<Vect>>();
		for (var i = 0; i < NumIterations; ++i) {
			var alignment = 1 << Random.Shared.Next(1, 5);
			var buffer = new UnmanagedBuffer<Vect>(3, alignment);
			buffers.Add(buffer);
			var ptr = buffer.BufferPointer;
			Assert.AreEqual(nuint.Zero, ((nuint) ptr) & (nuint) (alignment - 1));
			buffer.Resize(7);
			ptr = buffer.BufferPointer;
			Assert.AreEqual(nuint.Zero, ((nuint) ptr) & (nuint) (alignment - 1));
		}

		foreach (var buffer in buffers) buffer.Dispose();
	}

	[Test]
	public void IndexingShouldBeCorrectlyImplemented() {
		for (var i = 0; i < InitialLength; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer.GetAtIndex(i));

			_buffer[i] = new Vect(i + 1, i + 2, i + 3);

			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer[i]);
			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer.GetAtIndex(i));

			_buffer.SetAtIndex(i, new Vect(i, i * 2, i * 3));
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer.GetAtIndex(i));
		}

		_buffer.DoubleSize();

		for (var i = 0; i < InitialLength; ++i) {
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer.GetAtIndex(i));

			_buffer[i] = new Vect(i + 1, i + 2, i + 3);

			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer[i]);
			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer.GetAtIndex(i));

			_buffer.SetAtIndex(i, new Vect(i, i * 2, i * 3));
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer.GetAtIndex(i));
		}

		for (var i = InitialLength; i < InitialLength * 2; ++i) {
			_buffer[i] = new Vect(i + 1, i + 2, i + 3);

			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer[i]);
			Assert.AreEqual(new Vect(i + 1, i + 2, i + 3), _buffer.GetAtIndex(i));

			_buffer.SetAtIndex(i, new Vect(i, i * 2, i * 3));
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer.GetAtIndex(i));
		}
	}

	[Test]
	public void ShouldCorrectlyImplementResizing() {
		void ResizeAndAssertElements(int newLength) {
			var curLength = _buffer.Length;

			_buffer.Resize(newLength);
			Assert.AreEqual(newLength, _buffer.Length);
			Assert.AreEqual(newLength, _buffer.AsSpan.Length);

			for (var i = 0; i < Int32.Min(curLength, newLength); ++i) {
				Assert.AreEqual(new Vect(i, i * 2, i * 3), _buffer[i]);
			}
			for (var i = 0; i < newLength; ++i) {
				_buffer[i] = new Vect(i, i * 2, i * 3);
			}
		}

		Assert.AreEqual(InitialLength, _buffer.Length);
		Assert.AreEqual(InitialLength, _buffer.AsSpan.Length);

		Assert.Catch(() => _buffer.Resize(0));
		Assert.Catch(() => _buffer.Resize(-1));

		ResizeAndAssertElements(InitialLength - 3);
		ResizeAndAssertElements(InitialLength + 3);
	}
}