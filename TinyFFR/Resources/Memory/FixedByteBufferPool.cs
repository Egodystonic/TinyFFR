// Created on 2024-01-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class FixedByteBufferPool : IDisposable {
	const int NumBlocksPerSpace = 40;

	public readonly record struct FixedByteBuffer {
		public UIntPtr StartPtr { get; }
		public int SizeBytes { get; }
		internal int SpaceIndex { get; }
		internal int BlockIndex { get; }

		public Span<byte> AsByteSpan => new((void*) StartPtr, SizeBytes);
		public ReadOnlySpan<byte> AsReadOnlyByteSpan => new((void*) StartPtr, SizeBytes);

		internal FixedByteBuffer(UIntPtr startPtr, int sizeBytes, int spaceIndex, int blockIndex) {
			StartPtr = startPtr;
			SizeBytes = sizeBytes;
			SpaceIndex = spaceIndex;
			BlockIndex = blockIndex;
		}

		public Span<T> AsSpan<T>(int numElements) where T : unmanaged {
			if (numElements < 0 || numElements * sizeof(T) > SizeBytes) {
				throw new ArgumentException($"This buffer's size is {SizeBytes} bytes, allocating a span for {numElements} {typeof(T).Name} elements would require at least {numElements * sizeof(T)} bytes.", nameof(numElements));
			}
			return new Span<T>((void*) StartPtr, numElements);
		}
		public ReadOnlySpan<T> AsReadOnlySpan<T>(int numElements) where T : unmanaged => AsSpan<T>(numElements);

		public int Size<T>() where T : unmanaged => SizeBytes / sizeof(T);
		public Span<T> AsSpan<T>() where T : unmanaged => AsSpan<T>(Size<T>());
		public ReadOnlySpan<T> AsReadOnlySpan<T>() where T : unmanaged => AsSpan<T>();

		internal void ThrowIfInvalid() {
			if (StartPtr == UIntPtr.Zero) throw InvalidObjectException.InvalidDefault<FixedByteBuffer>();
		}
	}
	[InlineArray(NumBlocksPerSpace)]
	struct BlockLedger { bool _; }
	struct AllocatedSpace {
		public readonly byte* StartPtr;
		public int LargestContiguousMemoryBlockStartIndex;
		public int LargestContiguousMemoryBlockCount;
		public BlockLedger RentedBlocksLedger;

		public AllocatedSpace(byte* startPtr, int largestContiguousMemoryBlockStartIndex, int largestContiguousMemoryBlockCount, BlockLedger ledger) {
			StartPtr = startPtr;
			LargestContiguousMemoryBlockStartIndex = largestContiguousMemoryBlockStartIndex;
			LargestContiguousMemoryBlockCount = largestContiguousMemoryBlockCount;
			RentedBlocksLedger = ledger;
		}
	}

	public int MaxBufferSizeBytes => _blockSize * NumBlocksPerSpace;

	readonly int _blockSize;
	readonly int _blockSizeLessOne;
	readonly string _overSizedRentalRequestExceptionMessage;
	bool _isDisposed = false;
	AllocatedSpace* _allocatedSpaces;
	int _numAllocatedSpaces;

	public FixedByteBufferPool(int largestRequiredBufferSizeBytes, string overSizedRentalRequestExceptionMessage) {
		if (largestRequiredBufferSizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(largestRequiredBufferSizeBytes), largestRequiredBufferSizeBytes, $"Largest required buffer size must be at least 1 byte.");
		ArgumentNullException.ThrowIfNull(overSizedRentalRequestExceptionMessage);
		_overSizedRentalRequestExceptionMessage = overSizedRentalRequestExceptionMessage;
		_blockSizeLessOne = (largestRequiredBufferSizeBytes / NumBlocksPerSpace);
		_blockSize = _blockSizeLessOne + 1;
		_allocatedSpaces = (AllocatedSpace*) NativeMemory.Alloc((nuint) sizeof(AllocatedSpace));
		_numAllocatedSpaces = 1;
		AllocateNewSpaceAtEndOfList();
	}
	
	public static FixedByteBufferPool CreateFromUserConfigurableParameter(int largestRequiredBufferSizeBytes, [CallerArgumentExpression(nameof(largestRequiredBufferSizeBytes))] string? parameterName = null) {
		ArgumentNullException.ThrowIfNull(parameterName);
		return new FixedByteBufferPool(
			largestRequiredBufferSizeBytes,
			$"An operation has failed because a request was made to an internal buffer for an allocation larger than the maximum size the buffer can provide. " +
			$"To fix this, increase the size of the '{parameterName.Split(".")[^1]}' property to increase the size of the internal buffer." + System.Environment.NewLine +
			$"Current max buffer size (bytes) is {largestRequiredBufferSizeBytes}."
		);
	}  

	public int GetMaxBufferSize<T>() where T : unmanaged => MaxBufferSizeBytes / sizeof(T);

	public FixedByteBuffer Rent<T>(int numElementsMinimum) where T : unmanaged => Rent(sizeof(T) * numElementsMinimum);
	public FixedByteBuffer Rent(int numBytesMinimum) {
		ThrowIfThisIsDisposed();
		var numBlocksRequired = (numBytesMinimum + _blockSizeLessOne) / _blockSize;
		for (var i = 0; i < _numAllocatedSpaces; ++i) {
			if (_allocatedSpaces[i].LargestContiguousMemoryBlockCount >= numBlocksRequired) return RentBlocksFromSpace(i, numBlocksRequired);
		}

		if (numBlocksRequired > NumBlocksPerSpace) {
			throw new InvalidOperationException(_overSizedRentalRequestExceptionMessage + System.Environment.NewLine + "Requested buffer size (bytes) was " + numBytesMinimum + ".");
		}
		IncreaseSpaceListSize();
		AllocateNewSpaceAtEndOfList();
		return RentBlocksFromSpace(_numAllocatedSpaces - 1, numBlocksRequired);
	}

	public void Return(FixedByteBuffer buffer) {
		ThrowIfThisIsDisposed();
		buffer.ThrowIfInvalid();
		var numBlocks = buffer.SizeBytes / _blockSize;
		if (buffer.SpaceIndex < 0 || buffer.SpaceIndex >= _numAllocatedSpaces || buffer.BlockIndex < 0 || numBlocks + buffer.BlockIndex > NumBlocksPerSpace) {
			throw new ArgumentException(
				$"Given buffer has invalid state. " +
				$"Buffer: {buffer} | " +
				$"{nameof(_numAllocatedSpaces)}: {_numAllocatedSpaces} | " +
				$"{nameof(_blockSize)}: {_blockSize} | " +
				$"{nameof(NumBlocksPerSpace)}: {NumBlocksPerSpace}", 
				nameof(buffer)
			);
		}
		var spacePtr = _allocatedSpaces + buffer.SpaceIndex;
		WriteBlocksAndRecalculateLargestContiguousSpace(spacePtr, buffer.BlockIndex, numBlocks, false);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			for (var i = 0; i < _numAllocatedSpaces; ++i) {
				NativeMemory.Free((_allocatedSpaces + i)->StartPtr);
			}
			NativeMemory.Free(_allocatedSpaces);
		}
		finally {
			_isDisposed = true;
		}
	}

	void IncreaseSpaceListSize() {
		var newListPtr = NativeMemory.Alloc((nuint) (sizeof(AllocatedSpace) * (_numAllocatedSpaces + 1)));
		NativeMemory.Copy(_allocatedSpaces, newListPtr, (nuint) (sizeof(AllocatedSpace) * _numAllocatedSpaces));
		NativeMemory.Free(_allocatedSpaces);
		_allocatedSpaces = (AllocatedSpace*) newListPtr;
		_numAllocatedSpaces++;
	}

	void AllocateNewSpaceAtEndOfList() {
		var newSpace = NativeMemory.Alloc((nuint) (_blockSize * NumBlocksPerSpace));
		_allocatedSpaces[_numAllocatedSpaces - 1] = new AllocatedSpace((byte*) newSpace, 0, NumBlocksPerSpace, new());
	}

	FixedByteBuffer RentBlocksFromSpace(int spaceIndex, int numBlocks) {
		ArgumentOutOfRangeException.ThrowIfLessThan(spaceIndex, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(spaceIndex, _numAllocatedSpaces);
		var spacePtr = _allocatedSpaces + spaceIndex;
		ArgumentOutOfRangeException.ThrowIfLessThan(spacePtr->LargestContiguousMemoryBlockCount, numBlocks, nameof(numBlocks));
		var result = new FixedByteBuffer(
			(UIntPtr) (spacePtr->StartPtr + (spacePtr->LargestContiguousMemoryBlockStartIndex * _blockSize)), 
			numBlocks * _blockSize, 
			spaceIndex, 
			spacePtr->LargestContiguousMemoryBlockStartIndex
		);
		WriteBlocksAndRecalculateLargestContiguousSpace(spacePtr, spacePtr->LargestContiguousMemoryBlockStartIndex, numBlocks, true);
		return result;
	}

	void WriteBlocksAndRecalculateLargestContiguousSpace(AllocatedSpace* spacePtr, int firstBlockIndex, int numBlocks, bool value) {
		for (var i = firstBlockIndex; i < firstBlockIndex + numBlocks; ++i) {
			spacePtr->RentedBlocksLedger[i] = value;
		}
		var largestContiguousBlockStartIndex = -1;
		var largestContiguousBlockSize = 0;
		var currentBlockSize = 0;
		for (var i = 0; i < NumBlocksPerSpace; ++i) {
			if (spacePtr->RentedBlocksLedger[i]) {
				if (currentBlockSize > largestContiguousBlockSize) {
					largestContiguousBlockSize = currentBlockSize;
					largestContiguousBlockStartIndex = i - currentBlockSize;
				}
				currentBlockSize = 0;
			}
			else {
				++currentBlockSize;
			}
		}
		if (currentBlockSize > largestContiguousBlockSize) {
			largestContiguousBlockSize = currentBlockSize;
			largestContiguousBlockStartIndex = NumBlocksPerSpace - currentBlockSize;
		}
		spacePtr->LargestContiguousMemoryBlockStartIndex = largestContiguousBlockStartIndex;
		spacePtr->LargestContiguousMemoryBlockCount = largestContiguousBlockSize;
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(FixedByteBufferPool));
	}
}