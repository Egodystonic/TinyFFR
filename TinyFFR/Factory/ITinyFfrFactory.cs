// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Factory;

// Represents the common "factory" interface for all possible factory types
public interface ITinyFfrFactory : IDisposable {
	IDisplayDiscoverer DisplayDiscoverer { get; }
	IApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IAssetLoader AssetLoader { get; }
	ICameraBuilder CameraBuilder { get; }
	IObjectBuilder ObjectBuilder { get; }
	ISceneBuilder SceneBuilder { get; }
	IRendererBuilder RendererBuilder { get; }

	CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed);
	CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity);
	CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
	CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity);
}