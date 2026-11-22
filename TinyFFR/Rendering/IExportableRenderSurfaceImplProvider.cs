// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public interface IExportableRenderSurfaceImplProvider : IDisposableResourceImplProvider<ExportableRenderSurface> {
	XYPair<int> GetDimensions(ResourceHandle<ExportableRenderSurface> handle);
	void SetDimensions(ResourceHandle<ExportableRenderSurface> handle, XYPair<int> newDimensions);
	ExportableRenderSurfaceCapabilities GetCapabilities(ResourceHandle<ExportableRenderSurface> handle);
	void CopyDeviceUuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination);
	bool TryCopyDeviceLuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination);
	int GetImageCount(ResourceHandle<ExportableRenderSurface> handle);
	bool TryGetCurrentImageSet(ResourceHandle<ExportableRenderSurface> handle, out int generation, out XYPair<int> dimensions);
	ExportedRenderSurfaceImage? ExtractImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index);
	void CloseExportedHandle(ResourceHandle<ExportableRenderSurface> handle, ResourceHandle exportedHandle);
	bool TryTakeLatestPresentedImage(ResourceHandle<ExportableRenderSurface> handle, int generation, out int index, out ulong timelineWaitValue, out ulong timelineSignalValue);
	void ReturnImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, bool consumerSignalledRelease);
}
