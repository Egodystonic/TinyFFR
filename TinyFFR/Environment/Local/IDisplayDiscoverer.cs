// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IDisplayDiscoverer {
	ReadOnlySpan<Display> All { get; }
	Display? Recommended { get; }
	Display? Primary { get; }
}