// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

interface ILocalGpuHoldingBufferAllocator {
	FixedByteSlabPool GpuHoldingBufferPool { get; }
	long GpuHoldingBufferFlushThresholdBytes { get; }
	long GpuHoldingBufferStallThresholdBytes { get; }
	bool IsDisposed { get; }
}