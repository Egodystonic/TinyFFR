// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.ExceptionServices;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

/// <summary>
/// Static class that provides functions to help split CPU work in to blocks and run them in parallel on TinyFFR's worker threads.
/// </summary>
/// <remarks>
/// <para>
/// Every method takes an <see cref="IJobExecutionFacade"/> that determines where the work runs. Pass
/// <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to use TinyFFR's worker thread pool, or <see cref="IJobExecutionFacade.Synchronous"/>
/// to run everything on the calling thread. The calling thread always processes blocks too, and every method only returns once all
/// blocks have finished; so any results written by the blocks can be used straight away.
/// </para>
/// <para>
/// Block callbacks are static function pointers, and any state they need is passed in via a <c>context</c> argument rather than a closure;
/// so (once warmed up) none of these methods allocate any managed memory.
/// </para>
/// <para>
/// Block callbacks run on worker threads, so they must not call any TinyFFR API; they should only work on the data they are given (and their
/// <c>context</c>). Each block must only write to its own part of the data and should generally be written with parallel access patterns in mind for thread safety.
/// </para>
/// <para>
/// Small amounts of work (fewer than two blocks' worth of items) and facades without any worker threads simply run the whole range on the calling
/// thread, as a single block. If any block throws an exception, blocks that haven't started yet are skipped, and the first exception is rethrown on
/// the calling thread once every running block has finished.
/// </para>
/// </remarks>
public static unsafe class ParallelizationProvider {
	struct ParallelBlockState {
		public void* BlockFunc;
		public int ItemCount;
		public int ItemsPerBlock;
		public int BlockCount;
		public int NextBlock;
		public int CompletedBlocks;
		public int RemainingParticipants;
		public int HelpersRemainingToSpawn;
		public int InUse;
	}
	
	internal const int SlotCount = 256;
	internal const int BlocksPerParticipant = 4;
	internal const int DefaultMinimumElementsPerBlock = 4096;
	internal const int UserCallbackMinimumElementsPerBlock = 1024;
	const int MaxHelpersPerExecution = 128;
	const int InitialHelpersPerExecution = 4;
	const int HelpersSpawnedPerHelper = 2;
	static readonly UnmanagedBuffer<ParallelBlockState> _blockStateBuffer = CreateStateBuffer();
	static readonly Exception?[] _slotExceptions = new Exception?[SlotCount];
	static readonly IJobExecutionFacade?[] _slotFacades = new IJobExecutionFacade?[SlotCount];
	
	static UnmanagedBuffer<ParallelBlockState> CreateStateBuffer() {
		var result = new UnmanagedBuffer<ParallelBlockState>(SlotCount);
		result.AsSpan.Clear();
		return result;
	}

	static class ContextSlotContainer<TContext> {
		public static readonly TContext[] Slots = new TContext[SlotCount];
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
	
	/// <summary>
	/// Returns how many rows of the given width are needed to make up at least <paramref name="minimumElementsPerBlock"/> elements (and never fewer than one).
	/// </summary>
	/// <remarks>
	/// Useful for picking a <c>minimumRowsPerBlock</c> value for the row-based methods, so that each band has enough work to be worth handing to another thread.
	/// </remarks>
	/// <param name="rowWidth">The number of elements in each row.</param>
	/// <param name="minimumElementsPerBlock">The minimum number of elements each band should contain.</param>
	public static int GetMinimumRowsPerBand(int rowWidth, int minimumElementsPerBlock = DefaultMinimumElementsPerBlock) {
		return Int32.Max(1, minimumElementsPerBlock / Int32.Max(1, rowWidth));
	}

	/// <summary>
	/// Splits <paramref name="span"/> in to contiguous blocks and invokes <paramref name="blockCallback"/> for each, in parallel.
	/// </summary>
	/// <remarks>
	/// <paramref name="blockCallback"/> is passed the block (a slice of <paramref name="span"/>), the index of the block's first element within
	/// <paramref name="span"/>, and <paramref name="context"/>. See <see cref="ParallelizationProvider"/> for the rules block callbacks must follow.
	/// </remarks>
	/// <typeparam name="T">The type of the span's elements.</typeparam>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="span">The data to process.</param>
	/// <param name="blockCallback">The function to invoke for each block.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumElementsPerBlock">The fewest elements a block may contain. Raise this when the work per element is small, so that each block is
	/// worth handing to another thread.</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	public static void ExecuteOverSpan<T, TContext>(IJobExecutionFacade facade, Span<T> span, delegate* managed<Span<T>, int, TContext, void> blockCallback, TContext context, int minimumElementsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) where T : unmanaged {
		static void InvokeBlock(int start, int count, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<Span<T>, int, TContext, void>) blockContext.Callback)(new Span<T>(blockContext.Buffer + start, count), start, blockContext.Context);
		}

		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		fixed (T* bufferPtr = span) {
			Execute(facade, span.Length, &InvokeBlock, new SpanBlockContext<T, TContext>(blockCallback, bufferPtr, context), minimumElementsPerBlock, maxWorkerThreads);
		}
	}

	/// <summary>
	/// Splits <paramref name="span"/> in to contiguous blocks and invokes <paramref name="blockCallback"/> for each, in parallel, for reading only.
	/// </summary>
	/// <remarks>
	/// <paramref name="blockCallback"/> is passed the block (a slice of <paramref name="span"/>), the index of the block's first element within
	/// <paramref name="span"/>, and <paramref name="context"/>. See <see cref="ParallelizationProvider"/> for the rules block callbacks must follow.
	/// </remarks>
	/// <typeparam name="T">The type of the span's elements.</typeparam>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="span">The data to process.</param>
	/// <param name="blockCallback">The function to invoke for each block.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumElementsPerBlock">The fewest elements a block may contain. Raise this when the work per element is small, so that each block is
	/// worth handing to another thread.</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	public static void ExecuteOverReadOnlySpan<T, TContext>(IJobExecutionFacade facade, ReadOnlySpan<T> span, delegate* managed<ReadOnlySpan<T>, int, TContext, void> blockCallback, TContext context, int minimumElementsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) where T : unmanaged {
		static void InvokeBlock(int start, int count, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<ReadOnlySpan<T>, int, TContext, void>) blockContext.Callback)(new ReadOnlySpan<T>(blockContext.Buffer + start, count), start, blockContext.Context);
		}

		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		fixed (T* bufferPtr = span) {
			Execute(facade, span.Length, &InvokeBlock, new SpanBlockContext<T, TContext>(blockCallback, bufferPtr, context), minimumElementsPerBlock, maxWorkerThreads);
		}
	}

	/// <summary>
	/// Splits a two-dimensional <paramref name="span"/> in to bands of whole rows and invokes <paramref name="blockCallback"/> for each, in parallel.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="span"/> is treated as <paramref name="dimensions"/>.Y rows of <paramref name="dimensions"/>.X elements each, laid out row by row.
	/// It is split in to bands of whole rows; <paramref name="blockCallback"/> is passed each band (a slice of <paramref name="span"/>), the band's offset
	/// (its X is always <c>0</c>, and its Y is the index of the band's first row), the band's dimensions (its X is always the full row width), and
	/// <paramref name="context"/>.
	/// </para>
	/// <para>
	/// The element at <c>(x, y)</c> within the band is at <c>band[y * bandDimensions.X + x]</c>. See <see cref="ParallelizationProvider"/> for the rules block
	/// callbacks must follow.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the span's elements.</typeparam>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="span">The data to process. Must contain at least <c>dimensions.X * dimensions.Y</c> elements.</param>
	/// <param name="dimensions">The width (row length) and height (row count) of the data.</param>
	/// <param name="blockCallback">The function to invoke for each band.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumRowsPerBlock">The fewest rows a band may contain (see <see cref="GetMinimumRowsPerBand"/>).</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="span"/> contains fewer than <c>dimensions.X * dimensions.Y</c> elements.</exception>
	public static void ExecuteRowsOverSpan<T, TContext>(IJobExecutionFacade facade, Span<T> span, XYPair<int> dimensions, delegate* managed<Span<T>, XYPair<int>, XYPair<int>, TContext, void> blockCallback, TContext context, int minimumRowsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) where T : unmanaged {
		static void InvokeBlock(XYPair<int> offset, XYPair<int> bandDimensions, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<Span<T>, XYPair<int>, XYPair<int>, TContext, void>) blockContext.Callback)(new Span<T>(blockContext.Buffer + (nint) offset.Y * bandDimensions.X, bandDimensions.Area), offset, bandDimensions, blockContext.Context);
		}

		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		if ((long) dimensions.X * dimensions.Y > span.Length) throw new ArgumentException("Span is smaller than the given dimensions.", nameof(span));
		fixed (T* bufferPtr = span) {
			ExecuteRows(facade, dimensions, &InvokeBlock, new SpanBlockContext<T, TContext>(blockCallback, bufferPtr, context), minimumRowsPerBlock, maxWorkerThreads);
		}
	}

	/// <summary>
	/// Splits a two-dimensional <paramref name="span"/> in to bands of whole rows and invokes <paramref name="blockCallback"/> for each, in parallel, for reading only.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="span"/> is treated as <paramref name="dimensions"/>.Y rows of <paramref name="dimensions"/>.X elements each, laid out row by row.
	/// It is split in to bands of whole rows; <paramref name="blockCallback"/> is passed each band (a slice of <paramref name="span"/>), the band's offset
	/// (its X is always <c>0</c>, and its Y is the index of the band's first row), the band's dimensions (its X is always the full row width), and
	/// <paramref name="context"/>.
	/// </para>
	/// <para>
	/// The element at <c>(x, y)</c> within the band is at <c>band[y * bandDimensions.X + x]</c>. See <see cref="ParallelizationProvider"/> for the rules block
	/// callbacks must follow.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the span's elements.</typeparam>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="span">The data to process. Must contain at least <c>dimensions.X * dimensions.Y</c> elements.</param>
	/// <param name="dimensions">The width (row length) and height (row count) of the data.</param>
	/// <param name="blockCallback">The function to invoke for each band.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumRowsPerBlock">The fewest rows a band may contain (see <see cref="GetMinimumRowsPerBand"/>).</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="span"/> contains fewer than <c>dimensions.X * dimensions.Y</c> elements.</exception>
	public static void ExecuteRowsOverReadOnlySpan<T, TContext>(IJobExecutionFacade facade, ReadOnlySpan<T> span, XYPair<int> dimensions, delegate* managed<ReadOnlySpan<T>, XYPair<int>, XYPair<int>, TContext, void> blockCallback, TContext context, int minimumRowsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) where T : unmanaged {
		static void InvokeBlock(XYPair<int> offset, XYPair<int> bandDimensions, SpanBlockContext<T, TContext> blockContext) {
			((delegate* managed<ReadOnlySpan<T>, XYPair<int>, XYPair<int>, TContext, void>) blockContext.Callback)(new ReadOnlySpan<T>(blockContext.Buffer + (nint) offset.Y * bandDimensions.X, bandDimensions.Area), offset, bandDimensions, blockContext.Context);
		}

		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		if ((long) dimensions.X * dimensions.Y > span.Length) throw new ArgumentException("Span is smaller than the given dimensions.", nameof(span));
		fixed (T* bufferPtr = span) {
			ExecuteRows(facade, dimensions, &InvokeBlock, new SpanBlockContext<T, TContext>(blockCallback, bufferPtr, context), minimumRowsPerBlock, maxWorkerThreads);
		}
	}

	/// <summary>
	/// Splits a two-dimensional area in to bands of whole rows and invokes <paramref name="blockCallback"/> for each, in parallel.
	/// </summary>
	/// <remarks>
	/// <paramref name="blockCallback"/> is passed each band's offset (its X is always <c>0</c>, and its Y is the index of the band's first row), the band's
	/// dimensions (its X is always the full width), and <paramref name="context"/>. No data is passed in; use this when the work reads or writes data that
	/// <paramref name="context"/> refers to. See <see cref="ParallelizationProvider"/> for the rules block callbacks must follow.
	/// </remarks>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="dimensions">The width and height (row count) of the area to process. Nothing is invoked if either is <c>0</c>.</param>
	/// <param name="blockCallback">The function to invoke for each band.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumRowsPerBlock">The fewest rows a band may contain (see <see cref="GetMinimumRowsPerBand"/>).</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	public static void ExecuteRows<TContext>(IJobExecutionFacade facade, XYPair<int> dimensions, delegate* managed<XYPair<int>, XYPair<int>, TContext, void> blockCallback, TContext context, int minimumRowsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) {
		static void InvokeRowBand(int firstRow, int rowCount, RowBandContext<TContext> rowContext) {
			rowContext.BlockFunc(new XYPair<int>(0, firstRow), new XYPair<int>(rowContext.Width, rowCount), rowContext.Context);
		}

		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		ArgumentOutOfRangeException.ThrowIfNegative(dimensions.X, nameof(dimensions));
		ArgumentOutOfRangeException.ThrowIfNegative(dimensions.Y, nameof(dimensions));
		if (dimensions.X == 0 || dimensions.Y == 0) return;
		Execute(facade, dimensions.Y, &InvokeRowBand, new RowBandContext<TContext>(blockCallback, dimensions.X, context), minimumRowsPerBlock, maxWorkerThreads);
	}

	/// <summary>
	/// Splits the range <c>0</c> to <paramref name="itemCount"/> in to contiguous blocks and invokes <paramref name="blockCallback"/> for each, in parallel.
	/// </summary>
	/// <remarks>
	/// <paramref name="blockCallback"/> is passed the index of the block's first item, the number of items in the block, and <paramref name="context"/>.
	/// The items themselves are whatever the callback decides they are (e.g. elements of an array referred to by <paramref name="context"/>).
	/// See <see cref="ParallelizationProvider"/> for the rules block callbacks must follow.
	/// </remarks>
	/// <typeparam name="TContext">The type of <paramref name="context"/>.</typeparam>
	/// <param name="facade">Where to run the work. Pass <see cref="Resources.IResourceAllocator.ParallelizingFacade"/> to spread the blocks across
	/// TinyFFR's worker threads (the calling thread works on them too), or <see cref="IJobExecutionFacade.Synchronous"/> to run everything on the calling thread.</param>
	/// <param name="itemCount">The number of items to process. Nothing is invoked if this is <c>0</c>.</param>
	/// <param name="blockCallback">The function to invoke for each block.</param>
	/// <param name="context">An arbitrary value passed unchanged to every invocation of <paramref name="blockCallback"/>; use it to pass in whatever state the
	/// work needs without capturing variables in a closure. It is shared between all blocks (and therefore between threads).</param>
	/// <param name="minimumItemsPerBlock">The fewest items a block may contain. Raise this when the work per item is small, so that each block is
	/// worth handing to another thread.</param>
	/// <param name="maxWorkerThreads">The most worker threads that may help with this call (in addition to the calling thread). Lowering this can be
	/// faster for memory-bound work, where more threads only compete for memory bandwidth. <c>0</c> runs everything on the calling thread.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="facade"/> or <paramref name="blockCallback"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a count or dimension is negative, a minimum block size is not positive, or
	/// <paramref name="maxWorkerThreads"/> is negative.</exception>
	public static void Execute<TContext>(IJobExecutionFacade facade, int itemCount, delegate* managed<int, int, TContext, void> blockCallback, TContext context, int minimumItemsPerBlock = 1, int maxWorkerThreads = Int32.MaxValue) {
		ArgumentNullException.ThrowIfNull(facade);
		if (blockCallback == null) throw new ArgumentNullException(nameof(blockCallback));
		ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumItemsPerBlock);
		ArgumentOutOfRangeException.ThrowIfNegative(maxWorkerThreads);
		if (itemCount == 0) return;

		var workerThreadCount = facade.HasWorkerThreads ? Int32.Min(facade.WorkerThreadCount, maxWorkerThreads) : 0;
		if (workerThreadCount <= 0 || itemCount < minimumItemsPerBlock * 2L) {
			blockCallback(0, itemCount, context);
			return;
		}

		var maxParticipantCount = workerThreadCount + 1;
		var blockCount = Int32.Min(itemCount / minimumItemsPerBlock, maxParticipantCount * BlocksPerParticipant);
		var itemsPerBlock = (itemCount + blockCount - 1) / blockCount;
		blockCount = (itemCount + itemsPerBlock - 1) / itemsPerBlock;
		if (blockCount < 2) {
			blockCallback(0, itemCount, context);
			return;
		}

		static int? ClaimBlockStateSlot() {
			var states = _blockStateBuffer.BufferPointer;
			for (var i = 0; i < SlotCount; ++i) {
				if (Interlocked.CompareExchange(ref states[i].InUse, 1, 0) == 0) return i;
			}
			return null;
		}
		var slotClaim = ClaimBlockStateSlot();
		if (slotClaim is not { } slot) {
			blockCallback(0, itemCount, context);
			return;
		}

		var state = _blockStateBuffer.BufferPointer + slot;
		var helperCount = Int32.Min(Int32.Min(blockCount - 1, workerThreadCount), MaxHelpersPerExecution);
		var initialHelperCount = Int32.Min(helperCount, InitialHelpersPerExecution);
		ContextSlotContainer<TContext>.Slots[slot] = context;
		_slotFacades[slot] = facade;
		*state = new ParallelBlockState {
			BlockFunc = blockCallback,
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
		catch (Exception) {
			// No-op because we report exceptions via the helper participation
		}
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

	static void RunClaimedBlocks<TContext>(int slot) {
		var state = _blockStateBuffer.BufferPointer + slot;
		var blockFunc = (delegate* managed<int, int, TContext, void>) state->BlockFunc;
		var context = ContextSlotContainer<TContext>.Slots[slot];
		while (true) {
			var block = Interlocked.Increment(ref state->NextBlock) - 1;
			if (block >= state->BlockCount) return;

			try {
				if (_slotExceptions[slot] is not null) continue;
				var start = block * state->ItemsPerBlock;
				blockFunc(start, Int32.Min(state->ItemsPerBlock, state->ItemCount - start), context);
			}
#pragma warning disable CA1031
			catch (Exception e) {
#pragma warning restore CA1031
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
		var state = _blockStateBuffer.BufferPointer + slot;
		Span<ThreadJob> helperJobs = stackalloc ThreadJob[HelpersSpawnedPerHelper];
		var spawnCount = 0;
		while (spawnCount < HelpersSpawnedPerHelper && state->NextBlock < state->BlockCount - 1) {
			var remaining = state->HelpersRemainingToSpawn;
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
		catch (Exception) {
			// No-op because we report exceptions via the helper participation
		}
#pragma warning restore CA1031
	}

	static void ReleaseHelperParticipation<TContext>(nint slot, Exception? _, byte __) => ReleaseParticipation<TContext>((int) slot);

	static void ReleaseParticipation<TContext>(int slot) {
		var state = _blockStateBuffer.BufferPointer + slot;
		if (Interlocked.Decrement(ref state->RemainingParticipants) != 0) return;
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TContext>()) ContextSlotContainer<TContext>.Slots[slot] = default!;
		_slotFacades[slot] = null;
		Volatile.Write(ref state->InUse, 0);
	}
}
