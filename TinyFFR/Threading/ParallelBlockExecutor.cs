// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.ExceptionServices;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

static unsafe class ParallelBlockExecutor {
	public const int SlotCount = 256;
	public const int BlocksPerParticipant = 4;
	public const int DefaultMinimumElementsPerBlock = 4096;
	public const int UserCallbackMinimumElementsPerBlock = 1024;
	const int MaxHelpersPerExecution = 128;
	const int InitialHelpersPerExecution = 4;
	const int HelpersSpawnedPerHelper = 2;
	static readonly UnmanagedBuffer<ParallelBlockState> _states = CreateStates();
	static readonly Exception?[] _slotExceptions = new Exception?[SlotCount];
	static readonly IJobExecutionFacade?[] _slotFacades = new IJobExecutionFacade?[SlotCount];

	struct ParallelBlockState {
		public void* BlockFunc;
		public int ItemCount;
		public int ItemsPerBlock;
		public int BlockCount;
		public int NextBlock;
		public int CompletedBlocks;
		public int RemainingParticipants;
		public int HelpersRemainingToSpawn;
		public int Failed;
		public int InUse;
	}

	static class ContextSlots<TContext> {
		public static readonly TContext[] Values = new TContext[SlotCount];
	}

	readonly struct RowBandContext<TContext> {
		public readonly delegate* managed<XYPair<int>, XYPair<int>, TContext, void> BlockFunc;
		public readonly int Width;
		public readonly TContext Context;

		public RowBandContext(delegate* managed<XYPair<int>, XYPair<int>, TContext, void> blockFunc, int width, TContext context) {
			BlockFunc = blockFunc;
			Width = width;
			Context = context;
		}
	}

	readonly struct SpanBlockContext<T, TContext> where T : unmanaged {
		public readonly void* Callback;
		public readonly T* Buffer;
		public readonly TContext Context;

		public SpanBlockContext(void* callback, T* buffer, TContext context) {
			Callback = callback;
			Buffer = buffer;
			Context = context;
		}
	}

	public static void ExecuteOverSpan<T, TContext>(IJobExecutionFacade facade, Span<T> span, int minimumElementsPerBlock, delegate* managed<Span<T>, int, TContext, void> blockFunc, TContext context) where T : unmanaged {
		static void InvokeBlock(int start, int count, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<Span<T>, int, TContext, void>) blockContext.Callback)(new Span<T>(blockContext.Buffer + start, count), start, blockContext.Context);
		}

		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		fixed (T* bufferPtr = span) {
			Execute(facade, span.Length, minimumElementsPerBlock, &InvokeBlock, new SpanBlockContext<T, TContext>(blockFunc, bufferPtr, context));
		}
	}

	public static void ExecuteOverReadOnlySpan<T, TContext>(IJobExecutionFacade facade, ReadOnlySpan<T> span, int minimumElementsPerBlock, delegate* managed<ReadOnlySpan<T>, int, TContext, void> blockFunc, TContext context) where T : unmanaged {
		static void InvokeBlock(int start, int count, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<ReadOnlySpan<T>, int, TContext, void>) blockContext.Callback)(new ReadOnlySpan<T>(blockContext.Buffer + start, count), start, blockContext.Context);
		}

		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		fixed (T* bufferPtr = span) {
			Execute(facade, span.Length, minimumElementsPerBlock, &InvokeBlock, new SpanBlockContext<T, TContext>(blockFunc, bufferPtr, context));
		}
	}

	public static void ExecuteRowsOverSpan<T, TContext>(IJobExecutionFacade facade, Span<T> span, XYPair<int> dimensions, int minimumRowsPerBand, delegate* managed<Span<T>, XYPair<int>, XYPair<int>, TContext, void> blockFunc, TContext context) where T : unmanaged {
		static void InvokeBlock(XYPair<int> offset, XYPair<int> bandDimensions, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<Span<T>, XYPair<int>, XYPair<int>, TContext, void>) blockContext.Callback)(new Span<T>(blockContext.Buffer + (nint) offset.Y * bandDimensions.X, bandDimensions.Area), offset, bandDimensions, blockContext.Context);
		}

		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		if ((long) dimensions.X * dimensions.Y > span.Length) throw new ArgumentException("Span is smaller than the given dimensions.", nameof(span));
		fixed (T* bufferPtr = span) {
			ExecuteRows(facade, dimensions, minimumRowsPerBand, &InvokeBlock, new SpanBlockContext<T, TContext>(blockFunc, bufferPtr, context));
		}
	}

	public static void ExecuteRowsOverReadOnlySpan<T, TContext>(IJobExecutionFacade facade, ReadOnlySpan<T> span, XYPair<int> dimensions, int minimumRowsPerBand, delegate* managed<ReadOnlySpan<T>, XYPair<int>, XYPair<int>, TContext, void> blockFunc, TContext context) where T : unmanaged {
		static void InvokeBlock(XYPair<int> offset, XYPair<int> bandDimensions, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<ReadOnlySpan<T>, XYPair<int>, XYPair<int>, TContext, void>) blockContext.Callback)(new ReadOnlySpan<T>(blockContext.Buffer + (nint) offset.Y * bandDimensions.X, bandDimensions.Area), offset, bandDimensions, blockContext.Context);
		}

		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		if ((long) dimensions.X * dimensions.Y > span.Length) throw new ArgumentException("Span is smaller than the given dimensions.", nameof(span));
		fixed (T* bufferPtr = span) {
			ExecuteRows(facade, dimensions, minimumRowsPerBand, &InvokeBlock, new SpanBlockContext<T, TContext>(blockFunc, bufferPtr, context));
		}
	}

	static UnmanagedBuffer<ParallelBlockState> CreateStates() {
		var result = new UnmanagedBuffer<ParallelBlockState>(SlotCount);
		result.AsSpan.Clear();
		return result;
	}

	public static int GetMinimumRowsPerBand(int rowWidth, int minimumElementsPerBlock = DefaultMinimumElementsPerBlock) {
		return Int32.Max(1, minimumElementsPerBlock / Int32.Max(1, rowWidth));
	}

	public static void ExecuteRows<TContext>(IJobExecutionFacade facade, XYPair<int> dimensions, int minimumRowsPerBand, delegate* managed<XYPair<int>, XYPair<int>, TContext, void> blockFunc, TContext context) {
		static void InvokeRowBand(int firstRow, int rowCount, RowBandContext<TContext> rowContext) {
			rowContext.BlockFunc(new XYPair<int>(0, firstRow), new XYPair<int>(rowContext.Width, rowCount), rowContext.Context);
		}

		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		ArgumentOutOfRangeException.ThrowIfNegative(dimensions.X, nameof(dimensions));
		ArgumentOutOfRangeException.ThrowIfNegative(dimensions.Y, nameof(dimensions));
		if (dimensions.X == 0 || dimensions.Y == 0) return;
		Execute(facade, dimensions.Y, minimumRowsPerBand, &InvokeRowBand, new RowBandContext<TContext>(blockFunc, dimensions.X, context));
	}

	public static void Execute<TContext>(IJobExecutionFacade facade, int itemCount, int minimumItemsPerBlock, delegate* managed<int, int, TContext, void> blockFunc, TContext context) {
		ArgumentNullException.ThrowIfNull(facade);
		if (blockFunc == null) throw new ArgumentNullException(nameof(blockFunc));
		ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumItemsPerBlock);
		if (itemCount == 0) return;

		if (!facade.HasWorkerThreads || facade.WorkerThreadCount <= 0 || itemCount < minimumItemsPerBlock * 2L) {
			blockFunc(0, itemCount, context);
			return;
		}

		var maxParticipantCount = facade.WorkerThreadCount + 1;
		var blockCount = Int32.Min(itemCount / minimumItemsPerBlock, maxParticipantCount * BlocksPerParticipant);
		var itemsPerBlock = (itemCount + blockCount - 1) / blockCount;
		blockCount = (itemCount + itemsPerBlock - 1) / itemsPerBlock;
		if (blockCount < 2) {
			blockFunc(0, itemCount, context);
			return;
		}

		var slot = ClaimSlot();
		if (slot < 0) {
			blockFunc(0, itemCount, context);
			return;
		}

		var state = _states.BufferPointer + slot;
		var helperCount = Int32.Min(Int32.Min(blockCount - 1, facade.WorkerThreadCount), MaxHelpersPerExecution);
		var initialHelperCount = Int32.Min(helperCount, InitialHelpersPerExecution);
		ContextSlots<TContext>.Values[slot] = context;
		_slotFacades[slot] = facade;
		*state = new ParallelBlockState {
			BlockFunc = blockFunc,
			ItemCount = itemCount,
			ItemsPerBlock = itemsPerBlock,
			BlockCount = blockCount,
			RemainingParticipants = 1 + initialHelperCount,
			HelpersRemainingToSpawn = helperCount - initialHelperCount,
			InUse = 1
		};

		Span<ThreadJob> helperJobs = stackalloc ThreadJob[initialHelperCount];
		for (var i = 0; i < initialHelperCount; ++i) {
			helperJobs[i] = ThreadJob.CreateWithUnmanagedContextUnmanagedResult<nint, byte>(slot, &RunAsHelper<TContext>, &ReleaseHelperParticipation<TContext>);
		}
		try {
			facade.AddWorkerThreadJobs(helperJobs);
		}
#pragma warning disable CA1031
		catch (Exception) { }
#pragma warning restore CA1031

		try {
			RunClaimedBlocks<TContext>(slot);
		}
		finally {
			var spinWait = new SpinWait();
			while (Volatile.Read(ref state->CompletedBlocks) < blockCount) spinWait.SpinOnce(sleep1Threshold: -1);
		}

		var exception = Interlocked.Exchange(ref _slotExceptions[slot], null);
		ReleaseParticipation<TContext>(slot);
		if (exception != null) ExceptionDispatchInfo.Capture(exception).Throw();
	}

	static int ClaimSlot() {
		var states = _states.BufferPointer;
		for (var i = 0; i < SlotCount; ++i) {
			if (Interlocked.CompareExchange(ref states[i].InUse, 1, 0) == 0) return i;
		}
		return -1;
	}

	static void RunClaimedBlocks<TContext>(int slot) {
		var state = _states.BufferPointer + slot;
		var blockFunc = (delegate* managed<int, int, TContext, void>) state->BlockFunc;
		var context = ContextSlots<TContext>.Values[slot];
		while (true) {
			var block = Interlocked.Increment(ref state->NextBlock) - 1;
			if (block >= state->BlockCount) return;

			try {
				if (Volatile.Read(ref state->Failed) != 0) continue;
				var start = block * state->ItemsPerBlock;
				blockFunc(start, Int32.Min(state->ItemsPerBlock, state->ItemCount - start), context);
			}
#pragma warning disable CA1031
			catch (Exception e) {
#pragma warning restore CA1031
				Volatile.Write(ref state->Failed, 1);
				Interlocked.CompareExchange(ref _slotExceptions[slot], e, null);
			}
			finally {
				Interlocked.Increment(ref state->CompletedBlocks);
			}
		}
	}

	static byte RunAsHelper<TContext>(nint slot) {
		SpawnFurtherHelpers<TContext>((int) slot);
		RunClaimedBlocks<TContext>((int) slot);
		return 0;
	}

	static void SpawnFurtherHelpers<TContext>(int slot) {
		var state = _states.BufferPointer + slot;
		Span<ThreadJob> helperJobs = stackalloc ThreadJob[HelpersSpawnedPerHelper];
		var spawnCount = 0;
		while (spawnCount < HelpersSpawnedPerHelper && Volatile.Read(ref state->NextBlock) < state->BlockCount - 1) {
			var remaining = Volatile.Read(ref state->HelpersRemainingToSpawn);
			if (remaining <= 0) break;
			if (Interlocked.CompareExchange(ref state->HelpersRemainingToSpawn, remaining - 1, remaining) != remaining) continue;
			Interlocked.Increment(ref state->RemainingParticipants);
			helperJobs[spawnCount++] = ThreadJob.CreateWithUnmanagedContextUnmanagedResult<nint, byte>(slot, &RunAsHelper<TContext>, &ReleaseHelperParticipation<TContext>);
		}
		if (spawnCount == 0) return;

		try {
			_slotFacades[slot]!.AddWorkerThreadJobs(helperJobs[..spawnCount]);
		}
#pragma warning disable CA1031
		catch (Exception) { }
#pragma warning restore CA1031
	}

	static void ReleaseHelperParticipation<TContext>(nint slot, Exception? _, byte __) => ReleaseParticipation<TContext>((int) slot);

	static void ReleaseParticipation<TContext>(int slot) {
		var state = _states.BufferPointer + slot;
		if (Interlocked.Decrement(ref state->RemainingParticipants) != 0) return;
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TContext>()) ContextSlots<TContext>.Values[slot] = default!;
		_slotFacades[slot] = null;
		Volatile.Write(ref state->InUse, 0);
	}
}
