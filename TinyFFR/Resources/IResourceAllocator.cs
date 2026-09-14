// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Provides low/no-GC-allocation memory and collection utilities, plus <see cref="ResourceGroup"/> creation and worker-thread job dispatch, all scoped to (and released alongside) the owning factory.
/// </summary>
public interface IResourceAllocator {
	/// <summary>
	/// Creates a new, empty <see cref="ResourceGroup"/>.
	/// </summary>
	/// <param name="disposeContainedResourcesWhenDisposed">The value the created group's <see cref="ResourceGroup.DisposesContainedResourcesByDefaultWhenDisposed"/> should have.</param>
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed);
	/// <inheritdoc cref="CreateResourceGroup(bool)"/>
	/// <param name="disposeContainedResourcesWhenDisposed">The value the created group's <see cref="ResourceGroup.DisposesContainedResourcesByDefaultWhenDisposed"/> should have.</param>
	/// <param name="initialCapacity">A hint for how many resources the group is expected to eventually contain, used to reduce the number of internal resizes needed as resources are added.</param>
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity);
	/// <inheritdoc cref="CreateResourceGroup(bool)"/>
	/// <param name="disposeContainedResourcesWhenDisposed">The value the created group's <see cref="ResourceGroup.DisposesContainedResourcesByDefaultWhenDisposed"/> should have.</param>
	/// <param name="name">A name for the group.</param>
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
	/// <inheritdoc cref="CreateResourceGroup(bool)"/>
	/// <param name="disposeContainedResourcesWhenDisposed">The value the created group's <see cref="ResourceGroup.DisposesContainedResourcesByDefaultWhenDisposed"/> should have.</param>
	/// <param name="name">A name for the group.</param>
	/// <param name="initialCapacity">A hint for how many resources the group is expected to eventually contain, used to reduce the number of internal resizes needed as resources are added.</param>
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity);

	/// <summary>
	/// Rents a temporary, pooled <see cref="Span{T}"/> of exactly <paramref name="numElements"/> elements.
	/// </summary>
	/// <remarks>
	/// You must dispose the returned lease (e.g. via a <see langword="using"/> declaration/statement) once you have finished using it, to return the underlying memory to the shared pool. The memory is not necessarily zeroed when first borrowed: it may contain leftover data from a previous lease.
	/// </remarks>
	/// <param name="numElements">The number of elements the borrowed span should contain.</param>
	/// <param name="clearMemoryOnLeaseEnd">Whether to clear the memory back to its default values when the lease ends (i.e. before the underlying storage is returned to the shared pool).</param>
	ScopedSpanLease<T> BorrowSpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true);
	/// <inheritdoc cref="BorrowSpan{T}"/>
	ScopedReadOnlySpanLease<T> BorrowReadOnlySpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true);
	/// <summary>
	/// Rents a temporary, pooled <see cref="Memory{T}"/> buffer of exactly <paramref name="numElements"/> elements.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="BorrowSpan{T}"/>, this is not a scoped lease: you must explicitly return the buffer yourself via <see cref="ReturnPooledMemoryBuffer{T}"/> once you have finished using it. Prefer <see cref="BorrowSpan{T}"/> where possible, since it can't be forgotten. The memory is not necessarily zeroed when first borrowed: it may contain leftover data from a previous rental.
	/// </remarks>
	/// <param name="numElements">The number of elements the returned buffer should contain.</param>
	Memory<T> CreatePooledMemoryBuffer<T>(int numElements);
	/// <summary>
	/// Returns a buffer previously obtained from <see cref="CreatePooledMemoryBuffer{T}"/> back to the shared pool.
	/// </summary>
	/// <param name="buffer">The buffer to return. Must have been obtained from this same <see cref="IResourceAllocator"/>'s <see cref="CreatePooledMemoryBuffer{T}"/>, with the same element type <typeparamref name="T"/>.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="buffer"/> was not rented from this allocator with element type <typeparamref name="T"/>.</exception>
	void ReturnPooledMemoryBuffer<T>(Memory<T> buffer);

	/// <summary>
	/// Returns a scratch (temporary, reusable) <see cref="IList{T}"/>, shared across every caller that requests one with the same <typeparamref name="T"/> and <paramref name="bufferIndex"/> from this <see cref="IResourceAllocator"/>.
	/// </summary>
	/// <remarks>
	/// Because the returned instance is shared, never hold on to it (or otherwise keep using it) across any call that might request the same <typeparamref name="T"/>/<paramref name="bufferIndex"/> pair again — for example, a reentrant or recursive call — as both call sites would receive (and mutate) the very same underlying list. Use a different <paramref name="bufferIndex"/> per call site if you need more than one scratch list of the same element type live at once.
	/// </remarks>
	/// <param name="bufferIndex">An index distinguishing this scratch list from other scratch lists of the same element type <typeparamref name="T"/>.</param>
	/// <param name="clearBuffer">If <see langword="true"/> (the default), the list is cleared before being returned, so it always starts empty. Pass <see langword="false"/> to instead reuse whatever this buffer already contained from its previous use.</param>
	IList<T> GetSharedScratchList<T>(int bufferIndex = 0, bool clearBuffer = true);
	/// <inheritdoc cref="GetSharedScratchList{T}"/>
	IDictionary<TKey, TValue> GetSharedScratchDictionary<TKey, TValue>(int bufferIndex = 0, bool clearBuffer = true);
	/// <inheritdoc cref="GetSharedScratchList{T}"/>
	ISet<T> GetSharedScratchSet<T>(int bufferIndex = 0, bool clearBuffer = true);

	/// <summary>
	/// Creates a new, independent <see cref="IArrayPoolBackedList{T}"/>, backed by pooled storage.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="GetSharedScratchList{T}"/>, the returned instance is not shared with any other caller: you own it, and must dispose it yourself once you are finished with it.
	/// </remarks>
	/// <param name="initialCapacity">A hint for how many elements the list is expected to eventually contain, used to reduce the number of internal resizes needed as elements are added. Defaults to an implementation-defined value if <see langword="null"/>.</param>
	IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null);
	/// <summary>
	/// Creates a new, independent <see cref="IArrayPoolBackedDictionary{TKey,TValue}"/>, backed by pooled storage.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="GetSharedScratchDictionary{TKey,TValue}"/>, the returned instance is not shared with any other caller: you own it, and must dispose it yourself once you are finished with it.
	/// </remarks>
	IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>();
	/// <summary>
	/// Creates a new, independent <see cref="IArrayPoolBackedSet{T}"/>, backed by pooled storage.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="GetSharedScratchSet{T}"/>, the returned instance is not shared with any other caller: you own it, and must dispose it yourself once you are finished with it.
	/// </remarks>
	IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>();
	/// <summary>
	/// Creates a new <see cref="IArrayPoolBackedLruCache{TKey,TValue}"/> with a maximum capacity of <paramref name="maxValuesInCache"/> entries, backed by pooled storage.
	/// </summary>
	/// <param name="maxValuesInCache">The maximum number of entries the cache may hold before it starts evicting its least-recently-used entries. Must be positive.</param>
	IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache);
	/// <inheritdoc cref="CreateNewArrayPoolBackedLruCache{TKey,TValue}(int)"/>
	/// <param name="maxValuesInCache">The maximum number of entries the cache may hold before it starts evicting its least-recently-used entries. Must be positive.</param>
	/// <param name="cacheEvictionCallback">A callback invoked whenever a value leaves the cache (see the remarks on <see cref="IArrayPoolBackedLruCache{TKey,TValue}"/>).</param>
	/// <param name="cacheEvictionCallbackArg">An arbitrary value passed through to <paramref name="cacheEvictionCallback"/> unchanged on every invocation.</param>
	unsafe IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate* managed<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null);

	/// <summary>
	/// Schedules <paramref name="work"/> to run on one of TinyFFR's own shared worker threads, returning a <see cref="TinyFfrAsyncOperation{T}"/> representing its eventual result.
	/// </summary>
	/// <remarks>
	/// This is a way of sharing TinyFFR's own worker thread pool for your own arbitrary background work; it is not a way of making otherwise-synchronous TinyFFR functionality asynchronous. Everything in TinyFFR that can meaningfully be made asynchronous already has its own dedicated asynchronous API.
	/// </remarks>
	/// <param name="context">An arbitrary value passed through to <paramref name="work"/> unchanged; use this to pass in whatever state the job needs without capturing variables in a closure.</param>
	/// <param name="work">The work to execute on a worker thread.</param>
	TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, Func<TContext?, TResult?> work) where TContext : class where TResult : class;
	/// <inheritdoc cref="DispatchWorkerThreadJob{TContext,TResult}(TContext,Func{TContext,TResult})"/>
	/// <remarks>
	/// This overload accepts a function pointer rather than a delegate, avoiding a delegate allocation for callers that already have (or can easily obtain) a suitable function pointer.
	/// </remarks>
	unsafe TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, delegate* managed<TContext?, TResult?> work) where TContext : class where TResult : class;
}