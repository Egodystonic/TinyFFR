// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory;

/// <summary>
/// The factory object is the "root" entry point of TinyFFR. It must be created before all other resources, and should be disposed as the last step when you're done using TinyFFR. 
/// </summary>
/// <remarks>
/// <para>
/// The factory object has no API or methods, it only exposes a set of builders via properties.
/// Each builder presents a specific interface that is the only way to create or load resources for its resource type.
/// For example, the <see cref="ILightBuilder"/> is the only way to create <see cref="Light"/>s.
/// </para>
/// <para>
/// When you dispose the factory, any remaining resources will be immediately invalidated and should not be accessed.
/// Ideally, you should dispose all other resources created during a session before disposing the factory.
/// </para>
/// <para>
/// Any builder object accessed via a factory property will continue to be valid for the lifetime of the factory. That is to say, as long as you don't invoke factory.Dispose(), all your builder instances will be valid.
/// Builders themselves can not be disposed.
/// The same builder object instance is always returned from a given property on the factory (e.g. factory.LightBuilder always returns the same ILightBuilder instance).
/// It is safe to store and/or pass around a reference to a specific builder rather than passing around the entire factory.
/// </para>
/// <para>
/// Only one factory instance may be "live" at any given time. Trying to create a second factory before disposing the first will result in an exception being thrown.
/// </para>
/// </remarks>
/// <seealso cref="Local.ILocalTinyFfrFactory"/>
public interface ITinyFfrFactory : IDisposable {
	/// <summary>
	/// Returns the <see cref="IDisplayDiscoverer"/> owned by this factory.
	/// </summary>
	IDisplayDiscoverer DisplayDiscoverer { get; }
	/// <summary>
	/// Returns the <see cref="IApplicationLoopBuilder"/> owned by this factory.
	/// </summary>
	IApplicationLoopBuilder ApplicationLoopBuilder { get; }
	/// <summary>
	/// Returns the <see cref="IAssetLoader"/> owned by this factory.
	/// </summary>
	IAssetLoader AssetLoader { get; }
	/// <summary>
	/// Returns the <see cref="IAssetBakery"/> owned by this factory.
	/// </summary>
	IAssetBakery AssetBakery { get; }
	/// <summary>
	/// Returns the <see cref="IMeshBuilder"/> owned by this factory.
	/// </summary>
	IMeshBuilder MeshBuilder => AssetLoader.MeshBuilder;
	/// <summary>
	/// Returns the <see cref="ITextureBuilder"/> owned by this factory.
	/// </summary>
	ITextureBuilder TextureBuilder => AssetLoader.TextureBuilder;
	/// <summary>
	/// Returns the <see cref="IMaterialBuilder"/> owned by this factory.
	/// </summary>
	IMaterialBuilder MaterialBuilder => AssetLoader.MaterialBuilder;
	/// <summary>
	/// Returns the <see cref="ICameraBuilder"/> owned by this factory.
	/// </summary>
	ICameraBuilder CameraBuilder { get; }
	/// <summary>
	/// Returns the <see cref="ILightBuilder"/> owned by this factory.
	/// </summary>
	ILightBuilder LightBuilder { get; }
	/// <summary>
	/// Returns the <see cref="IObjectBuilder"/> owned by this factory.
	/// </summary>
	IObjectBuilder ObjectBuilder { get; }
	/// <summary>
	/// Returns the <see cref="ISceneBuilder"/> owned by this factory.
	/// </summary>
	ISceneBuilder SceneBuilder { get; }
	/// <summary>
	/// Returns the <see cref="IRendererBuilder"/> owned by this factory.
	/// </summary>
	IRendererBuilder RendererBuilder { get; }
	/// <summary>
	/// Returns the <see cref="IResourceAllocator"/> owned by this factory.
	/// </summary>
	IResourceAllocator ResourceAllocator { get; }
	/// <summary>
	/// Returns the <see cref="IResourceDirectory"/> owned by this factory.
	/// </summary>
	IResourceDirectory ResourceDirectory { get; }
}