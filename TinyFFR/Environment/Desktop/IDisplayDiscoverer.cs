// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public interface IDisplayDiscoverer {
	ReadOnlySpan<Display> All { get; }
	Display? Recommended { get; }
	Display? Primary { get; }
}