// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public static class TffrInitializer {
	static InitOptions? _initOptions = null;

	internal static InitOptions InitOptions => _initOptions ?? throw new InvalidOperationException($"Please initialize TinyFFR first (using the {nameof(ITffrFactory)} class).");

	public static void Init() => Init(new());
	public static void Init(in InitOptions options) {
		ArgumentNullException.ThrowIfNull(options);
		if (_initOptions != null) throw new InvalidOperationException("TinyFFR can only be initialized once.");

		_initOptions = options;
	}
}