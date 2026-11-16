// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Rendering;

public enum ExportableRenderSurfaceHandleType {
	None = 0,
	OpaqueFileDescriptor = 1
}

public enum ExportableRenderSurfaceSyncKind {
	None = 0,
	Semaphores = 1
}

public readonly record struct ExportableRenderSurfaceImageSet(int Generation, XYPair<int> Dimensions);

public readonly record struct ExportableRenderSurfaceCapabilities(ExportableRenderSurfaceHandleType HandleType, ExportableRenderSurfaceSyncKind SyncKind) {
	public const int DeviceUuidLengthBytes = 16;
	public static ExportableRenderSurfaceCapabilities None { get; } = new(ExportableRenderSurfaceHandleType.None, ExportableRenderSurfaceSyncKind.None);

	public bool IsSupported => HandleType != ExportableRenderSurfaceHandleType.None && SyncKind != ExportableRenderSurfaceSyncKind.None;
}
