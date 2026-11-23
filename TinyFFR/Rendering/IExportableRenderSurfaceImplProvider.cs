// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="ExportableRenderSurface"/> resources.
/// </summary>
public interface IExportableRenderSurfaceImplProvider : IDisposableResourceImplProvider<ExportableRenderSurface> {
	/// <summary>
	/// Invoked via the getter of <see cref="ExportableRenderSurface.Dimensions"/>.
	/// </summary>
	XYPair<int> GetDimensions(ResourceHandle<ExportableRenderSurface> handle);
	/// <summary>
	/// Invoked via the setter of <see cref="ExportableRenderSurface.Dimensions"/>.
	/// </summary>
	void SetDimensions(ResourceHandle<ExportableRenderSurface> handle, XYPair<int> newDimensions);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.Capabilities"/>.
	/// </summary>
	ExportableRenderSurfaceCapabilities GetCapabilities(ResourceHandle<ExportableRenderSurface> handle);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.CopyDeviceUuid"/>.
	/// </summary>
	void CopyDeviceUuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.TryCopyDeviceLuid"/>.
	/// </summary>
	bool TryCopyDeviceLuid(ResourceHandle<ExportableRenderSurface> handle, Span<byte> destination);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.ImageCount"/>.
	/// </summary>
	int GetImageCount(ResourceHandle<ExportableRenderSurface> handle);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.CurrentImageSet"/>.
	/// </summary>
	bool TryGetCurrentImageSet(ResourceHandle<ExportableRenderSurface> handle, out int generation, out XYPair<int> dimensions);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurfaceImageSet.ExtractImage"/>.
	/// </summary>
	ExportedRenderSurfaceImage? ExtractImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurface.CloseExportedHandle"/>.
	/// </summary>
	void CloseExportedHandle(ResourceHandle<ExportableRenderSurface> handle, ResourceHandle exportedHandle);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurfaceImageSet.TryTakeLatestPresentedImage(out int)"/> and <see cref="ExportableRenderSurfaceImageSet.TryTakeLatestPresentedImage(out int, out ulong, out ulong)"/>.
	/// </summary>
	bool TryTakeLatestPresentedImage(ResourceHandle<ExportableRenderSurface> handle, int generation, out int index, out ulong timelineWaitValue, out ulong timelineSignalValue);
	/// <summary>
	/// Invoked via <see cref="ExportableRenderSurfaceImageSet.ReturnImage"/>.
	/// </summary>
	void ReturnImage(ResourceHandle<ExportableRenderSurface> handle, int generation, int index, bool consumerSignalledRelease);
}
