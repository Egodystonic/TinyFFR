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
	int GetImageCount(ResourceHandle<ExportableRenderSurface> handle);
	ExportableRenderSurfaceImageSet? GetCurrentImageSet(ResourceHandle<ExportableRenderSurface> handle);
	nint ExportImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, out long memorySizeBytes);
	void ExportSemaphores(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, out nint readySemaphoreHandle, out nint releasedSemaphoreHandle);
	void CloseExportedHandle(ResourceHandle<ExportableRenderSurface> handle, nint exportedHandle);
	bool TryTakeLatestPresentedImage(ResourceHandle<ExportableRenderSurface> handle, out int generation, out int index);
	void ReturnImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, bool consumerSignalledRelease);
}
