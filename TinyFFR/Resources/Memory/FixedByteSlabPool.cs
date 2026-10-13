// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class FixedByteSlabPool : IDisposable {
	// Configuration
	const int SmallestAllocationLog2 = 8;
	const int LargestSmallSizeClassAllocationLog2 = 18;
	const int NumLargeSizeClassDivisionsPerPowerOfTwo = 4;
	const int SmallSlabSizeBytes = MaxSmallSlotSizeBytes * 4;
	const int RetainedEmptySmallSlabsPerSizeClass = 1;
	const int InitialSlabRecordCapacity = 16;
	const int DefaultSlabAlignmentBytes = 64;
	const int HugePageSlabAlignmentBytes = 2 * 1024 * 1024;
	const int SizeClassIntArrayLength = 64; // Needs updating if NumSizeClasses exceeds this value
	
	// Derived Values or Named Constants
	public const int MinSlotSizeBytes = 1 << SmallestAllocationLog2;
	public const int MaxSmallSlotSizeBytes = 1 << LargestSmallSizeClassAllocationLog2;
	const int NumSmallSizeClasses = LargestSmallSizeClassAllocationLog2 - SmallestAllocationLog2 + 1;
	const int NumSizeClasses = NumSmallSizeClasses + (31 - LargestSmallSizeClassAllocationLog2) * NumLargeSizeClassDivisionsPerPowerOfTwo;
	const int NullSlab = -1;

	public readonly record struct SlabRental {
		public UIntPtr SlotPtr { get; }
		public int SizeBytes { get; }
		internal int SlabIndex { get; }

		public Span<byte> AsByteSpan => new((void*) SlotPtr, SizeBytes);

		internal SlabRental(UIntPtr slotPtr, int sizeBytes, int slabIndex) {
			SlotPtr = slotPtr;
			SizeBytes = sizeBytes;
			SlabIndex = slabIndex;
		}

		internal void ThrowIfInvalid() {
			if (SlotPtr == UIntPtr.Zero) throw InvalidObjectException.InvalidDefault<SlabRental>();
		}
	}

	struct SlabRecord {
		public byte* AllocationPtr;
		// FreeListHead: This is a pointer in to the slab's own data slots. The pointed-to data actually contains the address of the next free slot,
		// forming an 'intrusive' linked-list of free slots that's cache-friendly. FreeListHead is null if no such data exists (i.e. no
		// previously-returned slots), in which case LowestNeverRentedSlotIndex is bumped + used instead.
		// We could remove LowestNeverRentedSlotIndex if we pre-filled the slot data with a linked-list chain of free slots at the slab's creation,
		// but that's actually wasted effort when we can just add the data on request (especially as slabs are often relinquished before they're filled).
		public byte* FreeListHead; 
		public int SizeClassIndex;
		public int UsedSlotCount;
		public int LowestNeverRentedSlotIndex;
		public int NextNonFullSlabIndex;
		public int PrevNonFullSlabIndex;
	}

	[InlineArray(SizeClassIntArrayLength)]
	struct SizeClassIntArray { int _; }

	static readonly Lock _liveInstancesLock = new();
	static readonly List<FixedByteSlabPool> _liveInstances = new();

	readonly long _maxRetainedEmptySlabBytes;
	readonly long _retentionWindowDurationTicks;
	long _currentRetentionWindowEndTimestamp;
	bool _isDisposed = false;
	
	// Precalculated values
	readonly SizeClassIntArray _slotSizePrecalculationTable;
	readonly SizeClassIntArray _slotCountPrecalculationTable;
	readonly SizeClassIntArray _slabSizePrecalculationTable;
	readonly SizeClassIntArray _slabAlignmentPrecalculationTable;
	
	// Slab record keeping
	int _slabRecordsLength;
	SlabRecord* _slabRecords;
	int* _freeSlabRecordIndices;
	int _returnedUnusedSlabRecordCounter; // This lets us re-use records at the start of _slabRecords before attempting to allocate more
	int _slabRecordMaxConcurrentUsageCount = 0;
	
	// Slab rental ledgers
	SizeClassIntArray _nonFullSlabRecordLinkHeadMap;
	SizeClassIntArray _emptySlabCountMap;
	SizeClassIntArray _largeSizeClassRentalCountMap;
	SizeClassIntArray _currentRetentionWindowLargeSizeClassRentalCountPeakMap;
	SizeClassIntArray _previousRetentionWindowLargeSizeClassRentalCountPeakMap;
	long _retainedEmptySlabBytesTotal;
	
	public long CurrentlyLeasedTotalBytes { get; private set; }

	public FixedByteSlabPool(long maxRetainedEmptySlabBytes, TimeSpan maxRetentionTime) {
		static int GetSlotSizeBytes(int sizeClassIndex) {
			if (!IsLargeSizeClass(sizeClassIndex)) return MinSlotSizeBytes << sizeClassIndex;
			var largeIndex = sizeClassIndex - NumSmallSizeClasses;
			var largeSizeClassRankStart = (long) MaxSmallSlotSizeBytes << (largeIndex / NumLargeSizeClassDivisionsPerPowerOfTwo);
			var result = largeSizeClassRankStart + largeSizeClassRankStart * (largeIndex % NumLargeSizeClassDivisionsPerPowerOfTwo + 1) / NumLargeSizeClassDivisionsPerPowerOfTwo;
			return (int) Int64.Min(result, Int32.MaxValue);
		}
		
		ArgumentOutOfRangeException.ThrowIfNegative(maxRetainedEmptySlabBytes);
		ArgumentOutOfRangeException.ThrowIfLessThan(maxRetentionTime, TimeSpan.Zero);
		_maxRetainedEmptySlabBytes = maxRetainedEmptySlabBytes;
		_retentionWindowDurationTicks = (long) Double.Clamp(maxRetentionTime.TotalSeconds * Stopwatch.Frequency / 2d, 1d, Int64.MaxValue / 4L);
		_currentRetentionWindowEndTimestamp = Stopwatch.GetTimestamp() + _retentionWindowDurationTicks;

		_slabRecordsLength = InitialSlabRecordCapacity;
		_slabRecords = (SlabRecord*) NativeMemory.Alloc((nuint) _slabRecordsLength, (nuint) sizeof(SlabRecord));
		_freeSlabRecordIndices = (int*) NativeMemory.Alloc((nuint) _slabRecordsLength, sizeof(int));
		
		for (var i = 0; i < NumSizeClasses; ++i) {
			_nonFullSlabRecordLinkHeadMap[i] = NullSlab;
			_slotSizePrecalculationTable[i] = GetSlotSizeBytes(i);
			_slotCountPrecalculationTable[i] = IsLargeSizeClass(i) ? 1 : (SmallSlabSizeBytes / GetSlotSizeBytes(i));
			_slabSizePrecalculationTable[i] = IsLargeSizeClass(i) ? GetSlotSizeBytes(i) : SmallSlabSizeBytes;
			_slabAlignmentPrecalculationTable[i] = _slabSizePrecalculationTable[i] >= HugePageSlabAlignmentBytes ? HugePageSlabAlignmentBytes : DefaultSlabAlignmentBytes;
		}

		lock (_liveInstancesLock) _liveInstances.Add(this);
	}

	public static void ReleaseAllPooledMemory() {
		lock (_liveInstancesLock) {
			foreach (var instance in _liveInstances) instance.ReleasePooledMemory();
		}
	}
	
	public static void NudgeLiveInstanceTimers() {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		lock (_liveInstancesLock) {
			foreach (var instance in _liveInstances) instance.RollRetentionWindowsIfElapsed(Stopwatch.GetTimestamp());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IsLargeSizeClass(int sizeClassIndex) => sizeClassIndex >= NumSmallSizeClasses;

	static int GetSizeClassIndex(int numBytes) {
		if (numBytes <= MinSlotSizeBytes) return 0;
		if (numBytes <= MaxSmallSlotSizeBytes) return BitOperations.Log2((uint) (numBytes - 1)) + 1 - SmallestAllocationLog2;
		
		var numBytesLog2 = BitOperations.Log2((uint) (numBytes - 1));
		var largeSizeClassRankStart = 1L << numBytesLog2;
		var divisionOffset = (int) (((numBytes - largeSizeClassRankStart) * NumLargeSizeClassDivisionsPerPowerOfTwo + largeSizeClassRankStart - 1) / largeSizeClassRankStart);
		return NumSmallSizeClasses + (numBytesLog2 - LargestSmallSizeClassAllocationLog2) * NumLargeSizeClassDivisionsPerPowerOfTwo + divisionOffset - 1;
	}

	public SlabRental Rent(int numBytes) {
		ThrowIfThisIsDisposed();
		ArgumentOutOfRangeException.ThrowIfNegative(numBytes);
		RollRetentionWindowsIfElapsed(Stopwatch.GetTimestamp());

		var sizeClassIndex = GetSizeClassIndex(numBytes);
		var slotSizeBytes = _slotSizePrecalculationTable[sizeClassIndex];
		var slabIndex = _nonFullSlabRecordLinkHeadMap[sizeClassIndex];
		if (slabIndex == NullSlab) {
			slabIndex = AllocateSlab(sizeClassIndex);
			LinkNonFullSlab(slabIndex);
		}

		var slabPtr = _slabRecords + slabIndex;
		if (slabPtr->UsedSlotCount == 0) {
			--_emptySlabCountMap[sizeClassIndex];
			_retainedEmptySlabBytesTotal -= _slabSizePrecalculationTable[sizeClassIndex];
		}

		byte* resultantSlotPtr;
		if (slabPtr->FreeListHead != null) {
			resultantSlotPtr = slabPtr->FreeListHead;
			slabPtr->FreeListHead = *(byte**) resultantSlotPtr;
		}
		else {
			resultantSlotPtr = slabPtr->AllocationPtr + (nint) slabPtr->LowestNeverRentedSlotIndex * slotSizeBytes;
			++slabPtr->LowestNeverRentedSlotIndex;
		}

		++slabPtr->UsedSlotCount;
		if (slabPtr->UsedSlotCount == _slotCountPrecalculationTable[sizeClassIndex]) UnlinkSlab(slabIndex);
		if (IsLargeSizeClass(sizeClassIndex)) {
			var rentedCount = ++_largeSizeClassRentalCountMap[sizeClassIndex];
			_currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex] = Int32.Max(_currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex], rentedCount);
		}
		CurrentlyLeasedTotalBytes += slotSizeBytes;
		return new((UIntPtr) resultantSlotPtr, numBytes, slabIndex);
	}

	public void Return(SlabRental rental) {
		ThrowIfThisIsDisposed();
		rental.ThrowIfInvalid();
		RollRetentionWindowsIfElapsed(Stopwatch.GetTimestamp());

		var slabIndex = rental.SlabIndex;
		if ((uint) slabIndex >= (uint) _slabRecordMaxConcurrentUsageCount || _slabRecords[slabIndex].AllocationPtr == null) {
			throw new ArgumentException($"Given rental refers to slab {slabIndex}, which is not allocated in this pool.", nameof(rental));
		}

		var slab = _slabRecords + slabIndex;
		var sizeClassIndex = slab->SizeClassIndex;
		var slotSizeBytes = _slotSizePrecalculationTable[sizeClassIndex];
		var slotPtr = (byte*) rental.SlotPtr;
		var slotOffset = slotPtr - slab->AllocationPtr;
		if (slotOffset < 0 || slotOffset >= _slabSizePrecalculationTable[sizeClassIndex] || slotOffset % slotSizeBytes != 0 || rental.SizeBytes > slotSizeBytes || slab->UsedSlotCount == 0) {
			throw new ArgumentException("Given rental does not match any slot currently rented from this pool.", nameof(rental));
		}

#if DEBUG
		for (var freeSlot = slab->FreeListHead; freeSlot != null; freeSlot = *(byte**) freeSlot) {
			if (freeSlot == slotPtr) throw new ArgumentException("Given rental has already been returned to this pool.", nameof(rental));
		}
#endif

		var slabUsedSlotCountPreReturn = slab->UsedSlotCount;
		var slabWasFull = slabUsedSlotCountPreReturn == _slotCountPrecalculationTable[sizeClassIndex];
		slab->UsedSlotCount = slabUsedSlotCountPreReturn - 1;
		*(byte**) slotPtr = slab->FreeListHead;
		slab->FreeListHead = slotPtr;
		CurrentlyLeasedTotalBytes -= slotSizeBytes;
		if (IsLargeSizeClass(sizeClassIndex)) --_largeSizeClassRentalCountMap[sizeClassIndex];
		if (slabWasFull) LinkNonFullSlab(slabIndex);

		if (slabUsedSlotCountPreReturn != 1) return;
		var slabSizeBytes = _slabSizePrecalculationTable[sizeClassIndex];
		if (ShouldRetainEmptySlab(sizeClassIndex, slabSizeBytes)) {
			++_emptySlabCountMap[sizeClassIndex];
			_retainedEmptySlabBytesTotal += slabSizeBytes;
			return;
		}

		UnlinkSlab(slabIndex);
		ReleaseSlab(slabIndex);
	}

	bool ShouldRetainEmptySlab(int sizeClassIndex, long slabSizeBytes) {
		if (!IsLargeSizeClass(sizeClassIndex)) {
			return _emptySlabCountMap[sizeClassIndex] < RetainedEmptySmallSlabsPerSizeClass
				&& _retainedEmptySlabBytesTotal + slabSizeBytes <= _maxRetainedEmptySlabBytes;
		}
		if (_emptySlabCountMap[sizeClassIndex] >= GetRetainedLargeSlotLimit(sizeClassIndex)) return false;
		if (_retainedEmptySlabBytesTotal + slabSizeBytes <= _maxRetainedEmptySlabBytes) return true;

		for (var otherSizeClassIndex = NumSizeClasses - 1; otherSizeClassIndex >= NumSmallSizeClasses; --otherSizeClassIndex) {
			if (otherSizeClassIndex == sizeClassIndex) continue;
			while (_emptySlabCountMap[otherSizeClassIndex] > _currentRetentionWindowLargeSizeClassRentalCountPeakMap[otherSizeClassIndex] && TryReleaseOneEmptySlab(otherSizeClassIndex)) {
				if (_retainedEmptySlabBytesTotal + slabSizeBytes <= _maxRetainedEmptySlabBytes) return true;
			}
		}
		return false;
	}

	bool TryReleaseOneEmptySlab(int sizeClassIndex) {
		for (var slabIndex = _nonFullSlabRecordLinkHeadMap[sizeClassIndex]; slabIndex != NullSlab; slabIndex = _slabRecords[slabIndex].NextNonFullSlabIndex) {
			if (_slabRecords[slabIndex].UsedSlotCount != 0) continue;
			--_emptySlabCountMap[sizeClassIndex];
			_retainedEmptySlabBytesTotal -= _slabSizePrecalculationTable[sizeClassIndex];
			UnlinkSlab(slabIndex);
			ReleaseSlab(slabIndex);
			return true;
		}
		return false;
	}

	int GetRetainedLargeSlotLimit(int sizeClassIndex) => Int32.Max(_previousRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex], _currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex]);

	internal void RollRetentionWindowsIfElapsed(long nowTimestamp) {
		if (nowTimestamp < _currentRetentionWindowEndTimestamp) return;
		RollRetentionWindow();
		if (nowTimestamp >= _currentRetentionWindowEndTimestamp + _retentionWindowDurationTicks) RollRetentionWindow();
		_currentRetentionWindowEndTimestamp = nowTimestamp + _retentionWindowDurationTicks;
	}

	void RollRetentionWindow() {
		for (var sizeClassIndex = NumSmallSizeClasses; sizeClassIndex < NumSizeClasses; ++sizeClassIndex) {
			_previousRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex] = _currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex];
			_currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex] = _largeSizeClassRentalCountMap[sizeClassIndex];

			while (_emptySlabCountMap[sizeClassIndex] > GetRetainedLargeSlotLimit(sizeClassIndex) && TryReleaseOneEmptySlab(sizeClassIndex)) { }
		}
	}

	int AllocateSlab(int sizeClassIndex) {
		// Resize the slab record buffer if we have to
		if (_returnedUnusedSlabRecordCounter == 0 && _slabRecordMaxConcurrentUsageCount == _slabRecordsLength) {
			var newCapacity = _slabRecordsLength * 2;
			_slabRecords = (SlabRecord*) NativeMemory.Realloc(_slabRecords, (nuint) (newCapacity * sizeof(SlabRecord)));
			_freeSlabRecordIndices = (int*) NativeMemory.Realloc(_freeSlabRecordIndices, (nuint) (newCapacity * sizeof(int)));
			_slabRecordsLength = newCapacity;
		}

		var allocationPtr = (byte*) NativeMemory.AlignedAlloc((nuint) _slabSizePrecalculationTable[sizeClassIndex], (nuint) _slabAlignmentPrecalculationTable[sizeClassIndex]);
		var slabIndex = _returnedUnusedSlabRecordCounter > 0
			? _freeSlabRecordIndices[--_returnedUnusedSlabRecordCounter]
			: _slabRecordMaxConcurrentUsageCount++;

		_slabRecords[slabIndex] = new SlabRecord {
			AllocationPtr = allocationPtr,
			SizeClassIndex = sizeClassIndex,
			NextNonFullSlabIndex = NullSlab,
			PrevNonFullSlabIndex = NullSlab
		};
		++_emptySlabCountMap[sizeClassIndex];
		_retainedEmptySlabBytesTotal += _slabSizePrecalculationTable[sizeClassIndex];
		return slabIndex;
	}

	void ReleasePooledMemory() {
		if (_isDisposed) return;
		for (var sizeClassIndex = 0; sizeClassIndex < NumSizeClasses; ++sizeClassIndex) {
			while (TryReleaseOneEmptySlab(sizeClassIndex)) { }
			if (!IsLargeSizeClass(sizeClassIndex)) continue;
			_previousRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex] = _largeSizeClassRentalCountMap[sizeClassIndex];
			_currentRetentionWindowLargeSizeClassRentalCountPeakMap[sizeClassIndex] = _largeSizeClassRentalCountMap[sizeClassIndex];
		}
		_currentRetentionWindowEndTimestamp = Stopwatch.GetTimestamp() + _retentionWindowDurationTicks;
	}

	void ReleaseSlab(int slabIndex) {
		NativeMemory.AlignedFree(_slabRecords[slabIndex].AllocationPtr);
		_slabRecords[slabIndex] = default;
		_freeSlabRecordIndices[_returnedUnusedSlabRecordCounter++] = slabIndex;
	}

	void LinkNonFullSlab(int slabIndex) {
		var slabPtr = _slabRecords + slabIndex;
		var headIndex = _nonFullSlabRecordLinkHeadMap[slabPtr->SizeClassIndex];
		slabPtr->NextNonFullSlabIndex = headIndex;
		slabPtr->PrevNonFullSlabIndex = NullSlab;
		if (headIndex != NullSlab) _slabRecords[headIndex].PrevNonFullSlabIndex = slabIndex;
		_nonFullSlabRecordLinkHeadMap[slabPtr->SizeClassIndex] = slabIndex;
	}

	void UnlinkSlab(int slabIndex) {
		var slab = _slabRecords + slabIndex;
		if (slab->PrevNonFullSlabIndex != NullSlab) _slabRecords[slab->PrevNonFullSlabIndex].NextNonFullSlabIndex = slab->NextNonFullSlabIndex;
		else _nonFullSlabRecordLinkHeadMap[slab->SizeClassIndex] = slab->NextNonFullSlabIndex;
		if (slab->NextNonFullSlabIndex != NullSlab) _slabRecords[slab->NextNonFullSlabIndex].PrevNonFullSlabIndex = slab->PrevNonFullSlabIndex;
		slab->NextNonFullSlabIndex = NullSlab;
		slab->PrevNonFullSlabIndex = NullSlab;
	}

	public void Dispose() {
		if (_isDisposed) return;
		lock (_liveInstancesLock) _liveInstances.Remove(this);
		try {
			for (var i = 0; i < _slabRecordMaxConcurrentUsageCount; ++i) {
				if (_slabRecords[i].AllocationPtr != null) NativeMemory.AlignedFree(_slabRecords[i].AllocationPtr);
			}
			NativeMemory.Free(_slabRecords);
			NativeMemory.Free(_freeSlabRecordIndices);
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(FixedByteSlabPool));
	}
}
