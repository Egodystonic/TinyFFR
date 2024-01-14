// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public sealed class TffrFactory : ITffrFactory {
	public FactoryConfig Config { get; }
	
	public TffrFactory() : this(new()) { }
	public TffrFactory(FactoryConfig config) {
		ArgumentNullException.ThrowIfNull(config);

		Config = config;
	}
}