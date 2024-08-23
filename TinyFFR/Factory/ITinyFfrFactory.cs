// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;

namespace Egodystonic.TinyFFR.Factory;

// Represents the common "factory" interface for all possible factory types
public interface ITinyFfrFactory : IDisposable {
	IApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IAssetLoader AssetLoader { get; }
}