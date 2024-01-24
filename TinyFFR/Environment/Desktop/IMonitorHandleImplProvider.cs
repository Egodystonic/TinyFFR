// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Desktop;

interface IMonitorHandleImplProvider {
	XYPair GetResolution(MonitorHandle handle);
	XYPair GetPositionOffset(MonitorHandle handle);
	int GetNameMaxLength();
	int GetName(MonitorHandle handle, Span<char> dest);
}