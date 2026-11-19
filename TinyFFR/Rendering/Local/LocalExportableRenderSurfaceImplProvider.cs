// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering.Local;

sealed class LocalExportableRenderSurfaceImplProvider : IExportableRenderSurfaceImplProvider, ILocalResourceImplProvider {
	readonly LocalRendererBuilder _owner;

	public LocalExportableRenderSurfaceImplProvider(LocalRendererBuilder owner) => _owner = owner;

	public string GetNameAsNewStringObject(ResourceHandle<ExportableRenderSurface> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<ExportableRenderSurface> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<ExportableRenderSurface> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<ExportableRenderSurface> handle) => _owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<ExportableRenderSurface> handle) => _owner.Dispose(handle);
	public XYPair<int> GetDimensions(ResourceHandle<ExportableRenderSurface> handle) => _owner.GetDimensions(handle);
	public void SetDimensions(ResourceHandle<ExportableRenderSurface> handle, XYPair<int> newDimensions) => _owner.SetDimensions(handle, newDimensions);
	public ExportableRenderSurfaceCapabilities GetCapabilities(ResourceHandle<ExportableRenderSurface> handle) => _owner.GetCapabilities(handle);
	public void CopyDeviceUuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination) => _owner.CopyDeviceUuid(handle, destination);
	public bool TryCopyDeviceLuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination) => _owner.TryCopyDeviceLuid(handle, destination);
	public int GetImageCount(ResourceHandle<ExportableRenderSurface> handle) => _owner.GetImageCount(handle);
	public bool TryGetCurrentImageSet(ResourceHandle<ExportableRenderSurface> handle, out int generation, out XYPair<int> dimensions) => _owner.TryGetCurrentImageSet(handle, out generation, out dimensions);
	public ExportedRenderSurfaceImage? ExtractImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index) => _owner.ExtractImageDataHandles(handle, generation, index);
	public void CloseExportedHandle(ResourceHandle<ExportableRenderSurface> handle, ResourceHandle exportedHandle) => _owner.CloseExportedHandle(handle, exportedHandle);
	public bool TryTakeLatestPresentedImage(ResourceHandle<ExportableRenderSurface> handle, int generation, out int index) => _owner.TryTakeLatestPresentedImage(handle, generation, out index);
	public void ReturnImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, bool consumerSignalledRelease) => _owner.ReturnImage(handle, generation, index, consumerSignalledRelease);
	public override string ToString() => _owner.ToString();
}
