// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalModelInstanceImplProvider : IModelInstanceImplProvider, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;

	public LocalModelInstanceImplProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ModelInstance CreateModelInstance(in ModelInstanceCreationConfig config) {
		
	}

	#region Native Methods

	#endregion

	#region Disposal
	public void Dispose() {

	}
	#endregion
}