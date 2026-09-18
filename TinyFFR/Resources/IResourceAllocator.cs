// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Allows allocation and borrowing of resource groups, memory, and memory-pooling collections.
/// </summary>
/// <remarks>
/// This type allows you to efficiently work with pooled memory buffers. In a nutshell:
/// <ul>
/// <li>If you want to bundle tightly-related resources together in a single group resource, use <see cref="CreateResourceGroup(bool)"/> and its overloads.</li>
/// <li>If you want to borrow a buffer for a short-lived operation, use <see cref="BorrowSpan"/>.</li>
/// <li>If you want to borrow a buffer for a long-lived operation, use <see cref="CreatePooledMemoryBuffer"/>.</li>
/// <li>If you want to borrow a zero-allocation collection-type for a short-lived operation, use <see cref="GetSharedScratchList"/>/<see cref="GetSharedScratchDictionary"/>/<see cref="GetSharedScratchSet"/>.</li>
/// <li>If you want to instantiate a zero-allocation collection-type for a field scoped to the entire lifetime of the application, use
/// <see cref="CreateNewArrayPoolBackedList"/>/<see cref="CreateNewArrayPoolBackedDictionary"/>/<see cref="CreateNewArrayPoolBackedSet"/>/<see cref="CreateNewArrayPoolBackedLruCache(int)"/>.</li>
/// </ul>
/// </remarks>
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
	/// Borrows a temporary <typeparamref name="T"/> span of length <paramref name="numElements"/> from the memory pool.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Borrowing a span for a short-lived operation (rather than creating garbage) has a profoundly positive impact on performance; <i>especially</i> where you are borrowing multiple
	/// spans or kilobytes+ of data every frame.
	/// </para>
	/// <para>
	/// You must dispose the returned lease once you have finished using it, at which point the <c>Span</c> should no longer be accessed.
	/// Failing to do so will create a memory leak. For most short-lived operations it's recommended to use a <c>using</c> statement.
	/// </para>
	/// </remarks>
	/// <param name="numElements">The number of elements the borrowed span should contain.</param>
	/// <param name="clearMemoryOnLeaseEnd">Whether to clear (zero) the memory on lease disposal. Can be set to <c>false</c> if you
	/// know your data contains no GC references and the performance hit of clearing the memory is costly.</param>
	/// <returns>A <see cref="ScopedSpanLease{T}"/> that should be disposed when the borrowed memory is no longer needed.
	/// Note: The leased span may not be zeroed (cleared); you should manually invoke <see cref="Span{T}.Clear"/> if necessary.</returns>
	/// <seealso cref="CreatePooledMemoryBuffer"/>
	ScopedSpanLease<T> BorrowSpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true);
	/// <summary>
	/// Borrows a temporary, read-only <typeparamref name="T"/> span of length <paramref name="numElements"/> from the memory pool.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Borrowing a span for a short-lived operation (rather than creating garbage) has a profoundly positive impact on performance; <i>especially</i> where you are borrowing multiple
	/// spans or kilobytes+ of data every frame.
	/// </para>
	/// <para>
	/// You must dispose the returned lease once you have finished using it, at which point the borrowed span should no longer be accessed.
	/// Failing to do so will create a memory leak. For most short-lived operations it's recommended to use a <c>using</c> statement.
	/// </para>
	/// </remarks>
	/// <param name="numElements">The number of elements the borrowed span should contain.</param>
	/// <param name="clearMemoryOnLeaseEnd">Whether to clear (zero) the memory on lease disposal. Can be set to <c>false</c> if you
	/// know your data contains no GC references and the performance hit of clearing the memory is costly.</param>
	/// <returns>A <see cref="ScopedReadOnlySpanLease{T}"/> that should be disposed when the borrowed memory is no longer needed.
	/// Note: The leased span may not be zeroed (cleared); you should not assume otherwise.</returns>
	/// <seealso cref="CreatePooledMemoryBuffer"/>
	ScopedReadOnlySpanLease<T> BorrowReadOnlySpan<T>(int numElements, bool clearMemoryOnLeaseEnd = true);
	
	/// <summary>
	/// Borrows a <see cref="Memory{T}"/> of length <paramref name="numElements"/> from the memory pool.
	/// </summary>
	/// <remarks>
	/// <para>
	/// You must return the borrowed memory to the memory pool when you no longer need it via <see cref="ReturnPooledMemoryBuffer"/>.
	/// </para>
	/// <para>
	/// In contrast to <see cref="BorrowSpan"/>, this method should be used when you need a long-lived lease. The returned <see cref="Memory{T}"/>
	/// can also be stored in the heap just like any other object.
	/// </para>
	/// </remarks>
	/// <param name="numElements">The number of elements the returned buffer should contain.</param>
	/// <returns>A <see cref="Memory{T}"/> containing the rented memory reference. The memory is guaranteed to have been zeroed (cleared).</returns>
	/// <seealso cref="BorrowSpan"/>
	Memory<T> CreatePooledMemoryBuffer<T>(int numElements);
	/// <summary>
	/// Returns a buffer previously obtained from <see cref="CreatePooledMemoryBuffer{T}"/> back to the shared pool.
	/// The buffer's memory will be zeroed (cleared).
	/// </summary>
	/// <param name="buffer">The buffer to return.</param>
	void ReturnPooledMemoryBuffer<T>(Memory<T> buffer);

	/// <summary>
	/// Returns a scratch (temporary, reusable) collection. The returned collection is reused/recycled (i.e. every invocation of this function returns the same object when passing the same type parameters).
	/// </summary>
	/// <remarks>
	/// Because the returned instance is shared, never hold on to it (or otherwise keep using it) across any call that might request the same <typeparamref name="T"/>/<paramref name="bufferIndex"/> pair again
	/// (for example, a reentrant or recursive call) as both call sites would receive (and mutate) the very same underlying list. Use a different <paramref name="bufferIndex"/> per call site if you need more
	/// than one scratch collection of the same element type live at once.
	/// </remarks>
	/// <param name="bufferIndex">Can be used to create/reuse a different collection instance. Each value of this parameter identifies a separate, new buffer. On first invocation of
	/// this method with a new <c>bufferIndex</c> a new collection will be created, each subsequent invocation returns the same buffer. Can be any value but each new value creates a new buffer that
	/// will be internally stored, so use sparingly.</param>
	/// <param name="clearBuffer">If <see langword="true"/> the collection will be cleared before being handed to you. Can set to <c>false</c> if you don't need the collection cleared.</param>
	IList<T> GetSharedScratchList<T>(int bufferIndex = 0, bool clearBuffer = true);
	/// <inheritdoc cref="GetSharedScratchList{T}"/>
	IDictionary<TKey, TValue> GetSharedScratchDictionary<TKey, TValue>(int bufferIndex = 0, bool clearBuffer = true);
	/// <inheritdoc cref="GetSharedScratchList{T}"/>
	ISet<T> GetSharedScratchSet<T>(int bufferIndex = 0, bool clearBuffer = true);

	/// <inheritdoc cref="CreateNewArrayPoolBackedDictionary" />
	/// <param name="initialCapacity">A hint for how many elements the list is expected to eventually contain, used to reduce the number of internal resizes needed as elements are added.
	/// Defaults to an implementation-defined value if <see langword="null"/>.</param>
	IArrayPoolBackedList<T> CreateNewArrayPoolBackedList<T>(int? initialCapacity = null);
	/// <summary>
	/// Creates a new collection, backed by pooled storage. Must be disposed when no longer needed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// All collection operations (e.g. resizing, adding, removing, clearing, enumerating etc) are guaranteed to create zero garbage. The returned object instance itself <b>does</b> represent
	/// garbage and that garbage will be released to the GC when you invoke <c>Dispose()</c>. Therefore it is recommended to create array-pool-backed collections once at initialization
	/// time and reuse them throughout the lifetime of your application.
	/// </para>
	/// <para>
	/// Unlike the shared scratch collections, the returned instance is not shared with any other caller: you own it, and must dispose it yourself once you are finished with it.
	/// </para>
	/// </remarks>
	IArrayPoolBackedDictionary<TKey, TValue> CreateNewArrayPoolBackedDictionary<TKey, TValue>();
	/// <inheritdoc cref="CreateNewArrayPoolBackedDictionary" />
	IArrayPoolBackedSet<T> CreateNewArrayPoolBackedSet<T>();
	/// <inheritdoc cref="CreateNewArrayPoolBackedDictionary" />
	/// <param name="maxValuesInCache">The maximum number of entries the cache may hold before it starts evicting its least-recently-used entries. Must be positive.</param>
	IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache);
	/// <inheritdoc cref="CreateNewArrayPoolBackedDictionary" />
	/// <param name="maxValuesInCache">The maximum number of entries the cache may hold before it starts evicting its least-recently-used entries. Must be positive.</param>
	/// <param name="cacheEvictionCallback">A callback invoked whenever a value leaves the cache (see the remarks on <see cref="IArrayPoolBackedLruCache{TKey,TValue}"/>).</param>
	/// <param name="cacheEvictionCallbackArg">An arbitrary value passed through to <paramref name="cacheEvictionCallback"/> unchanged on every invocation.</param>
	unsafe IArrayPoolBackedLruCache<TKey, TValue> CreateNewArrayPoolBackedLruCache<TKey, TValue>(int maxValuesInCache, delegate* managed<object?, TKey, TValue, void> cacheEvictionCallback, object? cacheEvictionCallbackArg = null);

	/// <summary>
	/// Schedules <paramref name="work"/> to run on one of TinyFFR's own shared worker threads, returning a <see cref="TinyFfrAsyncOperation{T}"/> representing its eventual result.
	/// </summary>
	/// <remarks>
	/// This is a way of sharing TinyFFR's own worker thread pool for your own arbitrary background work; it is not a way of making otherwise-synchronous TinyFFR functionality asynchronous.
	/// Everything in TinyFFR that can meaningfully be made asynchronous already has its own dedicated asynchronous API. You should not invoke any TinyFFR library function from inside the <paramref name="work"/>
	/// callback unless you're sure of what you're doing.
	/// </remarks>
	/// <param name="context">An arbitrary value passed through to <paramref name="work"/> unchanged; use this to pass in whatever state the job needs without capturing variables in a closure.</param>
	/// <param name="work">The work to execute on a worker thread.</param>
	TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, Func<TContext?, TResult?> work) where TContext : class where TResult : class;
	/// <inheritdoc cref="DispatchWorkerThreadJob{TContext,TResult}(TContext,Func{TContext,TResult})"/>
	unsafe TinyFfrAsyncOperation<TResult?> DispatchWorkerThreadJob<TContext, TResult>(TContext? context, delegate* managed<TContext?, TResult?> work) where TContext : class where TResult : class;
}