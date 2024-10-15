// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Factory;

// Represents the common "factory" interface for all possible factory types
public interface ITinyFfrFactory : ITrackedDisposable {
	IApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IAssetLoader AssetLoader { get; }
	ISceneCameraBuilder SceneCameraBuilder { get; }

	CombinedResourceGroup CreateResourceGroup(int capacity, bool disposeContainedResourcesWhenDisposed);
	CombinedResourceGroup CreateResourceGroup(int capacity, bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
}