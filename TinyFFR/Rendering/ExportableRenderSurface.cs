// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public enum ExportableRenderSurfaceHandleType {
	None = 0,
	VulkanOpaqueFileDescriptor = 1,
	D3D11TextureNtHandle = 2,
	D3D11TextureLegacySharedHandle = 3
}

public enum ExportableRenderSurfaceSyncKind {
	None = 0,
	VulkanOpaqueFileDescriptorSemaphores = 1,
	DxgiKeyedMutex = 2,
	CompletedBeforeTake = 3
}

public readonly record struct ExportableRenderSurfaceImageSet {
	readonly ExportableRenderSurface _surface;

	public int Generation { get; }
	public XYPair<int> Dimensions { get; }
	public int ImageCount => _surface.ImageCount;

	internal ExportableRenderSurfaceImageSet(ExportableRenderSurface surface, int generation, XYPair<int> dimensions) {
		_surface = surface;
		Generation = generation;
		Dimensions = dimensions;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ExportedRenderSurfaceImage? ExtractImage(int index) => _surface.ExtractImage(Generation, index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryTakeLatestPresentedImage(out int index) => _surface.TryTakeLatestPresentedImage(Generation, out index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReturnImage(int index, bool consumerSignalledRelease) => _surface.ReturnImage(Generation, index, consumerSignalledRelease);
}

public readonly record struct ExportedRenderSurfaceImage(ResourceHandle ImageHandle, long ImageSizeBytes, ResourceHandle ReadySemaphoreHandle, ResourceHandle ReleasedSemaphoreHandle);

public readonly record struct ExportableRenderSurfaceCapabilities(ExportableRenderSurfaceHandleType HandleType, ExportableRenderSurfaceSyncKind SyncKind) {
	public const int DeviceUuidLengthBytes = 16;
	public const int DeviceLuidLengthBytes = 8;
	public const ulong ConsumerKeyedMutexAcquireKey = 1UL;
	public const ulong ConsumerKeyedMutexReleaseKey = 0UL;
	public static ExportableRenderSurfaceCapabilities None { get; } = new(ExportableRenderSurfaceHandleType.None, ExportableRenderSurfaceSyncKind.None);

	public bool IsSupported => HandleType != ExportableRenderSurfaceHandleType.None && SyncKind != ExportableRenderSurfaceSyncKind.None;
}

public readonly struct ExportableRenderSurface : IDisposableResource<ExportableRenderSurface, IExportableRenderSurfaceImplProvider>, IRenderTarget {
	readonly ResourceHandle<ExportableRenderSurface> _handle;
	readonly IExportableRenderSurfaceImplProvider _impl;

	internal ResourceHandle<ExportableRenderSurface> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ExportableRenderSurface)) : _handle;
	internal IExportableRenderSurfaceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ExportableRenderSurface>();

	IExportableRenderSurfaceImplProvider IResource<ExportableRenderSurface, IExportableRenderSurfaceImplProvider>.Implementation => Implementation;
	ResourceHandle<ExportableRenderSurface> IResource<ExportableRenderSurface>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	public XYPair<int> Dimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDimensions(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDimensions(_handle, value);
	}

	public ExportableRenderSurfaceCapabilities Capabilities {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCapabilities(_handle);
	}

	public int ImageCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetImageCount(_handle);
	}

	public ExportableRenderSurfaceImageSet? CurrentImageSet {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.TryGetCurrentImageSet(_handle, out var generation, out var dimensions) ? new ExportableRenderSurfaceImageSet(this, generation, dimensions) : null;
	}

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<int> IRenderTarget.ViewportDimensions => Dimensions;
	XYPair<int> IRenderTarget.LogicalDimensions => Dimensions;

	internal ExportableRenderSurface(ResourceHandle<ExportableRenderSurface> handle, IExportableRenderSurfaceImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyDeviceUuid(Span<byte> destination) => Implementation.CopyDeviceUuid(_handle, destination);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryCopyDeviceLuid(Span<byte> destination) => Implementation.TryCopyDeviceLuid(_handle, destination);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CloseExportedHandle(ResourceHandle exportedHandle) => Implementation.CloseExportedHandle(_handle, exportedHandle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ExportedRenderSurfaceImage? ExtractImage(int generation, int index) => Implementation.ExtractImage(_handle, generation, index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryTakeLatestPresentedImage(int generation, out int index) => Implementation.TryTakeLatestPresentedImage(_handle, generation, out index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ReturnImage(int generation, int index, bool consumerSignalledRelease) => Implementation.ReturnImage(_handle, generation, index, consumerSignalledRelease);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ExportableRenderSurface IResource<ExportableRenderSurface>.CreateFromHandleAndImpl(ResourceHandle<ExportableRenderSurface> handle, IResourceImplProvider impl) {
		return new ExportableRenderSurface(handle, impl as IExportableRenderSurfaceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<ExportableRenderSurface> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<ExportableRenderSurface> IResource<ExportableRenderSurface>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Exportable Render Surface {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(ExportableRenderSurface other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	public override bool Equals(object? obj) => obj is ExportableRenderSurface other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(ExportableRenderSurface left, ExportableRenderSurface right) => left.Equals(right);
	public static bool operator !=(ExportableRenderSurface left, ExportableRenderSurface right) => !left.Equals(right);
	#endregion
}
