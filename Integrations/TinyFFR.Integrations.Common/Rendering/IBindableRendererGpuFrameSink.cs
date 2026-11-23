// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Rendering;

interface IBindableRendererGpuFrameSink {
	ExportableRenderSurfaceImageHandleType RequiredImageHandleType { get; }
	bool CanAcceptFrame { get; }
	void OnSurfaceAttached(ExportableRenderSurface surface, IBindableRendererGpuFrameSource source);
	void OnSurfaceDetached(ExportableRenderSurface surface);
	void OnFrameRendered(ExportableRenderSurface surface);
}

interface IBindableRendererGpuFrameSource {
	int MaxFramesInFlight { get; }
	void RenderDeferredFrameIfPending();
}
