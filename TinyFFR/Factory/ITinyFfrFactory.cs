// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory;

// Represents the common "factory" interface for all possible factory types
public interface ITinyFfrFactory : IDisposable {
	IDisplayDiscoverer DisplayDiscoverer { get; }
	IApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IAssetLoader AssetLoader { get; }
	ICameraBuilder CameraBuilder { get; }
	ILightBuilder LightBuilder { get; }
	IObjectBuilder ObjectBuilder { get; }
	ISceneBuilder SceneBuilder { get; }
	IRendererBuilder RendererBuilder { get; }

	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name);
	ResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity);
}