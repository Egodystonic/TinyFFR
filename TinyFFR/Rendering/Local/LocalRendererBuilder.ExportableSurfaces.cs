// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Rendering.Local;

sealed partial class LocalRendererBuilder {
	readonly record struct ExportableSurfaceData(ResourceHandle<ExportableRenderSurface> Handle, XYPair<int> Dimensions, UIntPtr DescriptorHandle);

	const string DefaultExportableRenderSurfaceName = "Unnamed Exportable Render Surface";
	const int ExportableSurfaceImageCount = 5;
	readonly ArrayPoolBackedMap<ResourceHandle<ExportableRenderSurface>, ExportableSurfaceData> _loadedExportableSurfaces = new();
	readonly LocalExportableRenderSurfaceImplProvider _exportableRenderSurfaceImplProvider;

	ExportableRenderSurfaceCapabilities QueryExportableRenderSurfaceCapabilities() {
		ThrowIfThisIsDisposed();
		Span<byte> uuidSpace = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes];
		GetExportableSurfaceCapabilities(out var handleType, out var syncKind, ref uuidSpace[0]).ThrowIfFailure();
		var result = new ExportableRenderSurfaceCapabilities((ExportableRenderSurfaceHandleType) handleType, (ExportableRenderSurfaceSyncKind) syncKind); 
		if (!Enum.IsDefined(result.HandleType) || !Enum.IsDefined(result.SyncKind)) return ExportableRenderSurfaceCapabilities.None;
		return result;
	}

	public ExportableRenderSurface? TryCreateExportableRenderSurface(in ExportableRenderSurfaceCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		if (!QueryExportableRenderSurfaceCapabilities().IsSupported) return null;

		AllocateExportableSurfaceDescriptor(
			(uint) config.Dimensions.X,
			(uint) config.Dimensions.Y,
			out var descriptorHandle
		).ThrowIfFailure();

		_previousHandleId++;
		var handle = new ResourceHandle<ExportableRenderSurface>(_previousHandleId);
		_loadedExportableSurfaces.Add(handle, new(handle, config.Dimensions, descriptorHandle));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultExportableRenderSurfaceName);
		return HandleToInstance(handle);
	}

	public XYPair<int> GetDimensions(ResourceHandle<ExportableRenderSurface> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedExportableSurfaces[handle].Dimensions;
	}
	public void SetDimensions(ResourceHandle<ExportableRenderSurface> handle, XYPair<int> newDimensions) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newDimensions.Clamp(new(ExportableRenderSurfaceCreationConfig.MinDimensionXY), new(ExportableRenderSurfaceCreationConfig.MaxDimensionXY)) != newDimensions) {
			throw new ArgumentOutOfRangeException(nameof(newDimensions), newDimensions, $"Both X and Y must be between {ExportableRenderSurfaceCreationConfig.MinDimensionXY} and {ExportableRenderSurfaceCreationConfig.MaxDimensionXY}.");
		}
		var data = _loadedExportableSurfaces[handle];
		if (data.Dimensions == newDimensions) return;
		SetExportableSurfaceDescriptorSize(data.DescriptorHandle, (uint) newDimensions.X, (uint) newDimensions.Y).ThrowIfFailure();
		_loadedExportableSurfaces[handle] = data with { Dimensions = newDimensions };
	}
	public ExportableRenderSurfaceCapabilities GetCapabilities(ResourceHandle<ExportableRenderSurface> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return QueryExportableRenderSurfaceCapabilities();
	}
	public void CopyDeviceUuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (destination.Length < ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes) {
			throw new ArgumentException($"Destination must be at least {ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes} bytes long.", nameof(destination));
		}
		GetExportableSurfaceCapabilities(out _, out _, ref destination[0]).ThrowIfFailure();
	}
	public int GetImageCount(ResourceHandle<ExportableRenderSurface> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return ExportableSurfaceImageCount;
	}
	public bool TryGetCurrentImageSet(ResourceHandle<ExportableRenderSurface> handle, out int generation, out XYPair<int> dimensions) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetExportableSurfaceGeneration(_loadedExportableSurfaces[handle].DescriptorHandle, out var nativeGeneration, out var isLive, out var width, out var height).ThrowIfFailure();
		generation = (int) nativeGeneration;
		dimensions = new XYPair<int>((int) width, (int) height);
		return isLive;
	}
	public ExportedRenderSurfaceImage? ExtractImageDataHandles(ResourceHandle<ExportableRenderSurface> handle, int generation, int index) {
		ThrowIfThisOrHandleIsDisposed(handle);
		ThrowIfExportableSurfaceImageIndexIsInvalid(index);
		ExportExportableSurfaceImageHandles(_loadedExportableSurfaces[handle].DescriptorHandle, (uint) generation, (uint) index, out var image, out var imageSize, out var ready, out var released, out var found).ThrowIfFailure();
		if (!found) return null;
		return new ExportedRenderSurfaceImage(new ResourceHandle((nuint) image), (long) imageSize, new ResourceHandle((nuint) ready), new ResourceHandle((nuint) released));
	}
	public void CloseExportedHandle(ResourceHandle<ExportableRenderSurface> handle, ResourceHandle exportedHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		CloseExportableSurfaceHandle((long) exportedHandle.AsInteger).ThrowIfFailure();
	}
	public bool TryTakeLatestPresentedImage(ResourceHandle<ExportableRenderSurface> handle, int generation, out int index) {
		ThrowIfThisOrHandleIsDisposed(handle);
		TakeLatestPresentedExportableSurfaceImage(_loadedExportableSurfaces[handle].DescriptorHandle, (uint) generation, out var nativeIndex, out var found).ThrowIfFailure();
		index = (int) nativeIndex;
		return found;
	}
	public void ReturnImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, bool consumerSignalledRelease) {
		ThrowIfThisOrHandleIsDisposed(handle);
		ThrowIfExportableSurfaceImageIndexIsInvalid(index);
		ReturnExportableSurfaceImage(_loadedExportableSurfaces[handle].DescriptorHandle, (uint) generation, (uint) index, consumerSignalledRelease).ThrowIfFailure();
	}

	static void ThrowIfExportableSurfaceImageIndexIsInvalid(int index) {
		if (index < 0 || index >= ExportableSurfaceImageCount) throw new ArgumentOutOfRangeException(nameof(index), index, $"Must be between 0 and {ExportableSurfaceImageCount - 1}.");
	}

	public string GetNameAsNewStringObject(ResourceHandle<ExportableRenderSurface> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultExportableRenderSurfaceName));
	}
	public int GetNameLength(ResourceHandle<ExportableRenderSurface> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultExportableRenderSurfaceName).Length;
	}
	public void CopyName(ResourceHandle<ExportableRenderSurface> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultExportableRenderSurfaceName, destinationBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ExportableRenderSurface HandleToInstance(ResourceHandle<ExportableRenderSurface> h) => new(h, _exportableRenderSurfaceImplProvider);

	unsafe IndirectEnumerable<object, ExportableRenderSurface> IResourceDirectory<ExportableRenderSurface>.AllActiveInstances {
		get {
			static LocalRendererBuilder CastSelf(object self) => self as LocalRendererBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._loadedExportableSurfaces.Count;
			static int GetVersion(object self) => CastSelf(self)._loadedExportableSurfaces.Version;
			static ExportableRenderSurface GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._loadedExportableSurfaces.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	bool IResourceDirectory<ExportableRenderSurface>.ResourceNameMatchIsMatching(ExportableRenderSurface resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultExportableRenderSurfaceName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultExportableRenderSurfaceName).Equals(name, comparisonType);
	}

	public bool IsDisposed(ResourceHandle<ExportableRenderSurface> handle) => _isDisposed || !_loadedExportableSurfaces.ContainsKey(handle);
	public void Dispose(ResourceHandle<ExportableRenderSurface> handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));

		var data = _loadedExportableSurfaces[handle];
		DisposeExportableSurfaceDescriptor(data.DescriptorHandle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		_loadedExportableSurfaces.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ExportableRenderSurface> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ExportableRenderSurface));

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_exportable_surface_capabilities")]
	static extern InteropResult GetExportableSurfaceCapabilities(
		out int outHandleType,
		out int outSyncKind,
		ref byte outDeviceUuid
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_exportable_surface_descriptor")]
	static extern InteropResult AllocateExportableSurfaceDescriptor(
		uint width,
		uint height,
		out UIntPtr outDescriptor
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_exportable_surface_descriptor_size")]
	static extern InteropResult SetExportableSurfaceDescriptorSize(
		UIntPtr descriptor,
		uint width,
		uint height
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_exportable_surface_descriptor")]
	static extern InteropResult DisposeExportableSurfaceDescriptor(
		UIntPtr descriptor
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_exportable_swap_chain")]
	static extern InteropResult AllocateExportableSwapChain(
		UIntPtr descriptor,
		out UIntPtr outSwapChain
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_exportable_surface_generation")]
	static extern InteropResult GetExportableSurfaceGeneration(
		UIntPtr descriptor,
		out uint outGeneration,
		out InteropBool outIsLive,
		out uint outWidth,
		out uint outHeight
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "export_exportable_surface_image_handles")]
	static extern InteropResult ExportExportableSurfaceImageHandles(
		UIntPtr descriptor,
		uint generation,
		uint index,
		out long outImageHandle,
		out ulong outImageSizeBytes,
		out long outReadyHandle,
		out long outReleasedHandle,
		out InteropBool outFound
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "close_exportable_surface_handle")]
	static extern InteropResult CloseExportableSurfaceHandle(
		long handle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "take_latest_presented_exportable_surface_image")]
	static extern InteropResult TakeLatestPresentedExportableSurfaceImage(
		UIntPtr descriptor,
		uint generation,
		out uint outIndex,
		out InteropBool outFound
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "return_exportable_surface_image")]
	static extern InteropResult ReturnExportableSurfaceImage(
		UIntPtr descriptor,
		uint generation,
		uint index,
		InteropBool consumerSignalledRelease
	);
	#endregion
}
