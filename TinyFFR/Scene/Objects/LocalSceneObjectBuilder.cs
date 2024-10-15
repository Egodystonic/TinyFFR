// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalSceneObjectBuilder : ISceneObjectBuilder, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalModelInstanceImplProvider _modelInstanceImplProvider;

	public LocalSceneObjectBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_modelInstanceImplProvider = new(globals);
	}

	public ModelInstance CreateModelInstance() => CreateModelInstance(new());
	public ModelInstance CreateModelInstance(in ModelInstanceCreationConfig config) {
		return _modelInstanceImplProvider.CreateModelInstance(in config);
	}

	#region Disposal
	public void Dispose() {
		_modelInstanceImplProvider.Dispose();
	}
	#endregion
}