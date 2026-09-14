// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Factory.Local;

/// <summary>
/// A specialization of <see cref="ITinyFfrFactory"/> with additional builder objects exposed for <see cref="LocalTinyFfrFactory"/> instances.
/// </summary>
public interface ILocalTinyFfrFactory : ITinyFfrFactory {
	/// <summary>
	/// Returns the <see cref="IWindowBuilder"/> owned by this factory.
	/// </summary>
	IWindowBuilder WindowBuilder { get; }
	/// <summary>
	/// Returns the <see cref="ILocalApplicationLoopBuilder"/> owned by this factory.
	/// </summary>
	new ILocalApplicationLoopBuilder ApplicationLoopBuilder { get; }
	IApplicationLoopBuilder ITinyFfrFactory.ApplicationLoopBuilder => ApplicationLoopBuilder;
	/// <summary>
	/// Returns the <see cref="ILocalAssetLoader"/> owned by this factory.
	/// </summary>
	new ILocalAssetLoader AssetLoader { get; }
	IAssetLoader ITinyFfrFactory.AssetLoader => AssetLoader;
}