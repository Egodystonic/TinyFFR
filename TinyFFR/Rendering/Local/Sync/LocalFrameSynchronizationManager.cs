// Created on 2025-04-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Rendering.Local.Sync;

static unsafe class LocalFrameSynchronizationManager {
	[InlineArray(MaxBufferSize)]
	struct CircularFenceBuffer {
		UIntPtr _;

		public CircularFenceBuffer Cycled(UIntPtr newHandle) {
			var result = new CircularFenceBuffer();

			var thisAsSpan = (ReadOnlySpan<UIntPtr>) this;
			var resultAsSpan = (Span<UIntPtr>) result;

			thisAsSpan[..^1].CopyTo(resultAsSpan[1..]);
			resultAsSpan[0] = newHandle;

			return result;
		}
	}
	readonly record struct FenceData(CircularFenceBuffer Buffer, int BufferSize);
	readonly struct QueuedResourceCallback {
		public readonly UIntPtr Handle;
		public readonly delegate* managed<UIntPtr, InteropResult> Callback;

		public QueuedResourceCallback(UIntPtr handle, delegate*<UIntPtr, InteropResult> callback) {
			Handle = handle;
			Callback = callback;
		}

		public void Invoke() => Callback(Handle).ThrowIfFailure();
	}

	public const int MaxBufferSize = 5;
	static readonly ArrayPoolBackedMap<ResourceHandle<Renderer>, FenceData> _rendererMap = new();
	static readonly ArrayPoolBackedMap<UIntPtr, ArrayPoolBackedVector<QueuedResourceCallback>> _fenceCallbackMap = new();
	static readonly ObjectPool<ArrayPoolBackedVector<QueuedResourceCallback>> _callbackQueuePool = new(&CreateCallbackQueuePool);
	static ArrayPoolBackedVector<QueuedResourceCallback> _unassignedCallbacks = _callbackQueuePool.Rent();

	public static void RegisterRenderer(ResourceHandle<Renderer> renderer, int bufferSize) {
		if (bufferSize is < 0 or > MaxBufferSize) {
			throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, $"Should be between 0 and {MaxBufferSize}.");
		}

		_rendererMap.Add(renderer, new(new(), bufferSize));
	}

	public static void DeregisterRenderer(ResourceHandle<Renderer> renderer) {
		if (!_rendererMap.Remove(renderer, out var fenceData)) throw new InvalidOperationException($"Renderer '{renderer}' was not registered.");

		for (var i = 0; i < fenceData.BufferSize; ++i) {
			var fenceHandle = fenceData.Buffer[i];
			if (fenceHandle != UIntPtr.Zero) ExecuteFence(fenceHandle);
		}

		// If this was the last renderer, anything in the unassigned callbacks will be left "hanging" so call them here
		if (_rendererMap.Count == 0) {
			foreach (var callback in _unassignedCallbacks) callback.Invoke();
			_unassignedCallbacks.Clear();
		}
	}

	public static void EmitFenceAndCycleBuffer(ResourceHandle<Renderer> renderer) {
		if (!_rendererMap.TryGetValue(renderer, out var currentFenceData)) throw new InvalidOperationException($"Renderer '{renderer}' was not registered.");

		// Special case handling for 0-buffer rendering
		if (currentFenceData.BufferSize == 0) {
			CreateGpuFence(out var immediateFenceHandle).ThrowIfFailure();
			ExecuteFence(immediateFenceHandle);
			
			foreach (var callback in _unassignedCallbacks) callback.Invoke();
			_unassignedCallbacks.Clear();

			return;
		}
		
		var fenceToExecute = currentFenceData.Buffer[currentFenceData.BufferSize - 1];
		if (fenceToExecute != UIntPtr.Zero) ExecuteFence(fenceToExecute);

		CreateGpuFence(out var newFenceHandle).ThrowIfFailure();
		_rendererMap[renderer] = currentFenceData with { Buffer = currentFenceData.Buffer.Cycled(newFenceHandle) };

		if (_unassignedCallbacks.Count == 0) return;
		_fenceCallbackMap[newFenceHandle] = _unassignedCallbacks;
		_unassignedCallbacks = _callbackQueuePool.Rent();
	}

	public static void QueueResourceDisposal(UIntPtr handle, delegate*<UIntPtr, InteropResult> callback) {
		var qrc = new QueuedResourceCallback(handle, callback);

		// If there are no renderers registered we won't get any fences, so just dispose straight away
		if (_rendererMap.Count == 0) {
			qrc.Invoke();
			return;
		}

		_unassignedCallbacks.Add(qrc);
	}

	static void ExecuteFence(UIntPtr fenceHandle) {
		WaitForFence(fenceHandle).ThrowIfFailure();

		if (!_fenceCallbackMap.Remove(fenceHandle, out var callbacks)) return;

		foreach (var callback in callbacks) {
			callback.Invoke();
		}

		callbacks.Clear();
		_callbackQueuePool.Return(callbacks);
	}

	static ArrayPoolBackedVector<QueuedResourceCallback> CreateCallbackQueuePool() => new();

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_gpu_fence")]
	static extern InteropResult CreateGpuFence(out UIntPtr outFenceHandle);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "wait_for_fence")]
	static extern InteropResult WaitForFence(UIntPtr fenceHandle);
	#endregion
}